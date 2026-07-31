using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Gregs_Auto;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Gregs_Auto.DAL.Context;
using Gregs_Auto.DAL.Repositories;
using Gregs_Auto.Domain.IRepositories;
using Gregs_Auto.Domain.Licensing;
using Gregs_Auto.Security;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// EF Core DbContext, pointed at the GregsAuto database using the connection
// string named "GregsAutoContext" in appsettings.json.
builder.Services.AddDbContext<GregsAutoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GregsAutoContext")));

// Cookie auth for staff sign-in. No ASP.NET Core Identity — just the built-in
// cookie handler plus our own Users table/PasswordHasher.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";

        // Signed in but not permitted is a different situation from not signed
        // in — bouncing to the login form for it just confuses people.
        options.AccessDeniedPath = "/Account/Denied";

        // Roughly a shift, and it slides while someone is working. The front
        // desk machine is shared, so this is the backstop for "nobody clicked
        // Log Out" rather than the primary protection.
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Cookie.Name = "GregsAuto.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

// How the shop runs, and what it has paid for — both read from the Shops row
// rather than from configuration, so the shop can change its own hours and bay
// count from a screen. Cached; the settings screen calls Reload after saving.
builder.Services.AddSingleton<IShopContext, ShopContextProvider>();
builder.Services.AddSingleton<IShopSettings, ShopSettingsFromContext>();
builder.Services.AddSingleton<IFeatureFlags, FeatureFlagsFromContext>();

builder.Services.AddSingleton<IAuthorizationHandler, FeatureAuthorizationHandler>();

// Who may do what. Declared as policies so the rule lives here rather than as
// role-name strings sprinkled through the controllers.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.ManageStaff, p => p.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.ManageCustomers, p => p.RequireRole(Roles.Admin, Roles.Manager));
    options.AddPolicy(Policies.ViewCustomers, p => p.RequireRole(Roles.Admin, Roles.Manager, Roles.Technician));
    options.AddPolicy(Policies.ManageAppointments, p => p.RequireRole(Roles.Admin, Roles.Manager, Roles.Technician));
    options.AddPolicy(Policies.ManageShopSettings, p => p.RequireRole(Roles.Admin, Roles.Manager));

    // Tier-gated areas. These combine a role check with a licensing check, so a
    // feature the shop hasn't bought is unreachable rather than merely hidden.
    // Nothing uses these yet — they're the socket the paid tiers plug into.
    options.AddPolicy(FeaturePolicies.Invoicing, p => p
        .RequireRole(Roles.Admin, Roles.Manager)
        .AddRequirements(new FeatureRequirement(Feature.Invoicing)));

    options.AddPolicy(FeaturePolicies.Inspections, p => p
        .RequireRole(Roles.Admin, Roles.Manager, Roles.Technician)
        .AddRequirements(new FeatureRequirement(Feature.DigitalInspections)));
});

// The public booking form is an anonymous POST that writes to the database, so
// it needs a ceiling. Per-IP: a real person books once, thinks about it, maybe
// books again. Five in ten minutes is far beyond that and far below what a
// script would want.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimitPolicies.PublicBooking, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));

    // Sign-in gets one too. UserLogic locks a known account after five bad
    // passwords, but that does nothing about someone spraying many different
    // addresses — each one is a first failure. This covers that.
    options.AddPolicy(RateLimitPolicies.Login, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));
});

// Repositories: bind each interface to its implementation.
// Scoped = one instance per web request.
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILoginAuditRepository, LoginAuditRepository>();
builder.Services.AddScoped<IBookingRequestRepository, BookingRequestRepository>();
builder.Services.AddScoped<IShopRepository, ShopRepository>();

// One transaction per business operation. See IUnitOfWork.
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Shop clock. Appointment times are wall-clock times at the shop, so the logic
// layer needs the shop's timezone rather than the server's — hosted on a UTC
// machine, DateTime.Now would reject same-day bookings as being in the past.
// A bad timezone id fails loudly at startup, which beats silently wrong times.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IShopClock, ShopClockFromContext>();

// Logic layer (business rules) — bind interface to implementation.
builder.Services.AddScoped<IAppointmentLogic, AppointmentLogic>();
builder.Services.AddScoped<IUserLogic, UserLogic>();
builder.Services.AddScoped<IBookingRequestLogic, BookingRequestLogic>();
builder.Services.AddScoped<IServiceLogic, ServiceLogic>();
builder.Services.AddScoped<IShopLogic, ShopLogic>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

// Must come before UseAuthorization — authorization checks read the identity
// authentication sets up here.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


// Top-level statements compile to an internal Program class. The smoke tests
// spin the real application up through WebApplicationFactory<Program>, which
// needs to see it.
public partial class Program { }

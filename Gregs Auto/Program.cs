using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Gregs_Auto.DAL.Context;
using Gregs_Auto.DAL.Repositories;
using Gregs_Auto.Domain.IRepositories;
using Gregs_Auto.Domain.Implementations;
using Gregs_Auto.Domain.Implementations.Interfaces;

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
        options.AccessDeniedPath = "/Account/Login";
    });

// Repositories: bind each interface to its implementation.
// Scoped = one instance per web request.
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Logic layer (business rules) — bind interface to implementation.
builder.Services.AddScoped<IAppointmentLogic, AppointmentLogic>();
builder.Services.AddScoped<IUserLogic, UserLogic>();

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

// Must come before UseAuthorization — authorization checks read the identity
// authentication sets up here.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

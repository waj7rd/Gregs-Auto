using Microsoft.EntityFrameworkCore;
using Gregs_Auto.DAL.Context;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// EF Core DbContext, pointed at the GregsAuto database using the connection
// string named "GregsAutoContext" in appsettings.json.
builder.Services.AddDbContext<GregsAutoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GregsAutoContext")));

// Repositories and logic-layer services get registered here as they're added,
// e.g. builder.Services.AddScoped<IFooRepository, FooRepository>();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

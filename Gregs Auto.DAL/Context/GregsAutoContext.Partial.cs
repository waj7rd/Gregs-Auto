using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Gregs_Auto.DAL.Context;

// Hand-written partial that extends the auto-generated GregsAutoContext.
// Adds a parameterless constructor + OnConfiguring so the context can be created
// with "new GregsAutoContext()" (as the GenericRepository does) and configure
// itself from appsettings. When the context is built through dependency
// injection (options already set), the IsConfigured guard skips this.
public partial class GregsAutoContext
{
    public GregsAutoContext()
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("GregsAutoContext");
            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}

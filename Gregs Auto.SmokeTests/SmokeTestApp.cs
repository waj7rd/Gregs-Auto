using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gregs_Auto.SmokeTests;

// The real application, wired the real way, against a throwaway database.
//
// Everything here goes through the actual HTTP pipeline: routing, model
// binding, anti-forgery, authorisation, DI. That's deliberate — the unit suite
// covers business rules in isolation and cannot see a broken migration, a
// mis-registered service, or a repository holding its own DbContext. All three
// of those have happened.
public class SmokeTestApp : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:GregsAutoContext"] = TestDatabase.ConnectionString,

                // Development turns on the developer exception page, which is
                // what surfaces a failure as a readable 500 rather than a blank
                // one when a test goes wrong.
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
            });
        });

        return base.CreateHost(builder);
    }

    // A client that doesn't chase redirects, so a test can assert on the
    // redirect itself rather than on whatever it landed on.
    //
    // The https base address is load-bearing. The auth cookie is issued with
    // CookieSecurePolicy.Always, so over http://localhost the handler accepts
    // it and then refuses to send it back — every staff request arrives
    // anonymous and bounces to the login page. The TestServer does no real TLS;
    // the scheme is what makes the cookie usable.
    //
    // Which is also the first real proof that the Secure flag lands.
    public HttpClient CreateDirectClient() =>
        CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
        });
}

// One database build shared by every test class in the collection. Rebuilding
// per class would be correct and far too slow.
public class SmokeDatabaseFixture : IDisposable
{
    public SmokeDatabaseFixture()
    {
        TestDatabase.Rebuild();
        App = new SmokeTestApp();
    }

    public SmokeTestApp App { get; }

    public void Dispose()
    {
        App.Dispose();
        TestDatabase.Drop();
        GC.SuppressFinalize(this);
    }
}

[CollectionDefinition(Name)]
public class SmokeCollection : ICollectionFixture<SmokeDatabaseFixture>
{
    public const string Name = "smoke";
}

using Microsoft.Data.SqlClient;

namespace Gregs_Auto.SmokeTests;

// Builds a throwaway database from the same SQL scripts you'd run by hand.
//
// Using the real scripts rather than EnsureCreated is the entire point: the
// outage these tests exist to catch came from a migration script, and a schema
// generated from the EF model would have been fine while the real one was not.
public static class TestDatabase
{
    public const string Name = "GregsAuto_SmokeTests";

    public static string ConnectionString =>
        $"Server=.;Database={Name};Trusted_Connection=True;TrustServerCertificate=True;";

    private const string MasterConnection =
        "Server=.;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

    // In the order a human would run them.
    private static readonly string[] Scripts =
    {
        "CreateDatabase.sql",
        "SeedData.sql",
        "SetStaffPasswords.sql",
        "AddUserControls.sql",
        "AddBookingRequests.sql",
        "AddArchiving.sql",
        "AddShop.sql",
        "SeedShops.sql",
        "AddShopScoping.sql",
        "FixShopIdDefaults.sql",
        "AddAppointmentSnapshot.sql",
    };

    public static void Rebuild()
    {
        Drop();

        var scriptDir = FindScriptDirectory();

        foreach (var script in Scripts)
        {
            var path = Path.Combine(scriptDir, script);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Missing migration script: {path}");

            // CreateDatabase.sql names the production database. Point every
            // script at the throwaway one instead.
            var sql = File.ReadAllText(path)
                .Replace("CREATE DATABASE GregsAuto;", $"CREATE DATABASE {Name};")
                .Replace("USE GregsAuto;", $"USE {Name};");

            Execute(sql, script);
        }
    }

    public static void Drop()
    {
        using var connection = new SqlConnection(MasterConnection);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $@"
            IF DB_ID('{Name}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{Name}];
            END";
        command.ExecuteNonQuery();
    }

    // sqlcmd splits on GO; SqlCommand doesn't understand it, so do it here.
    private static void Execute(string sql, string scriptName)
    {
        var batches = sql.Split(
            new[] { "\nGO\r\n", "\nGO\n", "\r\nGO\r\n", "\r\nGO\n" },
            StringSplitOptions.RemoveEmptyEntries);

        using var connection = new SqlConnection(MasterConnection);
        connection.Open();

        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
                continue;

            using var command = connection.CreateCommand();
            command.CommandText = batch;
            command.CommandTimeout = 60;

            try
            {
                command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(
                    $"{scriptName} failed: {ex.Message}\n\nBatch:\n{batch.Trim()[..Math.Min(400, batch.Trim().Length)]}", ex);
            }
        }
    }

    // Scripts are gitignored, so they're only ever on a developer's machine.
    // Walk up from the test binaries to find them.
    private static string FindScriptDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Gregs Auto.DAL", "Scripts");
            if (Directory.Exists(candidate))
                return candidate;

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Couldn't find Gregs Auto.DAL/Scripts. Those scripts are gitignored — " +
            "a fresh clone cannot run these tests until they're restored.");
    }
}

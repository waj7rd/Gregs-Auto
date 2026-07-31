using System.Net;
using Microsoft.Data.SqlClient;

namespace Gregs_Auto.SmokeTests;

// End-to-end checks against a real database through the real HTTP stack.
//
// Each of these covers something the unit suite structurally cannot see. The
// case that motivated them: a migration added ShopId as NOT NULL with no
// default and nothing in the code set it, so every INSERT in the application
// failed — while all 89 unit tests stayed green, because none of them touch a
// database.
[Collection(SmokeCollection.Name)]
public class SmokeTests
{
    private readonly SmokeTestApp _app;

    public SmokeTests(SmokeDatabaseFixture fixture) => _app = fixture.App;

    private static async Task<int> ScalarAsync(string sql)
    {
        await using var connection = new SqlConnection(TestDatabase.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static DateTime NextOpenWeekday(int hour)
    {
        // The seeded shop is closed Saturday and Sunday.
        var day = DateTime.Today.AddDays(3);
        while (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            day = day.AddDays(1);

        return day.AddHours(hour);
    }

    // ---------------------------------------------------------------- pages

    [Theory]
    [InlineData("/")]
    [InlineData("/Services")]
    [InlineData("/Appointments/Schedule")]
    [InlineData("/Account/Login")]
    public async Task Public_pages_load(string url)
    {
        var response = await _app.CreateDirectClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task The_public_booking_page_exposes_no_customer_data()
    {
        var html = await _app.CreateDirectClient().GetStringAsync("/Appointments/Schedule");

        // Seeded customers — none of them belong on a page anyone can reach.
        foreach (var name in new[] { "John Mitchell", "Sarah Alvarez", "David Chen", "Priya Natarajan", "Marcus Webb" })
            Assert.DoesNotContain(name, html);

        Assert.DoesNotContain("@example.com", html);
        Assert.DoesNotContain("id=\"vehicleId\"", html);
    }

    [Fact]
    public async Task Staff_pages_require_signing_in()
    {
        var response = await _app.CreateDirectClient().GetAsync("/Customers");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    // -------------------------------------------------------------- inserts

    // The regression test for the outage. Every insert path was broken and
    // nothing in the unit suite noticed.
    [Fact]
    public async Task A_guest_can_submit_a_booking_request()
    {
        var before = await ScalarAsync("SELECT COUNT(*) FROM BookingRequests");
        var client = _app.CreateDirectClient();

        var response = await Web.PostFormAsync(client, "/Appointments/SubmitRequest", "/Appointments/Schedule",
            new Dictionary<string, string>
            {
                ["CustomerName"] = "Smoke Guest",
                ["Phone"] = "555-0140",
                ["VehicleYear"] = "2019",
                ["VehicleMake"] = "Toyota",
                ["VehicleModel"] = "Tacoma",
                ["ServiceId"] = "1",
                ["RequestedAt"] = NextOpenWeekday(10).ToString("yyyy-MM-ddTHH:mm"),
            });

        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Redirect,
            $"Expected a redirect to the thank-you page. Got {(int)response.StatusCode}: {Web.ExtractAlert(body) ?? "no message"}");

        Assert.Equal(before + 1, await ScalarAsync("SELECT COUNT(*) FROM BookingRequests"));
    }

    [Fact]
    public async Task Staff_can_add_a_customer()
    {
        var client = await Web.SignInAsync(_app, "greg@gregsauto.com");
        var before = await ScalarAsync("SELECT COUNT(*) FROM Customers");

        await Web.PostFormAsync(client, "/Customers/Create", "/Customers/Create",
            new Dictionary<string, string>
            {
                ["FullName"] = "Smoke Customer",
                ["Email"] = $"smoke{Guid.NewGuid():N}@example.test",
                ["Phone"] = "555-0141",
            });

        Assert.Equal(before + 1, await ScalarAsync("SELECT COUNT(*) FROM Customers"));
    }

    // ----------------------------------------------------------------- auth

    [Fact]
    public async Task Sign_in_works_and_the_wrong_password_does_not()
    {
        await Web.SignInAsync(_app, "greg@gregsauto.com");   // throws if it fails

        var client = _app.CreateDirectClient();
        var response = await Web.PostFormAsync(client, "/Account/Login", "/Account/Login",
            new Dictionary<string, string> { ["Email"] = "greg@gregsauto.com", ["Password"] = "not-the-password" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);   // redisplays the form
        Assert.Equal("Invalid email or password.", Web.ExtractAlert(await response.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task A_technician_cannot_reach_a_manager_only_page()
    {
        var client = await Web.SignInAsync(_app, "omar@gregsauto.com");

        var response = await client.GetAsync("/Customers/Create");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Denied", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task An_admin_only_page_is_closed_to_a_manager()
    {
        var client = await Web.SignInAsync(_app, "lisa@gregsauto.com");

        var response = await client.GetAsync("/Staff");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Denied", response.Headers.Location!.ToString());
    }

    // ------------------------------------------------------- form defaults

    [Fact]
    public async Task The_booking_form_prefills_a_slot_the_rules_would_accept()
    {
        var html = await _app.CreateDirectClient().GetStringAsync("/Appointments/Schedule");

        var value = System.Text.RegularExpressions.Regex.Match(
            html, @"id=""Guest_RequestedAt""[^>]*value=""([^""]+)""").Groups[1].Value;

        Assert.False(string.IsNullOrWhiteSpace(value),
            "The date field rendered with no value. A DateTime formatted the default way is " +
            "silently rejected by datetime-local — asp-format is required.");

        // Must be the exact shape datetime-local accepts, or the browser blanks it.
        var slot = DateTime.ParseExact(value, "yyyy-MM-ddTHH:mm", null);

        Assert.True(slot > DateTime.Now, $"Prefilled {slot}, which is already in the past.");
        Assert.DoesNotContain(slot.DayOfWeek, new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });
        Assert.InRange(slot.TimeOfDay, TimeSpan.FromHours(8), TimeSpan.FromHours(17));
    }

    [Fact]
    public async Task The_prefilled_slot_can_actually_be_submitted()
    {
        var client = _app.CreateDirectClient();
        var html = await client.GetStringAsync("/Appointments/Schedule");
        var slot = System.Text.RegularExpressions.Regex.Match(
            html, @"id=""Guest_RequestedAt""[^>]*value=""([^""]+)""").Groups[1].Value;

        var response = await Web.PostFormAsync(client, "/Appointments/SubmitRequest", "/Appointments/Schedule",
            new Dictionary<string, string>
            {
                ["CustomerName"] = "Prefill Check",
                ["Phone"] = "555-0149",
                ["VehicleYear"] = "2021",
                ["VehicleMake"] = "Mazda",
                ["VehicleModel"] = "CX-5",
                ["ServiceId"] = "1",
                ["RequestedAt"] = slot,
            });

        // The whole point: submitting the default unchanged must not produce an error.
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Redirect,
            $"Submitting the prefilled default was refused: {Web.ExtractAlert(body) ?? "no message"}");
    }
}

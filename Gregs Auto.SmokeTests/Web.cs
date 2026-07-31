using System.Net;
using System.Text.RegularExpressions;

namespace Gregs_Auto.SmokeTests;

// Small helpers so the tests read as flows rather than as HTTP plumbing.
//
// Every POST needs an anti-forgery token lifted from a GET first, and staff
// pages need a signed-in cookie. Doing that inline in each test would bury the
// thing being asserted.
public static class Web
{
    private static readonly Regex TokenPattern = new(
        @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""", RegexOptions.Compiled);

    public static async Task<string> GetTokenAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        var html = await response.Content.ReadAsStringAsync();
        var match = TokenPattern.Match(html);

        if (!match.Success)
        {
            // Nearly always means the GET didn't land where you thought —
            // redirected to login because the cookie didn't stick, or to
            // Denied because the role was wrong. Say which.
            var where = response.Headers.Location?.ToString() ?? "no redirect";
            throw new InvalidOperationException(
                $"No anti-forgery token on {url} (HTTP {(int)response.StatusCode}, location: {where}).");
        }

        return match.Groups[1].Value;
    }

    public static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string url, string tokenFrom, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = await GetTokenAsync(client, tokenFrom);
        return await client.PostAsync(url, new FormUrlEncodedContent(fields));
    }

    // Signs in and returns the client, so a test can chain straight into the
    // staff pages.
    public static async Task<HttpClient> SignInAsync(SmokeTestApp app, string email, string password = "GregsAuto123!")
    {
        var client = app.CreateDirectClient();

        var response = await PostFormAsync(client, "/Account/Login", "/Account/Login",
            new Dictionary<string, string> { ["Email"] = email, ["Password"] = password });

        if (response.StatusCode != HttpStatusCode.Redirect)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Sign-in for {email} did not redirect (got {(int)response.StatusCode}). " +
                $"Error shown: {ExtractAlert(body) ?? "none"}");
        }

        return client;
    }

    // Pulls the message out of a Bootstrap alert, which is how this app reports
    // both refusals and confirmations.
    public static string? ExtractAlert(string html, string kind = "danger")
    {
        var match = Regex.Match(html, $@"alert alert-{kind}"">\s*([^<]+)");
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value).Trim() : null;
    }
}

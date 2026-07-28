using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.Implementations;
using Gregs_Auto.Domain.Implementations.Interfaces;
using Gregs_Auto.Domain.Security;
using Gregs_Auto.Tests.Fakes;

namespace Gregs_Auto.Tests;

public class UserLogicTests
{
    private const string GoodPassword = "CorrectHorse99";
    private const string AdminEmail = "greg@gregsauto.com";

    private readonly FakeUserRepository _users = new();
    private readonly FakeLoginAuditRepository _audit = new();
    private readonly TestClock _clock = new();

    private UserLogic Logic() => new(_users, _audit, _clock);

    private User Given(string email, string role = Roles.Technician, bool active = true, string password = GoodPassword)
    {
        var user = new User
        {
            FullName = "Test Person",
            Email = email,
            Role = role,
            IsActive = active,
            PasswordHash = PasswordHasher.Hash(password)
        };

        _users.Seed(user);
        user.UserId = _users.GetAll().Count();
        return user;
    }

    private async Task FailLoginAsync(string email, int times)
    {
        for (var i = 0; i < times; i++)
            await Logic().AuthenticateAsync(email, "wrong-password", "::1");
    }

    // ---------- basics ----------

    [Fact]
    public async Task Signs_in_with_the_right_password()
    {
        Given(AdminEmail, Roles.Admin);

        var result = await Logic().AuthenticateAsync(AdminEmail, GoodPassword, "::1");

        Assert.True(result.Succeeded);
        Assert.Equal(AdminEmail, result.User!.Email);
    }

    [Fact]
    public async Task An_unknown_email_looks_exactly_like_a_wrong_password()
    {
        Given(AdminEmail);

        var unknown = await Logic().AuthenticateAsync("nobody@example.com", GoodPassword, "::1");
        var wrongPassword = await Logic().AuthenticateAsync(AdminEmail, "nope", "::1");

        // Identical outcome, so the login form can't be used to find out which
        // addresses have accounts.
        Assert.Equal(AuthenticationOutcome.InvalidCredentials, unknown.Outcome);
        Assert.Equal(AuthenticationOutcome.InvalidCredentials, wrongPassword.Outcome);
    }

    [Fact]
    public async Task An_attempt_on_an_unknown_email_is_still_audited()
    {
        await Logic().AuthenticateAsync("stranger@example.com", "guess", "10.0.0.9");

        var row = Assert.Single(_audit.All);
        Assert.Null(row.UserId);
        Assert.Equal("stranger@example.com", row.EmailAttempted);
        Assert.Equal(LoginAuditEvent.Failure, row.Event);
        Assert.Equal("10.0.0.9", row.IpAddress);
    }

    [Fact]
    public async Task Successful_sign_in_stamps_last_login()
    {
        var user = Given(AdminEmail);

        await Logic().AuthenticateAsync(AdminEmail, GoodPassword, "::1");

        Assert.Equal(_clock.UtcNow, user.LastLoginAt);
    }

    // ---------- lockout ----------

    [Fact]
    public async Task Locks_out_after_five_failures()
    {
        Given(AdminEmail);

        await FailLoginAsync(AdminEmail, 4);
        var fifth = await Logic().AuthenticateAsync(AdminEmail, "wrong-password", "::1");

        Assert.Equal(AuthenticationOutcome.LockedOut, fifth.Outcome);
        Assert.NotNull(fifth.LockedOutUntil);
    }

    [Fact]
    public async Task The_right_password_is_refused_while_locked_out()
    {
        Given(AdminEmail);
        await FailLoginAsync(AdminEmail, 5);

        var result = await Logic().AuthenticateAsync(AdminEmail, GoodPassword, "::1");

        Assert.Equal(AuthenticationOutcome.LockedOut, result.Outcome);
    }

    [Fact]
    public async Task The_lockout_expires_on_its_own()
    {
        Given(AdminEmail);
        await FailLoginAsync(AdminEmail, 5);

        _clock.Advance(TimeSpan.FromMinutes(16));
        var result = await Logic().AuthenticateAsync(AdminEmail, GoodPassword, "::1");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task A_good_sign_in_resets_the_failure_count()
    {
        var user = Given(AdminEmail);

        await FailLoginAsync(AdminEmail, 3);
        await Logic().AuthenticateAsync(AdminEmail, GoodPassword, "::1");

        Assert.Equal(0, user.FailedLoginCount);

        // Four more shouldn't lock, because the counter went back to zero.
        await FailLoginAsync(AdminEmail, 4);
        Assert.Null(user.LockedOutUntil);
    }

    // ---------- deactivated accounts ----------

    [Fact]
    public async Task A_deactivated_account_is_refused_even_with_the_right_password()
    {
        Given("gone@gregsauto.com", active: false);

        var result = await Logic().AuthenticateAsync("gone@gregsauto.com", GoodPassword, "::1");

        Assert.Equal(AuthenticationOutcome.Inactive, result.Outcome);
    }

    [Fact]
    public async Task A_wrong_password_on_a_deactivated_account_reports_invalid_credentials()
    {
        Given("gone@gregsauto.com", active: false);

        var result = await Logic().AuthenticateAsync("gone@gregsauto.com", "wrong", "::1");

        // Not Inactive — otherwise guessing would reveal that the account exists.
        Assert.Equal(AuthenticationOutcome.InvalidCredentials, result.Outcome);
    }

    // ---------- staff management ----------

    [Fact]
    public async Task Will_not_create_two_accounts_with_the_same_email()
    {
        Given(AdminEmail, Roles.Admin);

        var result = await Logic().CreateStaffAsync("Someone Else", AdminEmail, Roles.Technician, GoodPassword);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorMessage);
    }

    [Fact]
    public async Task Rejects_a_role_that_isnt_real()
    {
        var result = await Logic().CreateStaffAsync("New Person", "new@gregsauto.com", "Overlord", GoodPassword);

        Assert.False(result.Success);
        Assert.Contains("Unknown role", result.ErrorMessage);
    }

    [Fact]
    public async Task Will_not_demote_the_only_admin()
    {
        var admin = Given(AdminEmail, Roles.Admin);
        Given("tech@gregsauto.com");

        var result = await Logic().UpdateStaffAsync(admin.UserId, "Greg", AdminEmail, Roles.Manager);

        Assert.False(result.Success);
        Assert.Contains("only active Admin", result.ErrorMessage);
    }

    [Fact]
    public async Task Will_not_deactivate_the_only_admin()
    {
        var admin = Given(AdminEmail, Roles.Admin);

        var result = await Logic().SetActiveAsync(admin.UserId, isActive: false);

        Assert.False(result.Success);
        Assert.True(admin.IsActive);
    }

    [Fact]
    public async Task Demoting_an_admin_is_fine_when_another_one_exists()
    {
        var first = Given(AdminEmail, Roles.Admin);
        Given("second@gregsauto.com", Roles.Admin);

        var result = await Logic().UpdateStaffAsync(first.UserId, "Greg", AdminEmail, Roles.Manager);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(Roles.Manager, first.Role);
    }

    [Fact]
    public async Task A_deactivated_admin_doesnt_count_as_cover()
    {
        var active = Given(AdminEmail, Roles.Admin);
        Given("dormant@gregsauto.com", Roles.Admin, active: false);

        var result = await Logic().SetActiveAsync(active.UserId, isActive: false);

        Assert.False(result.Success);
    }

    // ---------- passwords ----------

    [Fact]
    public async Task Changing_your_own_password_requires_the_current_one()
    {
        var user = Given(AdminEmail);

        var wrong = await Logic().ChangeOwnPasswordAsync(user.UserId, "not-it", "BrandNewPass99", "::1");
        Assert.False(wrong.Success);

        var right = await Logic().ChangeOwnPasswordAsync(user.UserId, GoodPassword, "BrandNewPass99", "::1");
        Assert.True(right.Success, right.ErrorMessage);

        var signIn = await Logic().AuthenticateAsync(AdminEmail, "BrandNewPass99", "::1");
        Assert.True(signIn.Succeeded);
    }

    [Fact]
    public async Task An_admin_reset_clears_the_lockout()
    {
        var user = Given(AdminEmail);
        await FailLoginAsync(AdminEmail, 5);

        await Logic().SetPasswordAsync(user.UserId, "ResetPass1234");

        Assert.Null(user.LockedOutUntil);
        Assert.True((await Logic().AuthenticateAsync(AdminEmail, "ResetPass1234", "::1")).Succeeded);
    }

    [Fact]
    public async Task Unlocking_leaves_the_password_alone()
    {
        var user = Given(AdminEmail);
        await FailLoginAsync(AdminEmail, 5);

        await Logic().UnlockAsync(user.UserId);

        Assert.True((await Logic().AuthenticateAsync(AdminEmail, GoodPassword, "::1")).Succeeded);
    }
}

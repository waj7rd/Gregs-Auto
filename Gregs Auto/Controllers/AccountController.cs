using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Gregs_Auto.Domain.Implementations.Interfaces;
using Gregs_Auto.ViewModels;

namespace Gregs_Auto.Controllers;

public class AccountController : Controller
{
    private readonly IUserLogic _userLogic;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IUserLogic userLogic, ILogger<AccountController> logger)
    {
        _userLogic = userLogic;
        _logger = logger;
    }

    // GET /Account/Login
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    // POST /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var result = await _userLogic.AuthenticateAsync(model.Email, model.Password, ClientIpAddress());

        if (!result.Succeeded)
        {
            // Warning rather than Information: a run of these is what a brute
            // force attempt looks like from the outside.
            _logger.LogWarning("Failed sign-in for {Email} from {Ip}: {Outcome}",
                model.Email, ClientIpAddress(), result.Outcome);

            model.ErrorMessage = result.Outcome switch
            {
                AuthenticationOutcome.LockedOut => LockoutMessage(result.LockedOutUntil),
                AuthenticationOutcome.Inactive => "This account has been deactivated. Ask an administrator to turn it back on.",
                _ => "Invalid email or password."
            };

            return View(model);
        }

        var user = result.User!;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation("{Email} signed in as {Role} from {Ip}", user.Email, user.Role, ClientIpAddress());

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    // POST /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = CurrentUserId();
        if (userId.HasValue)
        {
            await _userLogic.RecordLogoutAsync(
                userId.Value,
                User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                ClientIpAddress());
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    // GET /Account/Denied — signed in, but this role can't reach that page.
    [Authorize]
    public IActionResult Denied() => View();

    // GET /Account/ChangePassword
    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    // POST /Account/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userId = CurrentUserId();
        if (!userId.HasValue)
            return Forbid();

        var result = await _userLogic.ChangeOwnPasswordAsync(
            userId.Value, model.CurrentPassword, model.NewPassword, ClientIpAddress());

        if (!result.Success)
        {
            model.ErrorMessage = result.ErrorMessage;
            return View(model);
        }

        model.SuccessMessage = "Password changed.";
        return View(new ChangePasswordViewModel { SuccessMessage = "Password changed." });
    }

    private int? CurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private string? ClientIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private static string LockoutMessage(DateTime? until)
    {
        if (!until.HasValue)
            return "Too many failed attempts. Try again shortly.";

        var minutes = Math.Max(1, (int)Math.Ceiling((until.Value - DateTime.UtcNow).TotalMinutes));
        return $"Too many failed attempts. Try again in {minutes} minute{(minutes == 1 ? "" : "s")}.";
    }
}

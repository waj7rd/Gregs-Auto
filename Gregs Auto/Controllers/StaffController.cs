using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.ViewModels;

namespace Gregs_Auto.Controllers;

// Staff accounts. Admin only — this is the one area a Manager is kept out of,
// because whoever can create accounts effectively controls the shop's data.
[Authorize(Policy = Policies.ManageStaff)]
public class StaffController : Controller
{
    private const int ActivityRowCount = 100;

    private readonly IUserLogic _userLogic;
    private readonly IShopClock _clock;

    public StaffController(IUserLogic userLogic, IShopClock clock)
    {
        _userLogic = userLogic;
        _clock = clock;
    }

    // GET /Staff
    public async Task<IActionResult> Index()
    {
        return View(await BuildListAsync());
    }

    // GET /Staff/Create
    public IActionResult Create() => View(new CreateStaffViewModel());

    // POST /Staff/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStaffViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _userLogic.CreateStaffAsync(model.FullName, model.Email, model.Role, model.Password);

        if (!result.Success)
        {
            model.ErrorMessage = result.ErrorMessage;
            return View(model);
        }

        TempData["StaffSuccess"] = $"Added {model.FullName.Trim()}.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Staff/Edit/{id}
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userLogic.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        return View(new EditStaffViewModel
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        });
    }

    // POST /Staff/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditStaffViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _userLogic.UpdateStaffAsync(model.UserId, model.FullName, model.Email, model.Role);

        if (!result.Success)
        {
            model.ErrorMessage = result.ErrorMessage;
            return View(model);
        }

        TempData["StaffSuccess"] = $"Updated {model.FullName.Trim()}.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Staff/SetActive
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool isActive)
    {
        // Deactivating yourself signs you out of a page you can't get back to.
        if (!isActive && id == CurrentUserId())
            return await BackToIndexWithError("You can't deactivate your own account.");

        var result = await _userLogic.SetActiveAsync(id, isActive);

        if (!result.Success)
            return await BackToIndexWithError(result.ErrorMessage!);

        TempData["StaffSuccess"] = isActive ? "Account reactivated." : "Account deactivated.";
        return RedirectToAction(nameof(Index));
    }

    // POST /Staff/Unlock
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(int id)
    {
        var result = await _userLogic.UnlockAsync(id);

        if (!result.Success)
            return await BackToIndexWithError(result.ErrorMessage!);

        TempData["StaffSuccess"] = "Lockout cleared — they can sign in again now.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Staff/ResetPassword/{id}
    public async Task<IActionResult> ResetPassword(int id)
    {
        var user = await _userLogic.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        return View(new ResetStaffPasswordViewModel { UserId = user.UserId, FullName = user.FullName });
    }

    // POST /Staff/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetStaffPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _userLogic.SetPasswordAsync(model.UserId, model.NewPassword);

        if (!result.Success)
        {
            model.ErrorMessage = result.ErrorMessage;
            return View(model);
        }

        TempData["StaffSuccess"] = $"Password reset for {model.FullName}.";
        return RedirectToAction(nameof(Index));
    }

    // GET /Staff/Activity — recent sign-in attempts.
    public async Task<IActionResult> Activity()
    {
        var rows = await _userLogic.GetRecentLoginActivityAsync(ActivityRowCount);

        return View(rows.Select(r => new LoginActivityRowViewModel
        {
            OccurredAt = r.OccurredAt,
            EmailAttempted = r.EmailAttempted,
            UserFullName = r.User?.FullName,
            Event = r.Event,
            IpAddress = r.IpAddress
        }).ToList());
    }

    private async Task<IActionResult> BackToIndexWithError(string message)
    {
        var viewModel = await BuildListAsync();
        viewModel.ErrorMessage = message;
        return View(nameof(Index), viewModel);
    }

    private async Task<StaffListViewModel> BuildListAsync()
    {
        var staff = await _userLogic.GetStaffAsync();
        var currentUserId = CurrentUserId();
        var now = _clock.UtcNow;

        return new StaffListViewModel
        {
            SuccessMessage = TempData["StaffSuccess"] as string,
            Staff = staff
                .OrderByDescending(u => u.IsActive)
                .ThenBy(u => u.FullName)
                .Select(u => ToRow(u, currentUserId, now))
                .ToList()
        };
    }

    private static StaffRowViewModel ToRow(User user, int? currentUserId, DateTime now) => new()
    {
        UserId = user.UserId,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive,
        LastLoginAt = user.LastLoginAt,
        IsLockedOut = user.LockedOutUntil.HasValue && user.LockedOutUntil.Value > now,
        IsCurrentUser = currentUserId.HasValue && currentUserId.Value == user.UserId
    };

    private int? CurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

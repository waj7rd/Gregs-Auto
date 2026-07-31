using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.ViewModels;

namespace Gregs_Auto.Controllers;

// How the shop runs: hours, bays, timezone, contact details.
//
// These used to live in appsettings.json, which meant only someone with access
// to the server could change them. Now the shop owns them.
[Authorize(Policy = Policies.ManageShopSettings)]
public class SettingsController : Controller
{
    private readonly IShopLogic _shopLogic;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(IShopLogic shopLogic, ILogger<SettingsController> logger)
    {
        _shopLogic = shopLogic;
        _logger = logger;
    }

    // GET /Settings
    public async Task<IActionResult> Index()
    {
        var viewModel = ToViewModel(await _shopLogic.GetAsync());
        viewModel.SuccessMessage = TempData["SettingsSuccess"] as string;
        return View(viewModel);
    }

    // POST /Settings
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ShopSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.CurrentTier = (await _shopLogic.GetAsync()).TierName;
            return View(model);
        }

        var result = await _shopLogic.UpdateSettingsAsync(new ShopSettingsUpdate
        {
            Name = model.Name,
            Phone = model.Phone,
            AddressLine = model.AddressLine,
            City = model.City,
            State = model.State,
            PostalCode = model.PostalCode,
            TimeZoneId = model.TimeZoneId,
            BayCount = model.BayCount,
            OpensAt = model.OpensAt,
            ClosesAt = model.ClosesAt,
            ClosedDays = model.ClosedDays,
        });

        if (!result.Success)
        {
            model.CurrentTier = (await _shopLogic.GetAsync()).TierName;
            model.ErrorMessage = result.ErrorMessage;
            return View(model);
        }

        // Worth a log line: these values change the booking rules for everyone,
        // and "why did bookings stop working on Saturdays" is a question you'll
        // want an answer to.
        _logger.LogInformation(
            "Shop settings updated by {User}: {Bays} bays, {Opens}-{Closes}, closed {ClosedDays}, tz {TimeZone}",
            User.Identity?.Name, model.BayCount, model.OpensAt, model.ClosesAt,
            string.Join("/", model.ClosedDays), model.TimeZoneId);

        TempData["SettingsSuccess"] = "Saved. The booking rules use these straight away.";
        return RedirectToAction(nameof(Index));
    }

    private static ShopSettingsViewModel ToViewModel(Shop shop) => new()
    {
        Name = shop.Name,
        Phone = shop.Phone,
        AddressLine = shop.AddressLine,
        City = shop.City,
        State = shop.State,
        PostalCode = shop.PostalCode,
        TimeZoneId = shop.TimeZoneId,
        BayCount = shop.BayCount,
        OpensAt = shop.OpensAt,
        ClosesAt = shop.ClosesAt,
        ClosedDays = shop.ClosedDays.ToList(),
        CurrentTier = shop.TierName,
    };
}

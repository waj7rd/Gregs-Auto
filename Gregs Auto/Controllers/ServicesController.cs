using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Gregs_Auto.Domain.Implementations.Interfaces;
using Gregs_Auto.Domain.Security;
using Gregs_Auto.ViewModels;

namespace Gregs_Auto.Controllers;

public class ServicesController : Controller
{
    private readonly IServiceLogic _serviceLogic;

    public ServicesController(IServiceLogic serviceLogic)
    {
        _serviceLogic = serviceLogic;
    }

    // GET /Services — the public price list. Archived services aren't shown.
    public async Task<IActionResult> Index()
    {
        var services = await _serviceLogic.GetActiveAsync();
        return View(services.Select(ToRow).ToList());
    }

    // GET /Services/Manage — staff catalog, archived included.
    [Authorize(Policy = Policies.ManageCustomers)]
    public async Task<IActionResult> Manage()
    {
        return View(await BuildManageViewModelAsync());
    }

    // GET /Services/Create
    [Authorize(Policy = Policies.ManageCustomers)]
    public IActionResult Create() => View(new EditServiceViewModel());

    // POST /Services/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.ManageCustomers)]
    public async Task<IActionResult> Create(EditServiceViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _serviceLogic.CreateAsync(
            model.Name, model.Description, model.EstimatedDurationMinutes, model.Price);

        if (!result.Success)
        {
            model.ErrorMessage = result.ErrorMessage;
            return View(model);
        }

        TempData["ServiceSuccess"] = $"Added {model.Name.Trim()}.";
        return RedirectToAction(nameof(Manage));
    }

    // GET /Services/Edit/{id}
    [Authorize(Policy = Policies.ManageCustomers)]
    public async Task<IActionResult> Edit(int id)
    {
        var service = await _serviceLogic.GetByIdAsync(id);
        if (service == null)
            return NotFound();

        return View(new EditServiceViewModel
        {
            ServiceId = service.ServiceId,
            Name = service.Name,
            Description = service.Description,
            EstimatedDurationMinutes = service.EstimatedDurationMinutes,
            Price = service.Price
        });
    }

    // POST /Services/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.ManageCustomers)]
    public async Task<IActionResult> Edit(EditServiceViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _serviceLogic.UpdateAsync(
            model.ServiceId, model.Name, model.Description, model.EstimatedDurationMinutes, model.Price);

        if (!result.Success)
        {
            model.ErrorMessage = result.ErrorMessage;
            return View(model);
        }

        TempData["ServiceSuccess"] = $"Updated {model.Name.Trim()}.";
        return RedirectToAction(nameof(Manage));
    }

    // POST /Services/SetActive
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.ManageCustomers)]
    public async Task<IActionResult> SetActive(int id, bool isActive)
    {
        var result = await _serviceLogic.SetActiveAsync(id, isActive);

        if (!result.Success)
        {
            var viewModel = await BuildManageViewModelAsync();
            viewModel.ErrorMessage = result.ErrorMessage;
            return View(nameof(Manage), viewModel);
        }

        TempData["ServiceSuccess"] = isActive
            ? "Service is bookable again."
            : "Service archived — it's off the booking form, and past jobs still show it.";

        return RedirectToAction(nameof(Manage));
    }

    private async Task<ServiceCatalogViewModel> BuildManageViewModelAsync()
    {
        var services = await _serviceLogic.GetAllAsync();

        return new ServiceCatalogViewModel
        {
            SuccessMessage = TempData["ServiceSuccess"] as string,
            Services = services.Select(ToRow).ToList()
        };
    }

    private static ServiceRowViewModel ToRow(Gregs_Auto.Domain.EntityModels.Service s) => new()
    {
        Id = s.ServiceId,
        Name = s.Name,
        Description = s.Description,
        EstimatedDurationMinutes = s.EstimatedDurationMinutes,
        Price = s.Price,
        IsActive = s.IsActive
    };
}

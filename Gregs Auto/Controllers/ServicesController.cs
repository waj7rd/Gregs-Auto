using Microsoft.AspNetCore.Mvc;
using Gregs_Auto.Domain.IRepositories;
using Gregs_Auto.ViewModels;

namespace Gregs_Auto.Controllers;

public class ServicesController : Controller
{
    private readonly IServiceRepository _serviceRepository;

    public ServicesController(IServiceRepository serviceRepository)
    {
        _serviceRepository = serviceRepository;
    }

    // GET /Services
    public async Task<IActionResult> Index()
    {
        var services = await _serviceRepository.GetAllAsync();

        var viewModel = services
            .OrderBy(s => s.Name)
            .Select(s => new ServiceRowViewModel
            {
                Id = s.ServiceId,
                Name = s.Name,
                Description = s.Description,
                EstimatedDurationMinutes = s.EstimatedDurationMinutes,
                Price = s.Price
            }).ToList();

        return View(viewModel);
    }
}

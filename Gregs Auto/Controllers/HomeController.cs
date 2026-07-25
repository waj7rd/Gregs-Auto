using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Gregs_Auto.Domain.IRepositories;
using Gregs_Auto.Models;
using Gregs_Auto.ViewModels;

namespace Gregs_Auto.Controllers;

public class HomeController : Controller
{
    private readonly IServiceRepository _serviceRepository;

    public HomeController(IServiceRepository serviceRepository)
    {
        _serviceRepository = serviceRepository;
    }

    // GET /
    public async Task<IActionResult> Index()
    {
        // A short teaser of the catalog — the full list lives on /Services.
        var services = await _serviceRepository.GetAllAsync();

        var featured = services
            .OrderBy(s => s.Price)
            .Take(3)
            .Select(s => new ServiceRowViewModel
            {
                Id = s.ServiceId,
                Name = s.Name,
                Description = s.Description,
                EstimatedDurationMinutes = s.EstimatedDurationMinutes,
                Price = s.Price
            }).ToList();

        return View(featured);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

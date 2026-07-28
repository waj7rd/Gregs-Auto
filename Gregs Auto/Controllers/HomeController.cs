using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Gregs_Auto.Domain.Implementations.Interfaces;
using Gregs_Auto.Models;
using Gregs_Auto.ViewModels;

namespace Gregs_Auto.Controllers;

public class HomeController : Controller
{
    private readonly IServiceLogic _serviceLogic;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IServiceLogic serviceLogic, ILogger<HomeController> logger)
    {
        _serviceLogic = serviceLogic;
        _logger = logger;
    }

    // GET /
    public async Task<IActionResult> Index()
    {
        // A short teaser of the catalog — the full list lives on /Services.
        var services = await _serviceLogic.GetActiveAsync();

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
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        // UseExceptionHandler routes here after an unhandled exception. Pull the
        // actual exception out and log it — otherwise the only record of a
        // production failure is a customer telling you the site broke.
        //
        // The request id also goes on the error page, so "it said error ABC123"
        // is enough to find the entry.
        var handler = HttpContext.Features.Get<IExceptionHandlerFeature>();
        if (handler?.Error != null)
        {
            _logger.LogError(handler.Error,
                "Unhandled exception on {Path} (request {RequestId})",
                handler.Path, requestId);
        }

        return View(new ErrorViewModel { RequestId = requestId });
    }
}

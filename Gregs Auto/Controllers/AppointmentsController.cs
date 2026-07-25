using Microsoft.AspNetCore.Mvc;
using Gregs_Auto.Domain.Implementations.Interfaces;
using Gregs_Auto.Domain.IRepositories;
using Gregs_Auto.ViewModels;

namespace Gregs_Auto.Controllers;

public class AppointmentsController : Controller
{
    private readonly IAppointmentLogic _appointmentLogic;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IServiceRepository _serviceRepository;

    public AppointmentsController(IAppointmentLogic appointmentLogic, IVehicleRepository vehicleRepository, IServiceRepository serviceRepository)
    {
        _appointmentLogic = appointmentLogic;
        _vehicleRepository = vehicleRepository;
        _serviceRepository = serviceRepository;
    }

    // GET /Appointments/Schedule
    public async Task<IActionResult> Schedule()
    {
        return View(await BuildViewModelAsync());
    }

    // POST /Appointments/Book
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(int vehicleId, int serviceId, DateTime scheduledAt, string? notes)
    {
        var result = await _appointmentLogic.BookAsync(vehicleId, serviceId, scheduledAt, notes);

        if (!result.Success)
        {
            var viewModel = await BuildViewModelAsync();
            viewModel.ErrorMessage = result.ErrorMessage;
            return View(nameof(Schedule), viewModel);
        }

        return RedirectToAction(nameof(Schedule));
    }

    private async Task<ScheduleAppointmentViewModel> BuildViewModelAsync()
    {
        var vehicles = await _vehicleRepository.GetAllWithCustomerAsync();
        var services = await _serviceRepository.GetAllAsync();
        var appointments = await _appointmentLogic.GetUpcomingAsync();

        return new ScheduleAppointmentViewModel
        {
            Vehicles = vehicles.Select(v => new VehicleOptionViewModel
            {
                Id = v.VehicleId,
                CustomerName = v.Customer.FullName,
                Description = $"{v.Year} {v.Make} {v.Model}"
            }).ToList(),
            Services = services.Select(s => new ServiceOptionViewModel
            {
                Id = s.ServiceId,
                Name = s.Name,
                Price = s.Price,
                EstimatedDurationMinutes = s.EstimatedDurationMinutes
            }).ToList(),
            UpcomingAppointments = appointments.Select(a => new AppointmentRowViewModel
            {
                Id = a.AppointmentId,
                ScheduledAt = a.ScheduledAt,
                Status = a.Status,
                CustomerName = a.Vehicle.Customer.FullName,
                VehicleDescription = $"{a.Vehicle.Year} {a.Vehicle.Make} {a.Vehicle.Model}",
                ServiceName = a.Service.Name
            }).ToList()
        };
    }
}

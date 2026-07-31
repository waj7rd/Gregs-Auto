using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Gregs_Auto.Domain.IRepositories;
using Gregs_Auto.ViewModels;

namespace Gregs_Auto.Controllers;

public class AppointmentsController : Controller
{
    private const int HandledRequestCount = 25;

    private readonly IAppointmentLogic _appointmentLogic;
    private readonly IBookingRequestLogic _bookingRequestLogic;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IServiceLogic _serviceLogic;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(
        IAppointmentLogic appointmentLogic,
        IBookingRequestLogic bookingRequestLogic,
        IVehicleRepository vehicleRepository,
        IServiceLogic serviceLogic,
        ILogger<AppointmentsController> logger)
    {
        _logger = logger;
        _appointmentLogic = appointmentLogic;
        _bookingRequestLogic = bookingRequestLogic;
        _vehicleRepository = vehicleRepository;
        _serviceLogic = serviceLogic;
    }

    // GET /Appointments/Schedule
    public async Task<IActionResult> Schedule()
    {
        return View(await BuildViewModelAsync());
    }

    // POST /Appointments/Book — staff booking directly against a known vehicle.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.ManageAppointments)]
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

    // POST /Appointments/SubmitRequest — the public form. Anonymous by design.
    // Not named Request: that would shadow ControllerBase.Request.
    //
    // This does not create an appointment. It records what the visitor asked
    // for, and a staff member turns it into one. Nothing anonymous is written
    // into the customer records.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitPolicies.PublicBooking)]
    public async Task<IActionResult> SubmitRequest(GuestBookingViewModel guest)
    {
        // Honeypot tripped: a person can't see or tab to that field, so anything
        // in it means a bot. Show the same thank-you page rather than an error —
        // telling a scraper it was caught just teaches it to try again properly.
        if (!string.IsNullOrWhiteSpace(guest.Website))
        {
            _logger.LogWarning("Booking honeypot tripped from {Ip}", HttpContext.Connection.RemoteIpAddress);
            return RedirectToAction(nameof(Requested));
        }

        if (!ModelState.IsValid)
        {
            var invalid = await BuildViewModelAsync();
            invalid.Guest = guest;
            return View(nameof(Schedule), invalid);
        }

        var result = await _bookingRequestLogic.SubmitAsync(new NewBookingRequest
        {
            CustomerName = guest.CustomerName,
            Phone = guest.Phone,
            Email = guest.Email,
            VehicleYear = guest.VehicleYear,
            VehicleMake = guest.VehicleMake,
            VehicleModel = guest.VehicleModel,
            ServiceId = guest.ServiceId,
            RequestedAt = guest.RequestedAt,
            Notes = guest.Notes
        });

        if (!result.Success)
        {
            var failed = await BuildViewModelAsync();
            failed.Guest = guest;
            failed.ErrorMessage = result.ErrorMessage;
            return View(nameof(Schedule), failed);
        }

        _logger.LogInformation("Booking request {RequestId} received from the public site", result.BookingRequestId);
        return RedirectToAction(nameof(Requested));
    }

    // GET /Appointments/Requested — the thank-you page.
    public IActionResult Requested() => View();

    // GET /Appointments/Requests — staff queue of incoming requests.
    [Authorize(Policy = Policies.ManageAppointments)]
    public async Task<IActionResult> Requests()
    {
        return View(await BuildRequestsViewModelAsync());
    }

    // POST /Appointments/AcceptRequest
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.ManageAppointments)]
    public async Task<IActionResult> AcceptRequest(int id)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue)
            return Forbid();

        var result = await _bookingRequestLogic.AcceptAsync(id, userId.Value);

        if (!result.Success)
        {
            var viewModel = await BuildRequestsViewModelAsync();
            viewModel.ErrorMessage = result.ErrorMessage;
            return View(nameof(Requests), viewModel);
        }

        _logger.LogInformation("Booking request {RequestId} accepted by user {UserId}, became appointment {AppointmentId}",
            id, userId.Value, result.AppointmentId);

        TempData["RequestSuccess"] = "Booked. The customer and vehicle are on file now.";
        return RedirectToAction(nameof(Requests));
    }

    // POST /Appointments/DeclineRequest
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.ManageAppointments)]
    public async Task<IActionResult> DeclineRequest(int id)
    {
        var userId = CurrentUserId();
        if (!userId.HasValue)
            return Forbid();

        var result = await _bookingRequestLogic.DeclineAsync(id, userId.Value);

        if (!result.Success)
        {
            var viewModel = await BuildRequestsViewModelAsync();
            viewModel.ErrorMessage = result.ErrorMessage;
            return View(nameof(Requests), viewModel);
        }

        TempData["RequestSuccess"] = "Request declined. Give them a call if they're expecting one.";
        return RedirectToAction(nameof(Requests));
    }

    // GET /Appointments/Manage — staff view of every appointment, with status actions.
    [Authorize(Policy = Policies.ManageAppointments)]
    public async Task<IActionResult> Manage()
    {
        var appointments = await _appointmentLogic.GetScheduleAsync();
        return View(appointments.Select(a => ToRowViewModel(a, includeCustomerDetail: true)).ToList());
    }

    // POST /Appointments/Start — Scheduled -> InProgress
    [Authorize(Policy = Policies.ManageAppointments)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int id)
    {
        await _appointmentLogic.StartAsync(id);
        return RedirectToAction(nameof(Manage));
    }

    // POST /Appointments/Complete — Scheduled/InProgress -> Completed
    [Authorize(Policy = Policies.ManageAppointments)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        await _appointmentLogic.CompleteAsync(id);
        return RedirectToAction(nameof(Manage));
    }

    // POST /Appointments/Cancel — Scheduled/InProgress -> Cancelled
    [Authorize(Policy = Policies.ManageAppointments)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        await _appointmentLogic.CancelAsync(id);
        return RedirectToAction(nameof(Manage));
    }

    // includeCustomerDetail is false for anonymous visitors: the customer name
    // and vehicle are never put on the model at all, so they can't leak through
    // the rendered HTML.
    private static AppointmentRowViewModel ToRowViewModel(Gregs_Auto.Domain.EntityModels.Appointment a, bool includeCustomerDetail)
    {
        return new AppointmentRowViewModel
        {
            Id = a.AppointmentId,
            ScheduledAt = a.ScheduledAt,
            Status = a.Status,
            CustomerName = includeCustomerDetail ? a.Vehicle.Customer.FullName : string.Empty,
            VehicleDescription = includeCustomerDetail
                ? $"{a.Vehicle.Year} {a.Vehicle.Make} {a.Vehicle.Model}"
                : string.Empty,
            ServiceName = a.Service.Name
        };
    }

    private async Task<ScheduleAppointmentViewModel> BuildViewModelAsync()
    {
        var includeCustomerDetail = User.Identity?.IsAuthenticated == true;

        var vehicles = (await _vehicleRepository.GetAllWithCustomerAsync())
            .Where(v => v.IsActive && v.Customer.IsActive)
            .ToList();
        var services = await _serviceLogic.GetActiveAsync();
        var appointments = await _appointmentLogic.GetUpcomingAsync();

        return new ScheduleAppointmentViewModel
        {
            ShowCustomerDetail = includeCustomerDetail,
            Vehicles = vehicles.Select(v => new VehicleOptionViewModel
            {
                Id = v.VehicleId,
                CustomerName = includeCustomerDetail ? v.Customer.FullName : string.Empty,
                Description = $"{v.Year} {v.Make} {v.Model}"
            }).ToList(),
            Services = services.Select(s => new ServiceOptionViewModel
            {
                Id = s.ServiceId,
                Name = s.Name,
                Price = s.Price,
                EstimatedDurationMinutes = s.EstimatedDurationMinutes
            }).ToList(),
            UpcomingAppointments = appointments.Select(a => ToRowViewModel(a, includeCustomerDetail)).ToList()
        };
    }

    private async Task<BookingRequestsViewModel> BuildRequestsViewModelAsync()
    {
        var pending = await _bookingRequestLogic.GetPendingAsync();
        var handled = await _bookingRequestLogic.GetRecentlyHandledAsync(HandledRequestCount);

        return new BookingRequestsViewModel
        {
            SuccessMessage = TempData["RequestSuccess"] as string,
            Pending = pending.Select(ToRequestRow).ToList(),
            RecentlyHandled = handled.Select(ToRequestRow).ToList()
        };
    }

    private static BookingRequestRowViewModel ToRequestRow(Gregs_Auto.Domain.EntityModels.BookingRequest r) => new()
    {
        Id = r.BookingRequestId,
        CustomerName = r.CustomerName,
        Phone = r.Phone,
        Email = r.Email,
        VehicleDescription = r.VehicleDescription,
        ServiceName = r.Service?.Name ?? "—",
        RequestedAt = r.RequestedAt,
        Notes = r.Notes,
        CreatedAt = r.CreatedAt,
        Status = r.Status,
        HandledByName = r.HandledByUser?.FullName,
        HandledAt = r.HandledAt
    };

    private int? CurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

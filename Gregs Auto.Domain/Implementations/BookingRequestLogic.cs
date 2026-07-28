using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.Implementations.Interfaces;
using Gregs_Auto.Domain.IRepositories;

namespace Gregs_Auto.Domain.Implementations;

public class BookingRequestLogic : IBookingRequestLogic
{
    private readonly IBookingRequestRepository _requestRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IAppointmentLogic _appointmentLogic;
    private readonly IShopClock _clock;
    private readonly IShopSettings _settings;

    public BookingRequestLogic(
        IBookingRequestRepository requestRepository,
        ICustomerRepository customerRepository,
        IVehicleRepository vehicleRepository,
        IServiceRepository serviceRepository,
        IAppointmentLogic appointmentLogic,
        IShopClock clock,
        IShopSettings settings)
    {
        _requestRepository = requestRepository;
        _customerRepository = customerRepository;
        _vehicleRepository = vehicleRepository;
        _serviceRepository = serviceRepository;
        _appointmentLogic = appointmentLogic;
        _clock = clock;
        _settings = settings;
    }

    public async Task<BookingRequestResult> SubmitAsync(NewBookingRequest request)
    {
        if (request.RequestedAt <= _clock.LocalNow)
            return BookingRequestResult.Fail("Please pick a time in the future.");

        var service = await _serviceRepository.GetAsync(s => s.ServiceId == request.ServiceId);
        if (service == null || !service.IsActive)
            return BookingRequestResult.Fail("Please choose a service from the list.");

        var outsideHours = ShopHours.Check(_settings, request.RequestedAt, service.EstimatedDurationMinutes);
        if (outsideHours != null)
            return BookingRequestResult.Fail(outsideHours);

        var entity = new BookingRequest
        {
            CustomerName = request.CustomerName.Trim(),
            Phone = request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            VehicleYear = request.VehicleYear,
            VehicleMake = request.VehicleMake.Trim(),
            VehicleModel = request.VehicleModel.Trim(),
            ServiceId = request.ServiceId,
            RequestedAt = request.RequestedAt,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Status = BookingRequestStatus.Pending,
            CreatedAt = _clock.UtcNow
        };

        await _requestRepository.AddAsync(entity);
        await _requestRepository.SaveChangesAsync();

        return BookingRequestResult.Ok(entity.BookingRequestId);
    }

    public async Task<IList<BookingRequest>> GetPendingAsync() =>
        await _requestRepository.GetPendingAsync();

    public async Task<IList<BookingRequest>> GetRecentlyHandledAsync(int count) =>
        await _requestRepository.GetRecentAsync(count);

    public async Task<BookingRequestResult> AcceptAsync(int bookingRequestId, int handledByUserId)
    {
        var request = await _requestRepository.GetWithDetailsAsync(bookingRequestId);
        if (request == null)
            return BookingRequestResult.Fail("That request no longer exists.");

        if (request.Status != BookingRequestStatus.Pending)
            return BookingRequestResult.Fail("That request has already been dealt with.");

        // Match an existing customer on phone number. It's the closest thing a
        // shop has to a customer key, and it's what the visitor typed. A near
        // match is left alone deliberately — merging the wrong two customers is
        // worse than having a duplicate a staff member can tidy up.
        var phone = request.Phone.Trim();
        var customer = await _customerRepository.GetAsync(c => c.Phone == phone);

        if (customer == null)
        {
            customer = new Customer
            {
                FullName = request.CustomerName,
                Email = request.Email,
                Phone = phone,
                CreatedAt = _clock.UtcNow
            };

            await _customerRepository.AddAsync(customer);
            await _customerRepository.SaveChangesAsync();
        }

        // Same idea for the vehicle: same owner, same year/make/model is treated
        // as the same car. No VIN is collected on the public form, so this is
        // the best available match.
        var vehicle = await _vehicleRepository.GetAsync(v =>
            v.CustomerId == customer.CustomerId &&
            v.Year == request.VehicleYear &&
            v.Make == request.VehicleMake &&
            v.Model == request.VehicleModel);

        if (vehicle == null)
        {
            vehicle = new Vehicle
            {
                CustomerId = customer.CustomerId,
                Year = request.VehicleYear,
                Make = request.VehicleMake,
                Model = request.VehicleModel,
                CreatedAt = _clock.UtcNow
            };

            await _vehicleRepository.AddAsync(vehicle);
            await _vehicleRepository.SaveChangesAsync();
        }

        // Reuse the existing booking rules rather than a second copy of them.
        var booking = await _appointmentLogic.BookAsync(
            vehicle.VehicleId, request.ServiceId, request.RequestedAt, request.Notes);

        if (!booking.Success)
            return BookingRequestResult.Fail(booking.ErrorMessage!);

        request.Status = BookingRequestStatus.Accepted;
        request.HandledByUserId = handledByUserId;
        request.HandledAt = _clock.UtcNow;
        request.AppointmentId = booking.AppointmentId;

        await _requestRepository.SaveChangesAsync();

        return BookingRequestResult.Ok(request.BookingRequestId, booking.AppointmentId);
    }

    public async Task<BookingRequestResult> DeclineAsync(int bookingRequestId, int handledByUserId)
    {
        var request = await _requestRepository.GetAsync(r => r.BookingRequestId == bookingRequestId);
        if (request == null)
            return BookingRequestResult.Fail("That request no longer exists.");

        if (request.Status != BookingRequestStatus.Pending)
            return BookingRequestResult.Fail("That request has already been dealt with.");

        request.Status = BookingRequestStatus.Declined;
        request.HandledByUserId = handledByUserId;
        request.HandledAt = _clock.UtcNow;

        await _requestRepository.SaveChangesAsync();

        return BookingRequestResult.Ok(request.BookingRequestId);
    }
}

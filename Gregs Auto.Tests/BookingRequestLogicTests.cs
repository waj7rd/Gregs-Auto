using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Tests.Fakes;

namespace Gregs_Auto.Tests;

public class BookingRequestLogicTests
{
    private const int OilChangeId = 1;
    private const int StaffUserId = 7;

    private readonly FakeBookingRequestRepository _requests = new();
    private readonly FakeCustomerRepository _customers = new();
    private readonly FakeVehicleRepository _vehicles = new();
    private readonly FakeServiceRepository _services = new();
    private readonly FakeAppointmentRepository _appointments = new();
    private readonly TestClock _clock = new();
    private readonly TestUnitOfWork _unitOfWork = new();

    public BookingRequestLogicTests()
    {
        _services.Seed(new Service
        {
            ServiceId = OilChangeId,
            Name = "Oil Change",
            EstimatedDurationMinutes = 30
        });
    }

    private BookingRequestLogic Logic()
    {
        var settings = new TestShopSettings(3);
        var appointmentLogic = new AppointmentLogic(
            _appointments, _vehicles, _services, _clock, settings);

        return new BookingRequestLogic(
            _requests, _customers, _vehicles, _services, appointmentLogic, _clock, settings, _unitOfWork);
    }

    private static DateTime Tomorrow(int hour) => new DateTime(2026, 7, 16, hour, 0, 0);

    private static NewBookingRequest Request(string phone = "636-555-0142", string name = "Ellen Brady") => new()
    {
        CustomerName = name,
        Phone = phone,
        Email = "ellen@example.com",
        VehicleYear = 2016,
        VehicleMake = "Jeep",
        VehicleModel = "Cherokee",
        ServiceId = OilChangeId,
        RequestedAt = Tomorrow(10),
        Notes = "Grinding noise when braking"
    };

    // ---------- submitting ----------

    [Fact]
    public async Task Records_a_request_without_creating_a_customer()
    {
        var result = await Logic().SubmitAsync(Request());

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Single(_requests.All);

        // The whole point: anonymous input doesn't reach the customer records.
        Assert.Empty(await _customers.GetAllAsync());
        Assert.Empty(await _vehicles.GetAllAsync());
        Assert.Empty(await _appointments.GetAllAsync());
    }

    [Fact]
    public async Task Refuses_a_time_in_the_past()
    {
        var request = Request();
        request.RequestedAt = new DateTime(2026, 7, 15, 8, 0, 0);

        var result = await Logic().SubmitAsync(request);

        Assert.False(result.Success);
        Assert.Empty(_requests.All);
    }

    [Fact]
    public async Task Refuses_a_service_that_doesnt_exist()
    {
        var request = Request();
        request.ServiceId = 999;

        var result = await Logic().SubmitAsync(request);

        Assert.False(result.Success);
        Assert.Empty(_requests.All);
    }

    [Fact]
    public async Task A_new_request_starts_pending()
    {
        await Logic().SubmitAsync(Request());

        Assert.Equal(BookingRequestStatus.Pending, _requests.All[0].Status);
        Assert.Single(await Logic().GetPendingAsync());
    }

    // ---------- accepting ----------

    [Fact]
    public async Task Accepting_creates_the_customer_vehicle_and_appointment()
    {
        await Logic().SubmitAsync(Request());
        var requestId = _requests.All[0].BookingRequestId;

        var result = await Logic().AcceptAsync(requestId, StaffUserId);

        Assert.True(result.Success, result.ErrorMessage);

        var customer = Assert.Single(await _customers.GetAllAsync());
        Assert.Equal("Ellen Brady", customer.FullName);

        var vehicle = Assert.Single(await _vehicles.GetAllAsync());
        Assert.Equal("Jeep", vehicle.Make);
        Assert.Equal(customer.CustomerId, vehicle.CustomerId);

        var appointment = Assert.Single(await _appointments.GetAllAsync());
        Assert.Equal(Tomorrow(10), appointment.ScheduledAt);
        Assert.Equal("Grinding noise when braking", appointment.Notes);
    }

    [Fact]
    public async Task Accepting_records_who_did_it_and_what_it_became()
    {
        await Logic().SubmitAsync(Request());
        var request = _requests.All[0];

        await Logic().AcceptAsync(request.BookingRequestId, StaffUserId);

        Assert.Equal(BookingRequestStatus.Accepted, request.Status);
        Assert.Equal(StaffUserId, request.HandledByUserId);
        Assert.Equal(_clock.UtcNow, request.HandledAt);
        Assert.NotNull(request.AppointmentId);
    }

    [Fact]
    public async Task A_returning_customer_is_matched_on_phone_rather_than_duplicated()
    {
        _customers.Seed(new Customer
        {
            CustomerId = 1,
            FullName = "Ellen Brady",
            Phone = "636-555-0142"
        });

        await Logic().SubmitAsync(Request());
        await Logic().AcceptAsync(_requests.All[0].BookingRequestId, StaffUserId);

        Assert.Single(await _customers.GetAllAsync());
    }

    [Fact]
    public async Task A_different_phone_number_makes_a_different_customer()
    {
        _customers.Seed(new Customer { CustomerId = 1, FullName = "Ellen Brady", Phone = "636-555-9999" });

        await Logic().SubmitAsync(Request(phone: "636-555-0142"));
        await Logic().AcceptAsync(_requests.All[0].BookingRequestId, StaffUserId);

        Assert.Equal(2, (await _customers.GetAllAsync()).Count);
    }

    [Fact]
    public async Task The_same_car_twice_is_not_added_twice()
    {
        await Logic().SubmitAsync(Request());
        await Logic().AcceptAsync(_requests.All[0].BookingRequestId, StaffUserId);

        var second = Request();
        second.RequestedAt = Tomorrow(14);
        await Logic().SubmitAsync(second);
        await Logic().AcceptAsync(_requests.All[1].BookingRequestId, StaffUserId);

        Assert.Single(await _customers.GetAllAsync());
        Assert.Single(await _vehicles.GetAllAsync());
        Assert.Equal(2, (await _appointments.GetAllAsync()).Count);
    }

    [Fact]
    public async Task Accepting_respects_the_booking_rules()
    {
        // Fill all three bays at 10:00 tomorrow.
        for (var i = 1; i <= 3; i++)
        {
            _vehicles.Seed(new Vehicle { VehicleId = 100 + i });
            _appointments.Seed(new Appointment
            {
                VehicleId = 100 + i,
                ServiceId = OilChangeId,
                ScheduledAt = Tomorrow(10),
                Status = AppointmentStatus.Scheduled,
                Service = _services.GetAll().First(),
                DurationMinutes = 30,
                Price = 49.99m
            });
        }

        await Logic().SubmitAsync(Request());
        var result = await Logic().AcceptAsync(_requests.All[0].BookingRequestId, StaffUserId);

        Assert.False(result.Success);
        Assert.Contains("bays", result.ErrorMessage);

        // Still pending, so staff can pick a different time rather than losing it.
        Assert.Equal(BookingRequestStatus.Pending, _requests.All[0].Status);
    }

    [Fact]
    public async Task A_request_cannot_be_accepted_twice()
    {
        await Logic().SubmitAsync(Request());
        var id = _requests.All[0].BookingRequestId;

        await Logic().AcceptAsync(id, StaffUserId);
        var again = await Logic().AcceptAsync(id, StaffUserId);

        Assert.False(again.Success);
        Assert.Contains("already been dealt with", again.ErrorMessage);
        Assert.Single(await _appointments.GetAllAsync());
    }

    // ---------- declining ----------

    [Fact]
    public async Task Declining_creates_nothing()
    {
        await Logic().SubmitAsync(Request());
        var request = _requests.All[0];

        var result = await Logic().DeclineAsync(request.BookingRequestId, StaffUserId);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(BookingRequestStatus.Declined, request.Status);
        Assert.Equal(StaffUserId, request.HandledByUserId);
        Assert.Empty(await _customers.GetAllAsync());
        Assert.Empty(await _appointments.GetAllAsync());
    }

    [Fact]
    public async Task A_declined_request_leaves_the_pending_queue()
    {
        await Logic().SubmitAsync(Request());
        await Logic().DeclineAsync(_requests.All[0].BookingRequestId, StaffUserId);

        Assert.Empty(await Logic().GetPendingAsync());
        Assert.Single(await Logic().GetRecentlyHandledAsync(10));
    }

    // ---------- transactional behaviour ----------

    [Fact]
    public async Task Accepting_runs_inside_a_unit_of_work()
    {
        await Logic().SubmitAsync(Request());

        await Logic().AcceptAsync(_requests.All[0].BookingRequestId, StaffUserId);

        Assert.Equal(1, _unitOfWork.Executions);
    }

    [Fact]
    public async Task A_refused_accept_is_marked_for_rollback()
    {
        // Fill the bays so the booking step refuses after the customer and
        // vehicle have been created. Against a real database the transaction
        // rolls back and neither row survives; here we assert the logic layer
        // signalled that it should.
        for (var i = 1; i <= 3; i++)
        {
            _vehicles.Seed(new Vehicle { VehicleId = 200 + i });
            _appointments.Seed(new Appointment
            {
                VehicleId = 200 + i,
                ServiceId = OilChangeId,
                ScheduledAt = Tomorrow(10),
                Status = AppointmentStatus.Scheduled,
                Service = _services.GetAll().First(),
                DurationMinutes = 30,
                Price = 49.99m
            });
        }

        await Logic().SubmitAsync(Request());
        var result = await Logic().AcceptAsync(_requests.All[0].BookingRequestId, StaffUserId);

        Assert.False(result.Success);
        Assert.True(_unitOfWork.LastWouldRollBack);
    }

    [Fact]
    public async Task A_successful_accept_is_not_marked_for_rollback()
    {
        await Logic().SubmitAsync(Request());

        var result = await Logic().AcceptAsync(_requests.All[0].BookingRequestId, StaffUserId);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.False(_unitOfWork.LastWouldRollBack);
    }
}

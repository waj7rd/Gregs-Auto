using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Tests.Fakes;

namespace Gregs_Auto.Tests;

public class ShopHoursTests
{
    // 8am–5pm, closed Sundays.
    private static readonly TestShopSettings Weekdays = new(
        bayCount: 3,
        opensAt: new TimeOnly(8, 0),
        closesAt: new TimeOnly(17, 0),
        closedDays: new[] { DayOfWeek.Sunday });

    // 2026-07-15 is a Wednesday; 2026-07-19 is a Sunday.
    private static DateTime Wednesday(int hour, int minute = 0) => new(2026, 7, 15, hour, minute, 0);
    private static DateTime Sunday(int hour) => new(2026, 7, 19, hour, 0, 0);

    [Fact]
    public void Mid_morning_is_fine()
    {
        Assert.Null(ShopHours.Check(Weekdays, Wednesday(10), 30));
    }

    [Fact]
    public void Before_opening_is_refused()
    {
        var reason = ShopHours.Check(Weekdays, Wednesday(7), 30);

        Assert.NotNull(reason);
        Assert.Contains("open", reason);
    }

    [Fact]
    public void Three_in_the_morning_is_refused()
    {
        Assert.NotNull(ShopHours.Check(Weekdays, Wednesday(3), 30));
    }

    [Fact]
    public void Closing_time_itself_is_too_late_to_start()
    {
        Assert.NotNull(ShopHours.Check(Weekdays, Wednesday(17), 30));
    }

    [Fact]
    public void A_closed_day_is_refused_whatever_the_hour()
    {
        var reason = ShopHours.Check(Weekdays, Sunday(10), 30);

        Assert.NotNull(reason);
        Assert.Contains("Sunday", reason);
    }

    [Fact]
    public void The_whole_job_has_to_fit_before_closing()
    {
        // 4:30pm start, 90-minute job, shop shuts at 5. The start is inside
        // hours but the work isn't finished — which is the case a naive
        // "is the start time in range" check would wave through.
        var reason = ShopHours.Check(Weekdays, Wednesday(16, 30), 90);

        Assert.NotNull(reason);
        Assert.Contains("closing", reason);
    }

    [Fact]
    public void A_job_finishing_exactly_at_closing_is_fine()
    {
        Assert.Null(ShopHours.Check(Weekdays, Wednesday(16, 30), 30));
    }

    [Fact]
    public void A_job_that_would_run_past_midnight_is_refused()
    {
        var lateNight = new TestShopSettings(
            opensAt: new TimeOnly(0, 0),
            closesAt: new TimeOnly(23, 59));

        Assert.NotNull(ShopHours.Check(lateNight, Wednesday(23, 30), 120));
    }
}

// The rules have to hold at the point of booking, not just in the helper.
public class BookingHoursIntegrationTests
{
    private readonly FakeAppointmentRepository _appointments = new();
    private readonly FakeVehicleRepository _vehicles = new();
    private readonly FakeServiceRepository _services = new();
    private readonly TestClock _clock = new();

    public BookingHoursIntegrationTests()
    {
        _services.Seed(new Service { ServiceId = 1, Name = "Oil Change", EstimatedDurationMinutes = 30 });
        _vehicles.Seed(new Vehicle { VehicleId = 1 });
    }

    private AppointmentLogic Logic() => new(_appointments, _vehicles, _services, _clock,
        new TestShopSettings(
            opensAt: new TimeOnly(8, 0),
            closesAt: new TimeOnly(17, 0),
            closedDays: new[] { DayOfWeek.Sunday }));

    [Fact]
    public async Task Booking_outside_hours_is_refused()
    {
        var result = await Logic().BookAsync(1, 1, new DateTime(2026, 7, 15, 22, 0, 0), null);

        Assert.False(result.Success);
        Assert.Contains("open", result.ErrorMessage);
    }

    [Fact]
    public async Task Booking_on_a_closed_day_is_refused()
    {
        var result = await Logic().BookAsync(1, 1, new DateTime(2026, 7, 19, 10, 0, 0), null);

        Assert.False(result.Success);
        Assert.Contains("Sunday", result.ErrorMessage);
    }

    [Fact]
    public async Task Booking_inside_hours_still_works()
    {
        var result = await Logic().BookAsync(1, 1, new DateTime(2026, 7, 15, 14, 0, 0), null);

        Assert.True(result.Success, result.ErrorMessage);
    }

    [Fact]
    public async Task An_archived_service_cannot_be_booked()
    {
        _services.Seed(new Service
        {
            ServiceId = 2,
            Name = "Retired Service",
            EstimatedDurationMinutes = 30,
            IsActive = false
        });

        var result = await Logic().BookAsync(1, 2, new DateTime(2026, 7, 15, 14, 0, 0), null);

        Assert.False(result.Success);
        Assert.Contains("isn't offered", result.ErrorMessage);
    }
}

// Archived records stop being bookable, but only because the logic says so —
// not because a dropdown happened to leave them out.
public class ArchivingTests
{
    private readonly FakeAppointmentRepository _appointments = new();
    private readonly FakeVehicleRepository _vehicles = new();
    private readonly FakeServiceRepository _services = new();
    private readonly TestClock _clock = new();

    public ArchivingTests()
    {
        _services.Seed(new Service { ServiceId = 1, Name = "Oil Change", EstimatedDurationMinutes = 30 });
    }

    private AppointmentLogic Logic() =>
        new(_appointments, _vehicles, _services, _clock, new TestShopSettings());

    private static DateTime Later() => new(2026, 7, 15, 14, 0, 0);

    [Fact]
    public async Task An_archived_vehicle_cannot_be_booked()
    {
        _vehicles.Seed(new Vehicle { VehicleId = 1, IsActive = false });

        var result = await Logic().BookAsync(1, 1, Later(), null);

        Assert.False(result.Success);
        Assert.Contains("archived", result.ErrorMessage);
    }

    [Fact]
    public async Task An_active_vehicle_still_books_fine()
    {
        _vehicles.Seed(new Vehicle { VehicleId = 1, IsActive = true });

        var result = await Logic().BookAsync(1, 1, Later(), null);

        Assert.True(result.Success, result.ErrorMessage);
    }

    [Fact]
    public async Task Archived_records_keep_their_appointment_history()
    {
        _vehicles.Seed(new Vehicle { VehicleId = 1, IsActive = true });
        await Logic().BookAsync(1, 1, Later(), null);

        // Archiving after the fact must not remove what already happened.
        _vehicles.GetAll().First().IsActive = false;

        Assert.Single(await Logic().GetScheduleAsync());
    }
}

using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Tests.Fakes;

namespace Gregs_Auto.Tests;

public class AppointmentLogicTests
{
    private const int OilChangeId = 1;      // 30 minutes
    private const int BrakeJobId = 2;       // 90 minutes
    private const int VehicleA = 1;
    private const int VehicleB = 2;
    private const int VehicleC = 3;
    private const int VehicleD = 4;

    private readonly FakeAppointmentRepository _appointments = new();
    private readonly FakeVehicleRepository _vehicles = new();
    private readonly FakeServiceRepository _services = new();
    private readonly TestClock _clock = new();

    public AppointmentLogicTests()
    {
        _services.Seed(
            new Service { ServiceId = OilChangeId, Name = "Oil Change", EstimatedDurationMinutes = 30 },
            new Service { ServiceId = BrakeJobId, Name = "Brake Pad Replacement", EstimatedDurationMinutes = 90 });

        _vehicles.Seed(
            new Vehicle { VehicleId = VehicleA },
            new Vehicle { VehicleId = VehicleB },
            new Vehicle { VehicleId = VehicleC },
            new Vehicle { VehicleId = VehicleD });
    }

    private AppointmentLogic Logic(int bayCount = 3) =>
        new(_appointments, _vehicles, _services, _clock, new TestShopSettings(bayCount));

    // Shop-local wall time, relative to the pinned "now" of 9am.
    private static DateTime Today(int hour, int minute = 0) =>
        new(2026, 7, 15, hour, minute, 0);

    private void Existing(int vehicleId, int serviceId, DateTime at, string status = AppointmentStatus.Scheduled)
    {
        var service = _services.GetAll().First(s => s.ServiceId == serviceId);
        _appointments.Seed(new Appointment
        {
            VehicleId = vehicleId,
            ServiceId = serviceId,
            ScheduledAt = at,
            Status = status,
            Service = service,

            // Mirrors what BookAsync does — the snapshot is what the overlap
            // rules read, so a fixture without it occupies no time at all.
            Price = service.Price,
            DurationMinutes = service.EstimatedDurationMinutes
        });
    }

    // ---------- time ----------

    [Fact]
    public async Task Refuses_a_time_in_the_past()
    {
        var result = await Logic().BookAsync(VehicleA, OilChangeId, Today(8), null);

        Assert.False(result.Success);
        Assert.Contains("future", result.ErrorMessage);
    }

    [Fact]
    public async Task Compares_against_shop_local_time_not_utc()
    {
        // 11am at the shop is 4pm UTC. Judged against UTC this would look like
        // the past; against the shop's own clock it's two hours away.
        var result = await Logic().BookAsync(VehicleA, OilChangeId, Today(11), null);

        Assert.True(result.Success, result.ErrorMessage);
    }

    // ---------- overlap ----------

    [Fact]
    public async Task Allows_the_same_vehicle_at_a_genuinely_different_time()
    {
        Existing(VehicleA, OilChangeId, Today(10));

        var result = await Logic().BookAsync(VehicleA, OilChangeId, Today(13), null);

        Assert.True(result.Success, result.ErrorMessage);
    }

    [Fact]
    public async Task Refuses_the_same_vehicle_overlapping_a_longer_job()
    {
        // 90-minute brake job at 10:00 runs to 11:30.
        Existing(VehicleA, BrakeJobId, Today(10));

        var result = await Logic().BookAsync(VehicleA, OilChangeId, Today(10, 15), null);

        Assert.False(result.Success);
        Assert.Contains("already booked", result.ErrorMessage);
    }

    [Fact]
    public async Task Refuses_when_the_new_job_runs_into_an_existing_one()
    {
        // Existing oil change at 11:00–11:30. A 90-minute job at 10:00 would
        // run to 11:30 and collide, even though it starts first.
        Existing(VehicleA, OilChangeId, Today(11));

        var result = await Logic().BookAsync(VehicleA, BrakeJobId, Today(10), null);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Allows_a_booking_that_starts_exactly_when_another_ends()
    {
        // 10:00 + 90 minutes ends at 11:30. Half-open intervals, so 11:30 is free.
        Existing(VehicleA, BrakeJobId, Today(10));

        var result = await Logic().BookAsync(VehicleA, OilChangeId, Today(11, 30), null);

        Assert.True(result.Success, result.ErrorMessage);
    }

    [Fact]
    public async Task Ignores_cancelled_appointments_when_checking_overlap()
    {
        Existing(VehicleA, BrakeJobId, Today(10), AppointmentStatus.Cancelled);

        var result = await Logic().BookAsync(VehicleA, OilChangeId, Today(10, 15), null);

        Assert.True(result.Success, result.ErrorMessage);
    }

    // ---------- bay capacity ----------

    [Fact]
    public async Task Fills_every_bay_then_refuses_the_next()
    {
        var logic = Logic(bayCount: 3);

        // No Existing() calls here — BookAsync writes into the same fake, so
        // seeding as well would put six appointments in three bays. That used
        // to pass only because a booked appointment had no Service navigation
        // and so counted as zero minutes; now the snapshot makes it real.
        Assert.True((await logic.BookAsync(VehicleA, OilChangeId, Today(14), null)).Success);
        Assert.True((await logic.BookAsync(VehicleB, OilChangeId, Today(14), null)).Success);
        Assert.True((await logic.BookAsync(VehicleC, OilChangeId, Today(14), null)).Success);

        var fourth = await logic.BookAsync(VehicleD, OilChangeId, Today(14), null);

        Assert.False(fourth.Success);
        Assert.Contains("3 bays", fourth.ErrorMessage);
    }

    [Fact]
    public async Task A_one_bay_shop_takes_one_car_at_a_time()
    {
        Existing(VehicleA, OilChangeId, Today(14));

        var result = await Logic(bayCount: 1).BookAsync(VehicleB, OilChangeId, Today(14), null);

        Assert.False(result.Success);
        Assert.Contains("already booked", result.ErrorMessage);
    }

    [Fact]
    public async Task Capacity_only_counts_jobs_that_actually_overlap()
    {
        // Three earlier jobs, all finished well before 14:00.
        Existing(VehicleA, OilChangeId, Today(10));
        Existing(VehicleB, OilChangeId, Today(10));
        Existing(VehicleC, OilChangeId, Today(10));

        var result = await Logic(bayCount: 3).BookAsync(VehicleD, OilChangeId, Today(14), null);

        Assert.True(result.Success, result.ErrorMessage);
    }

    // ---------- lookups ----------

    [Fact]
    public async Task Refuses_an_unknown_vehicle()
    {
        var result = await Logic().BookAsync(vehicleId: 999, OilChangeId, Today(14), null);

        Assert.False(result.Success);
        Assert.Contains("Vehicle not found", result.ErrorMessage);
    }

    [Fact]
    public async Task Refuses_an_unknown_service()
    {
        var result = await Logic().BookAsync(VehicleA, serviceId: 999, Today(14), null);

        Assert.False(result.Success);
        Assert.Contains("Service not found", result.ErrorMessage);
    }

    // ---------- status transitions ----------

    [Fact]
    public async Task Starting_moves_a_scheduled_job_to_in_progress()
    {
        Existing(VehicleA, OilChangeId, Today(14));
        var id = _appointments.GetAll().First().AppointmentId;

        await Logic().StartAsync(id);

        Assert.Equal(AppointmentStatus.InProgress, _appointments.GetAll().First().Status);
    }

    [Fact]
    public async Task A_completed_job_cannot_be_completed_again()
    {
        Existing(VehicleA, OilChangeId, Today(14), AppointmentStatus.Completed);
        var appointment = _appointments.GetAll().First();

        await Logic().CancelAsync(appointment.AppointmentId);

        // Still Completed — cancelling a finished job must not rewrite it.
        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
    }

    [Fact]
    public async Task A_cancelled_job_cannot_be_started()
    {
        Existing(VehicleA, OilChangeId, Today(14), AppointmentStatus.Cancelled);
        var appointment = _appointments.GetAll().First();

        await Logic().StartAsync(appointment.AppointmentId);

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
    }

    // ---------- listing ----------

    [Fact]
    public async Task Upcoming_covers_all_of_today_but_not_yesterday()
    {
        Existing(VehicleA, OilChangeId, Today(8));                        // earlier today
        Existing(VehicleB, OilChangeId, Today(8).AddDays(-1));            // yesterday
        Existing(VehicleC, OilChangeId, Today(14));                       // later today

        var upcoming = await Logic().GetUpcomingAsync();

        // Someone booking at 4pm still needs to see this morning was taken.
        Assert.Equal(2, upcoming.Count);
        Assert.DoesNotContain(upcoming, a => a.ScheduledAt < Today(0));
    }

    [Fact]
    public async Task Upcoming_leaves_out_cancelled_but_the_staff_board_keeps_them()
    {
        Existing(VehicleA, OilChangeId, Today(14), AppointmentStatus.Cancelled);
        Existing(VehicleB, OilChangeId, Today(15));

        Assert.Single(await Logic().GetUpcomingAsync());
        Assert.Equal(2, (await Logic().GetScheduleAsync()).Count);
    }
}

// Changing the catalogue must not rewrite what has already been booked. This is
// the whole reason price and duration are copied onto the appointment.
public class PriceSnapshotTests
{
    private const int OilChangeId = 1;
    private const int VehicleA = 1;
    private const int VehicleB = 2;

    private readonly FakeAppointmentRepository _appointments = new();
    private readonly FakeVehicleRepository _vehicles = new();
    private readonly FakeServiceRepository _services = new();
    private readonly TestClock _clock = new();
    private readonly Service _oilChange;

    public PriceSnapshotTests()
    {
        _oilChange = new Service
        {
            ServiceId = OilChangeId,
            Name = "Oil Change",
            EstimatedDurationMinutes = 30,
            Price = 49.99m
        };

        _services.Seed(_oilChange);
        _vehicles.Seed(new Vehicle { VehicleId = VehicleA }, new Vehicle { VehicleId = VehicleB });
    }

    private AppointmentLogic Logic(int bayCount = 3) =>
        new(_appointments, _vehicles, _services, _clock, new TestShopSettings(bayCount));

    private static DateTime At(int hour, int minute = 0) => new(2026, 7, 15, hour, minute, 0);

    [Fact]
    public async Task Booking_records_the_price_and_duration_of_the_day()
    {
        await Logic().BookAsync(VehicleA, OilChangeId, At(14), null);

        var booked = _appointments.GetAll().Single();
        Assert.Equal(49.99m, booked.Price);
        Assert.Equal(30, booked.DurationMinutes);
    }

    [Fact]
    public async Task Raising_the_price_does_not_restate_a_booked_job()
    {
        await Logic().BookAsync(VehicleA, OilChangeId, At(14), null);

        _oilChange.Price = 54.99m;   // the shop puts its prices up

        Assert.Equal(49.99m, _appointments.GetAll().Single().Price);
    }

    [Fact]
    public async Task Lengthening_a_service_does_not_reshuffle_an_existing_booking()
    {
        // A 30-minute job at 14:00 occupies 14:00-14:30, so 14:30 is free.
        await Logic().BookAsync(VehicleA, OilChangeId, At(14), null);

        // The shop decides oil changes really take 90 minutes.
        _oilChange.EstimatedDurationMinutes = 90;

        // The existing booking still occupies only its original half hour, so a
        // one-bay shop can still take the 14:30 slot. Reading the catalogue
        // instead would have made 14:00 run to 15:30 and refused this.
        var result = await Logic(bayCount: 1).BookAsync(VehicleB, OilChangeId, At(14, 30), null);

        Assert.True(result.Success, result.ErrorMessage);
    }

    [Fact]
    public async Task A_new_booking_picks_up_the_new_price()
    {
        // 10:00, not 9:00 — the pinned clock is 9:00, so a 9:00 booking isn't
        // in the future and would be refused before price came into it.
        await Logic().BookAsync(VehicleA, OilChangeId, At(10), null);
        _oilChange.Price = 54.99m;
        await Logic().BookAsync(VehicleB, OilChangeId, At(14), null);

        var prices = _appointments.GetAll().OrderBy(a => a.ScheduledAt).Select(a => a.Price).ToList();

        Assert.Equal(new[] { 49.99m, 54.99m }, prices);
    }

    [Fact]
    public async Task EndsAt_comes_from_the_snapshot()
    {
        await Logic().BookAsync(VehicleA, OilChangeId, At(14), null);
        _oilChange.EstimatedDurationMinutes = 240;

        Assert.Equal(At(14, 30), _appointments.GetAll().Single().EndsAt);
    }
}

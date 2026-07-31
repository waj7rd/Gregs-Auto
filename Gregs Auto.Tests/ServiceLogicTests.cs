using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Tests.Fakes;

namespace Gregs_Auto.Tests;

public class ServiceLogicTests
{
    private readonly FakeServiceRepository _services = new();

    private ServiceLogic Logic() => new(_services);

    private Service Given(string name, bool active = true, decimal price = 49.99m, int minutes = 30)
    {
        var service = new Service
        {
            Name = name,
            EstimatedDurationMinutes = minutes,
            Price = price,
            IsActive = active
        };

        _services.Seed(service);
        service.ServiceId = _services.GetAll().Count();
        return service;
    }

    [Fact]
    public async Task Creates_a_service()
    {
        var result = await Logic().CreateAsync("Wheel Alignment", "Four-wheel alignment", 60, 89.99m);

        Assert.True(result.Success, result.ErrorMessage);
        var created = Assert.Single(await _services.GetAllAsync());
        Assert.Equal("Wheel Alignment", created.Name);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task Refuses_a_duplicate_name()
    {
        Given("Oil Change");

        var result = await Logic().CreateAsync("Oil Change", null, 30, 49.99m);

        Assert.False(result.Success);
        Assert.Contains("already a service", result.ErrorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(481)]
    public async Task Refuses_an_implausible_duration(int minutes)
    {
        // A wrong duration quietly breaks the overlap rules, so it's worth
        // refusing rather than storing.
        var result = await Logic().CreateAsync("Odd Job", null, minutes, 10m);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Refuses_a_negative_price()
    {
        var result = await Logic().CreateAsync("Refund Special", null, 30, -5m);

        Assert.False(result.Success);
        Assert.Contains("negative", result.ErrorMessage);
    }

    [Fact]
    public async Task Allows_a_free_service()
    {
        var result = await Logic().CreateAsync("Brake Inspection", null, 45, 0m);

        Assert.True(result.Success, result.ErrorMessage);
    }

    [Fact]
    public async Task Updates_price_and_duration()
    {
        var service = Given("Oil Change", price: 49.99m, minutes: 30);

        var result = await Logic().UpdateAsync(service.ServiceId, "Oil Change", "Now with synthetic", 45, 54.99m);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(54.99m, service.Price);
        Assert.Equal(45, service.EstimatedDurationMinutes);
    }

    [Fact]
    public async Task Refuses_a_rename_that_collides()
    {
        Given("Oil Change");
        var other = Given("Tire Rotation");

        var result = await Logic().UpdateAsync(other.ServiceId, "Oil Change", null, 30, 29.99m);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Archiving_hides_it_from_the_active_list_but_not_the_catalog()
    {
        var service = Given("Old Service");
        Given("Still Offered");

        await Logic().SetActiveAsync(service.ServiceId, isActive: false);

        Assert.DoesNotContain(await Logic().GetActiveAsync(), s => s.Name == "Old Service");
        Assert.Contains(await Logic().GetAllAsync(), s => s.Name == "Old Service");
    }

    [Fact]
    public async Task Will_not_archive_the_last_bookable_service()
    {
        var only = Given("Oil Change");

        var result = await Logic().SetActiveAsync(only.ServiceId, isActive: false);

        Assert.False(result.Success);
        Assert.True(only.IsActive);
    }

    [Fact]
    public async Task An_archived_service_doesnt_count_as_the_spare()
    {
        var active = Given("Oil Change");
        Given("Retired", active: false);

        var result = await Logic().SetActiveAsync(active.ServiceId, isActive: false);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Archived_services_can_be_restored()
    {
        var service = Given("Seasonal Special", active: false);
        Given("Oil Change");

        var result = await Logic().SetActiveAsync(service.ServiceId, isActive: true);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Contains(await Logic().GetActiveAsync(), s => s.Name == "Seasonal Special");
    }
}

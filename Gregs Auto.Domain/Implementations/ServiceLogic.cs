using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.Implementations.Interfaces;
using Gregs_Auto.Domain.IRepositories;

namespace Gregs_Auto.Domain.Implementations;

public class ServiceLogic : IServiceLogic
{
    // A job has to take some time, and nothing in a shop takes a full day in one
    // unbroken block — a wrong figure here quietly breaks the overlap rules.
    private const int MinDurationMinutes = 5;
    private const int MaxDurationMinutes = 8 * 60;

    private readonly IServiceRepository _serviceRepository;

    public ServiceLogic(IServiceRepository serviceRepository)
    {
        _serviceRepository = serviceRepository;
    }

    public async Task<IList<Service>> GetActiveAsync()
    {
        var all = await _serviceRepository.GetAllAsync();
        return all.Where(s => s.IsActive).OrderBy(s => s.Name).ToList();
    }

    public async Task<IList<Service>> GetAllAsync()
    {
        var all = await _serviceRepository.GetAllAsync();
        return all.OrderByDescending(s => s.IsActive).ThenBy(s => s.Name).ToList();
    }

    public async Task<Service?> GetByIdAsync(int serviceId) =>
        await _serviceRepository.GetAsync(s => s.ServiceId == serviceId);

    public async Task<ServiceResult> CreateAsync(string name, string? description, int durationMinutes, decimal price)
    {
        var validation = Validate(name, durationMinutes, price);
        if (validation != null)
            return ServiceResult.Fail(validation);

        name = name.Trim();

        var clash = await _serviceRepository.GetAsync(s => s.Name == name);
        if (clash != null)
            return ServiceResult.Fail("There's already a service with that name.");

        var service = new Service
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            EstimatedDurationMinutes = durationMinutes,
            Price = price,
            IsActive = true
        };

        await _serviceRepository.AddAsync(service);
        await _serviceRepository.SaveChangesAsync();

        return ServiceResult.Ok(service.ServiceId);
    }

    public async Task<ServiceResult> UpdateAsync(int serviceId, string name, string? description, int durationMinutes, decimal price)
    {
        var validation = Validate(name, durationMinutes, price);
        if (validation != null)
            return ServiceResult.Fail(validation);

        var service = await _serviceRepository.GetAsync(s => s.ServiceId == serviceId);
        if (service == null)
            return ServiceResult.Fail("Service not found.");

        name = name.Trim();

        var clash = await _serviceRepository.GetAsync(s => s.Name == name && s.ServiceId != serviceId);
        if (clash != null)
            return ServiceResult.Fail("There's already a service with that name.");

        // Note: changing the price here changes what past appointments appear to
        // have cost, because Appointment doesn't carry its own price. Harmless
        // while this is only a schedule; it has to be fixed before invoicing.
        service.Name = name;
        service.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        service.EstimatedDurationMinutes = durationMinutes;
        service.Price = price;

        await _serviceRepository.SaveChangesAsync();
        return ServiceResult.Ok(service.ServiceId);
    }

    public async Task<ServiceResult> SetActiveAsync(int serviceId, bool isActive)
    {
        var service = await _serviceRepository.GetAsync(s => s.ServiceId == serviceId);
        if (service == null)
            return ServiceResult.Fail("Service not found.");

        // Archiving the last one would leave nothing bookable at all.
        if (!isActive)
        {
            var others = await _serviceRepository.FindByAsync(s => s.IsActive && s.ServiceId != serviceId);
            if (others.Count == 0)
                return ServiceResult.Fail("That's the only service left — add another before archiving this one.");
        }

        service.IsActive = isActive;
        await _serviceRepository.SaveChangesAsync();

        return ServiceResult.Ok(service.ServiceId);
    }

    private static string? Validate(string name, int durationMinutes, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Give the service a name.";

        if (durationMinutes < MinDurationMinutes || durationMinutes > MaxDurationMinutes)
            return $"How long it takes has to be between {MinDurationMinutes} minutes and {MaxDurationMinutes / 60} hours.";

        if (price < 0)
            return "Price can't be negative. Use 0 for something you don't charge for.";

        return null;
    }
}

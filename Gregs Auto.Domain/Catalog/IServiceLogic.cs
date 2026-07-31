using Gregs_Auto.Domain.EntityModels;

namespace Gregs_Auto.Domain.Catalog;

// The service catalog. Prices and durations change — an oil change goes up a
// dollar, a brake job turns out to take two hours — and none of that should
// require a developer.
public interface IServiceLogic
{
    // What the public sees and what can be booked.
    Task<IList<Service>> GetActiveAsync();

    // Everything, archived included, for the management screen.
    Task<IList<Service>> GetAllAsync();

    Task<Service?> GetByIdAsync(int serviceId);

    Task<ServiceResult> CreateAsync(string name, string? description, int durationMinutes, decimal price);

    Task<ServiceResult> UpdateAsync(int serviceId, string name, string? description, int durationMinutes, decimal price);

    // Archived services disappear from the booking form but stay attached to the
    // appointments already booked against them.
    Task<ServiceResult> SetActiveAsync(int serviceId, bool isActive);
}

public class ServiceResult
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int ServiceId { get; private set; }

    public static ServiceResult Ok(int serviceId) => new() { Success = true, ServiceId = serviceId };

    public static ServiceResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}

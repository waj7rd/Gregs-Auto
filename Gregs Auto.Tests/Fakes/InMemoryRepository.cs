using System.Linq.Expressions;
using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories;
using Gregs_Auto.Domain.IRepositories.IBase;

namespace Gregs_Auto.Tests.Fakes;

// In-memory stand-in for the generic repository. The Domain only ever sees the
// interfaces, so this is enough to exercise every business rule without a
// database — and without a mocking library, which keeps the tests readable.
public class InMemoryRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly List<T> Items = new();

    // Assigns the next identity value, mimicking what the database would do on
    // insert. Business logic reads the id back after saving.
    private readonly Action<T, int>? _assignId;
    private int _nextId = 1;

    public InMemoryRepository(Action<T, int>? assignId = null) => _assignId = assignId;

    public int SaveCount { get; private set; }

    // Seeds without touching identity — for rows that already "exist".
    public InMemoryRepository<T> Seed(params T[] items)
    {
        foreach (var item in items)
        {
            Items.Add(item);
            _nextId++;
        }

        return this;
    }

    public IQueryable<T> GetAll() => Items.AsQueryable();

    public IQueryable<T> FindBy(Expression<Func<T, bool>> predicate) =>
        Items.AsQueryable().Where(predicate);

    public Task<IList<T>> FindByAsync(Expression<Func<T, bool>> predicate) =>
        Task.FromResult<IList<T>>(Items.AsQueryable().Where(predicate).ToList());

    public Task<IList<T>> GetAllAsync() => Task.FromResult<IList<T>>(Items.ToList());

    public Task<T?> GetAsync(Expression<Func<T, bool>> predicate) =>
        Task.FromResult(Items.AsQueryable().FirstOrDefault(predicate));

    public Task AddAsync(T entity)
    {
        Add(entity);
        return Task.CompletedTask;
    }

    public void Add(T entity)
    {
        _assignId?.Invoke(entity, _nextId++);
        Items.Add(entity);
    }

    public void Delete(T entity) => Items.Remove(entity);

    public void Edit(T entity) { }

    public void Save() => SaveCount++;

    public Task SaveChangesAsync()
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

public class FakeAppointmentRepository : InMemoryRepository<Appointment>, IAppointmentRepository
{
    public FakeAppointmentRepository() : base((a, id) => a.AppointmentId = id) { }

    public Task<IList<Appointment>> GetAllWithDetailsAsync() =>
        Task.FromResult<IList<Appointment>>(Items.OrderBy(a => a.ScheduledAt).ToList());

    public Task<IList<Appointment>> GetActiveBetweenAsync(DateTime fromInclusive, DateTime toExclusive) =>
        Task.FromResult<IList<Appointment>>(Items
            .Where(a => a.ScheduledAt >= fromInclusive
                     && a.ScheduledAt < toExclusive
                     && a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.ScheduledAt)
            .ToList());
}

public class FakeServiceRepository : InMemoryRepository<Service>, IServiceRepository
{
    public FakeServiceRepository() : base((s, id) => s.ServiceId = id) { }
}

public class FakeVehicleRepository : InMemoryRepository<Vehicle>, IVehicleRepository
{
    public FakeVehicleRepository() : base((v, id) => v.VehicleId = id) { }

    public Task<IList<Vehicle>> GetAllWithCustomerAsync() =>
        Task.FromResult<IList<Vehicle>>(Items.ToList());
}

public class FakeCustomerRepository : InMemoryRepository<Customer>, ICustomerRepository
{
    public FakeCustomerRepository() : base((c, id) => c.CustomerId = id) { }

    public Task<IList<Customer>> GetAllWithVehiclesAsync() =>
        Task.FromResult<IList<Customer>>(Items.ToList());

    public Task<Customer?> GetByIdWithVehiclesAsync(int customerId) =>
        Task.FromResult(Items.FirstOrDefault(c => c.CustomerId == customerId));
}

public class FakeUserRepository : InMemoryRepository<User>, IUserRepository
{
    public FakeUserRepository() : base((u, id) => u.UserId = id) { }
}

public class FakeLoginAuditRepository : InMemoryRepository<LoginAudit>, ILoginAuditRepository
{
    public FakeLoginAuditRepository() : base((l, id) => l.LoginAuditId = id) { }

    public IReadOnlyList<LoginAudit> All => Items;

    public Task<IList<LoginAudit>> GetRecentAsync(int count) =>
        Task.FromResult<IList<LoginAudit>>(Items.OrderByDescending(l => l.OccurredAt).Take(count).ToList());
}

public class FakeBookingRequestRepository : InMemoryRepository<BookingRequest>, IBookingRequestRepository
{
    public FakeBookingRequestRepository() : base((r, id) => r.BookingRequestId = id) { }

    public IReadOnlyList<BookingRequest> All => Items;

    public Task<IList<BookingRequest>> GetPendingAsync() =>
        Task.FromResult<IList<BookingRequest>>(Items
            .Where(r => r.Status == BookingRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToList());

    public Task<IList<BookingRequest>> GetRecentAsync(int count) =>
        Task.FromResult<IList<BookingRequest>>(Items
            .Where(r => r.Status != BookingRequestStatus.Pending)
            .OrderByDescending(r => r.HandledAt)
            .Take(count)
            .ToList());

    public Task<BookingRequest?> GetWithDetailsAsync(int bookingRequestId) =>
        Task.FromResult(Items.FirstOrDefault(r => r.BookingRequestId == bookingRequestId));
}

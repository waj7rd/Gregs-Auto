using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories.IBase;

namespace Gregs_Auto.Domain.IRepositories;

public interface IBookingRequestRepository : IGenericRepository<BookingRequest>
{
    // Pending requests, oldest first — it's a queue, so the longest wait is at
    // the top.
    Task<IList<BookingRequest>> GetPendingAsync();

    // Everything, newest first, for the "already dealt with" view.
    Task<IList<BookingRequest>> GetRecentAsync(int count);

    Task<BookingRequest?> GetWithDetailsAsync(int bookingRequestId);
}

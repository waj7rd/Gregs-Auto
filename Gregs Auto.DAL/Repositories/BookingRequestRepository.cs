using Microsoft.EntityFrameworkCore;
using Gregs_Auto.DAL.Context;
using Gregs_Auto.DAL.Repositories.Base;
using Gregs_Auto.Domain.EntityModels;
using Gregs_Auto.Domain.IRepositories;

namespace Gregs_Auto.DAL.Repositories;

public class BookingRequestRepository : GenericRepository<GregsAutoContext, BookingRequest>, IBookingRequestRepository
{
    public async Task<IList<BookingRequest>> GetPendingAsync()
    {
        return await Context.BookingRequests
            .Include(r => r.Service)
            .Where(r => r.Status == BookingRequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IList<BookingRequest>> GetRecentAsync(int count)
    {
        return await Context.BookingRequests
            .Include(r => r.Service)
            .Include(r => r.HandledByUser)
            .Where(r => r.Status != BookingRequestStatus.Pending)
            .OrderByDescending(r => r.HandledAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<BookingRequest?> GetWithDetailsAsync(int bookingRequestId)
    {
        return await Context.BookingRequests
            .Include(r => r.Service)
            .Include(r => r.HandledByUser)
            .FirstOrDefaultAsync(r => r.BookingRequestId == bookingRequestId);
    }
}

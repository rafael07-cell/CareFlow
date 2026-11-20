using System.Data;
using CareFlow.Application.Interfaces;
using CareFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CareFlow.Infrastructure.Persistence;

public sealed class EfBookingUnitOfWorkFactory : IBookingUnitOfWorkFactory
{
    private readonly IDbContextFactory<CareFlowDbContext> _factory;

    public EfBookingUnitOfWorkFactory(IDbContextFactory<CareFlowDbContext> factory) => _factory = factory;

    public async Task<IBookingUnitOfWork> BeginAsync(CancellationToken ct)
    {
        var db = await _factory.CreateDbContextAsync(ct);
        try
        {
            // Serializable: two staff confirming at the same moment cannot both take the last slot.
            var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            return new EfBookingUnitOfWork(db, tx);
        }
        catch
        {
            await db.DisposeAsync();
            throw;
        }
    }
}

public sealed class EfBookingUnitOfWork : IBookingUnitOfWork
{
    private readonly CareFlowDbContext _db;
    private readonly IDbContextTransaction _tx;

    public EfBookingUnitOfWork(CareFlowDbContext db, IDbContextTransaction tx)
    {
        _db = db;
        _tx = tx;
    }

    public Task<bool> PatientExistsAsync(int patientId, CancellationToken ct)
        => _db.Patients.AnyAsync(p => p.Id == patientId, ct);

    public async Task<IReadOnlyList<DiagnosticService>> GetServicesAsync(
        IReadOnlyCollection<int> serviceIds, CancellationToken ct)
    {
        var idList = serviceIds.ToList();
        return await _db.DiagnosticServices.Where(s => idList.Contains(s.Id)).ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<int, int>> GetBookedCountsAsync(
        DateOnly date, IReadOnlyCollection<int> serviceIds, CancellationToken ct)
    {
        var idList = serviceIds.ToList();
        return await _db.BookingItems
            .Where(i => i.Booking.BookingDate == date && idList.Contains(i.ServiceId))
            .GroupBy(i => i.ServiceId)
            .Select(g => new { ServiceId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ServiceId, x => x.Count, ct);
    }

    public async Task<int> AddBookingAsync(Booking booking, CancellationToken ct)
    {
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(ct);   // writes inside the open transaction, not yet committed
        return booking.Id;
    }

    public Task CommitAsync(CancellationToken ct) => _tx.CommitAsync(ct);

    public async ValueTask DisposeAsync()
    {
        await _tx.DisposeAsync();   // rolls back if CommitAsync was never called
        await _db.DisposeAsync();
    }
}

using CareFlow.Application.Interfaces;
using CareFlow.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Infrastructure.Persistence;

public sealed class EfBookingQueries : IBookingQueries
{
    private readonly IDbContextFactory<CareFlowDbContext> _factory;

    public EfBookingQueries(IDbContextFactory<CareFlowDbContext> factory) => _factory = factory;

    public async Task<IReadOnlyList<PatientItem>> GetPatientsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Patients.AsNoTracking()
            .OrderBy(p => p.FullName)
            .Select(p => new PatientItem(p.Id, p.FullName))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ServiceAvailability>> GetAvailabilityAsync(DateOnly date, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var services = await db.DiagnosticServices.AsNoTracking()
            .OrderBy(s => s.Name).ToListAsync(ct);

        var counts = await db.BookingItems.AsNoTracking()
            .Where(i => i.Booking.BookingDate == date)
            .GroupBy(i => i.ServiceId)
            .Select(g => new { ServiceId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ServiceId, x => x.Count, ct);

        return services.Select(s =>
        {
            var booked = counts.GetValueOrDefault(s.Id);
            return new ServiceAvailability(s.Id, s.Name, s.DailyCapacity, booked,
                Math.Max(0, s.DailyCapacity - booked));
        }).ToList();
    }

    public async Task<IReadOnlyList<BookingSummary>> GetBookingsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Bookings.AsNoTracking()
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new BookingSummary(b.Id, b.Patient.FullName, b.BookingDate, b.Items.Count))
            .ToListAsync(ct);
    }

    public async Task<BookingDetail?> GetBookingDetailAsync(int bookingId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Bookings.AsNoTracking()
            .Where(b => b.Id == bookingId)
            .Select(b => new BookingDetail(
                b.Id,
                b.Patient.FullName,
                b.BookingDate,
                b.CreatedAtUtc,
                b.Items.Select(i => i.Service.Name).OrderBy(n => n).ToList()))
            .FirstOrDefaultAsync(ct);
    }
}

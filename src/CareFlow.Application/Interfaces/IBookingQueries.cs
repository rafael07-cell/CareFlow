using CareFlow.Application.Models;

namespace CareFlow.Application.Interfaces;

public interface IBookingQueries
{
    Task<IReadOnlyList<PatientItem>> GetPatientsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ServiceAvailability>> GetAvailabilityAsync(DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<BookingSummary>> GetBookingsAsync(CancellationToken ct = default);
    Task<BookingDetail?> GetBookingDetailAsync(int bookingId, CancellationToken ct = default);
}

using CareFlow.Domain;

namespace CareFlow.Application.Interfaces;

/// <summary>
/// One database transaction. Disposing it WITHOUT calling CommitAsync rolls everything back.
/// </summary>
public interface IBookingUnitOfWork : IAsyncDisposable
{
    Task<bool> PatientExistsAsync(int patientId, CancellationToken ct);
    Task<IReadOnlyList<DiagnosticService>> GetServicesAsync(IReadOnlyCollection<int> serviceIds, CancellationToken ct);
    Task<IReadOnlyDictionary<int, int>> GetBookedCountsAsync(DateOnly date, IReadOnlyCollection<int> serviceIds, CancellationToken ct);
    Task<int> AddBookingAsync(Booking booking, CancellationToken ct);
    Task CommitAsync(CancellationToken ct);
}

public interface IBookingUnitOfWorkFactory
{
    Task<IBookingUnitOfWork> BeginAsync(CancellationToken ct);
}

using CareFlow.Application.Interfaces;
using CareFlow.Application.Models;
using CareFlow.Domain;

namespace CareFlow.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingUnitOfWorkFactory _uowFactory;
    private readonly IBookingQueries _queries;
    private readonly TimeProvider _time;

    public BookingService(IBookingUnitOfWorkFactory uowFactory, IBookingQueries queries, TimeProvider time)
    {
        _uowFactory = uowFactory;
        _queries = queries;
        _time = time;
    }

    public Task<IReadOnlyList<PatientItem>> GetPatientsAsync(CancellationToken ct = default)
        => _queries.GetPatientsAsync(ct);

    public Task<IReadOnlyList<ServiceAvailability>> GetAvailabilityAsync(DateOnly date, CancellationToken ct = default)
        => _queries.GetAvailabilityAsync(date, ct);

    public Task<IReadOnlyList<BookingSummary>> GetBookingsAsync(CancellationToken ct = default)
        => _queries.GetBookingsAsync(ct);

    public Task<BookingDetail?> GetBookingDetailAsync(int bookingId, CancellationToken ct = default)
        => _queries.GetBookingDetailAsync(bookingId, ct);

    public async Task<BookingResult> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct = default)
    {
        // ---- 1. Rules that need no database ----
        var errors = new List<string>();
        var ids = request.ServiceIds ?? Array.Empty<int>();

        if (request.PatientId <= 0)
            errors.Add("Select a patient.");
        if (ids.Count < 1 || ids.Count > 3)                                   // BR-02
            errors.Add("Select between 1 and 3 services.");
        if (ids.Distinct().Count() != ids.Count)                              // BR-06
            errors.Add("The same service cannot be selected more than once.");
        var today = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
        if (request.Date < today)                                             // BR-03
            errors.Add("The booking date cannot be earlier than today.");

        if (errors.Count > 0)
            return BookingResult.Fail(errors);

        // ---- 2. One transaction for everything else (BR-07) ----
        try
        {
            // If we return early or throw, DisposeAsync runs WITHOUT a commit => full rollback.
            await using var uow = await _uowFactory.BeginAsync(ct);

            if (!await uow.PatientExistsAsync(request.PatientId, ct))         // BR-01
                return BookingResult.Fail("The selected patient does not exist.");

            var services = await uow.GetServicesAsync(ids, ct);
            if (services.Count != ids.Count)
                return BookingResult.Fail("One or more selected services do not exist.");

            // BR-08: recount capacity NOW, inside the transaction, never trust what the screen showed.
            var booked = await uow.GetBookedCountsAsync(request.Date, ids, ct);
            var full = services
                .Where(s => booked.GetValueOrDefault(s.Id) + 1 > s.DailyCapacity)   // BR-04 / BR-05
                .Select(s => s.Name)
                .ToList();

            if (full.Count > 0)
                return BookingResult.Fail(
                    $"Fully booked on {request.Date:MMM d, yyyy}: {string.Join(", ", full)}. " +
                    "No part of this booking was recorded.");

            var booking = new Booking
            {
                PatientId = request.PatientId,
                BookingDate = request.Date,
                CreatedAtUtc = _time.GetUtcNow().UtcDateTime
            };
            foreach (var id in ids)
                booking.Items.Add(new BookingItem { ServiceId = id });

            var bookingId = await uow.AddBookingAsync(booking, ct);
            await uow.CommitAsync(ct);                                         // all-or-nothing
            return BookingResult.Ok(bookingId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // BR-10: understandable message, and never reported as success.
            return BookingResult.Fail(
                "The booking could not be saved because of a database problem. " +
                "No changes were made. Please try again.");
        }
    }
}

namespace CareFlow.Application.Models;

public record PatientItem(int Id, string FullName);

public record ServiceAvailability(int ServiceId, string Name, int DailyCapacity, int Booked, int Remaining);

public record CreateBookingRequest(int PatientId, DateOnly Date, IReadOnlyList<int> ServiceIds);

public record BookingSummary(int Id, string PatientName, DateOnly BookingDate, int ServiceCount);

public record BookingDetail(int Id, string PatientName, DateOnly BookingDate,
                            DateTime CreatedAtUtc, IReadOnlyList<string> Services);

public record BookingResult(bool Success, int? BookingId, IReadOnlyList<string> Errors)
{
    public string Message => Success
        ? $"Booking #{BookingId} confirmed."
        : string.Join(" ", Errors);

    public static BookingResult Ok(int bookingId) => new(true, bookingId, Array.Empty<string>());
    public static BookingResult Fail(string error) => new(false, null, new[] { error });
    public static BookingResult Fail(IReadOnlyList<string> errors) => new(false, null, errors);
}

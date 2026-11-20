namespace CareFlow.Domain;

public class Booking
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public DateOnly BookingDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<BookingItem> Items { get; set; } = new();
}

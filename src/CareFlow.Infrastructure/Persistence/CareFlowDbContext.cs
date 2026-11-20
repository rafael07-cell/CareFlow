using CareFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Infrastructure.Persistence;

public class CareFlowDbContext : DbContext
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<DiagnosticService> DiagnosticServices => Set<DiagnosticService>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingItem> BookingItems => Set<BookingItem>();

    public CareFlowDbContext(DbContextOptions<CareFlowDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Patient>(e =>
        {
            e.ToTable("Patients");
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(150);
        });

        b.Entity<DiagnosticService>(e =>
        {
            e.ToTable("DiagnosticServices",
                t => t.HasCheckConstraint("CK_DiagnosticServices_DailyCapacity", "\"DailyCapacity\" > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<Booking>(e =>
        {
            e.ToTable("Bookings");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Patient).WithMany()
                .HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.BookingDate);
        });

        b.Entity<BookingItem>(e =>
        {
            e.ToTable("BookingItems");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Booking).WithMany(x => x.Items)
                .HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Service).WithMany()
                .HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
            // BR-06 enforced by the database too: same service twice in one booking is impossible
            e.HasIndex(x => new { x.BookingId, x.ServiceId }).IsUnique();
        });
    }
}

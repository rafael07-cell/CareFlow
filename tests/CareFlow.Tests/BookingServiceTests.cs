using CareFlow.Application.Interfaces;
using CareFlow.Application.Models;
using CareFlow.Application.Services;
using CareFlow.Domain;
using CareFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Tests;

[Collection("Database")]
public class BookingServiceTests : IAsyncLifetime
{
    private const string Cbc = "Complete Blood Count";
    private const string Ua = "Urinalysis";
    private const string Cxr = "Chest X-Ray";
    private const string Ecg = "ECG";

    private readonly DatabaseFixture _fx;
    public BookingServiceTests(DatabaseFixture fx) => _fx = fx;

    public Task InitializeAsync() => _fx.ResetBookingsAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    // ---------- helpers ----------
    private static DateOnly Future => DateOnly.FromDateTime(DateTime.Now).AddDays(7);

    private BookingService NewService(Func<IBookingUnitOfWorkFactory, IBookingUnitOfWorkFactory>? decorate = null)
    {
        IBookingUnitOfWorkFactory uow = new EfBookingUnitOfWorkFactory(_fx.Factory);
        if (decorate is not null) uow = decorate(uow);
        return new BookingService(uow, new EfBookingQueries(_fx.Factory), TimeProvider.System);
    }

    private async Task<int> PatientIdAsync()
    {
        await using var db = _fx.Factory.CreateDbContext();
        return await db.Patients.OrderBy(p => p.Id).Select(p => p.Id).FirstAsync();
    }

    private async Task<int> ServiceIdAsync(string name)
    {
        await using var db = _fx.Factory.CreateDbContext();
        return await db.DiagnosticServices.Where(s => s.Name == name).Select(s => s.Id).SingleAsync();
    }

    private async Task<(int Bookings, int Items)> CountsAsync()
    {
        await using var db = _fx.Factory.CreateDbContext();
        return (await db.Bookings.CountAsync(), await db.BookingItems.CountAsync());
    }

    private static async Task BookOkAsync(BookingService svc, int patient, DateOnly date, params int[] services)
    {
        var r = await svc.CreateBookingAsync(new CreateBookingRequest(patient, date, services));
        Assert.True(r.Success, r.Message);
    }

    private async Task AssertRejectedAsync(CreateBookingRequest request)
    {
        var r = await NewService().CreateBookingAsync(request);
        Assert.False(r.Success);
        Assert.NotEmpty(r.Message);
        Assert.Equal((0, 0), await CountsAsync());
    }

    // ---------- AC-01 ----------
    [Fact]
    public async Task AC01_Availability_IsCalculatedFromPersistedBookings()
    {
        var svc = NewService();
        var p = await PatientIdAsync();
        var cbc = await ServiceIdAsync(Cbc);
        var ua = await ServiceIdAsync(Ua);

        await BookOkAsync(svc, p, Future, cbc, ua);

        var day = await svc.GetAvailabilityAsync(Future);
        Assert.Equal(2, day.Single(a => a.Name == Cbc).Remaining);   // capacity 3, one booked
        Assert.Equal(2, day.Single(a => a.Name == Ua).Remaining);
        Assert.Equal(2, day.Single(a => a.Name == Cxr).Remaining);   // untouched
        Assert.Equal(2, day.Single(a => a.Name == Ecg).Remaining);

        var otherDay = await svc.GetAvailabilityAsync(Future.AddDays(1));
        Assert.All(otherDay, a => Assert.Equal(a.DailyCapacity, a.Remaining));
    }

    // ---------- AC-02 ----------
    [Fact]
    public async Task AC02_MultiServiceBooking_RecordsOneBookingWithEverySelectedService()
    {
        var svc = NewService();
        var p = await PatientIdAsync();
        var ids = new[] { await ServiceIdAsync(Cbc), await ServiceIdAsync(Ua), await ServiceIdAsync(Ecg) };

        var result = await svc.CreateBookingAsync(new CreateBookingRequest(p, Future, ids));

        Assert.True(result.Success, result.Message);
        Assert.Equal((1, 3), await CountsAsync());

        var detail = await svc.GetBookingDetailAsync(result.BookingId!.Value);
        Assert.NotNull(detail);
        Assert.Equal(new[] { Cbc, Ecg, Ua }, detail!.Services);
        Assert.Equal(Future, detail.BookingDate);
    }

    // ---------- AC-03 ----------
    [Fact]
    public async Task AC03_FullService_RejectsTheWholeRequest_NothingIsSaved()
    {
        var svc = NewService();
        var p = await PatientIdAsync();
        var cbc = await ServiceIdAsync(Cbc);
        var ecg = await ServiceIdAsync(Ecg);

        await BookOkAsync(svc, p, Future, ecg);
        await BookOkAsync(svc, p, Future, ecg);          // ECG capacity (2) is now full
        var before = await CountsAsync();

        var result = await svc.CreateBookingAsync(new CreateBookingRequest(p, Future, new[] { cbc, ecg }));

        Assert.False(result.Success);
        Assert.Contains("ECG", result.Message);
        Assert.Equal(before, await CountsAsync());       // CBC was NOT recorded either

        var day = await svc.GetAvailabilityAsync(Future);
        Assert.Equal(3, day.Single(a => a.Name == Cbc).Remaining);   // CBC capacity unchanged
    }

    [Fact]
    public async Task AC03_NoServicesSelected_IsRejected()
        => await AssertRejectedAsync(new CreateBookingRequest(await PatientIdAsync(), Future, Array.Empty<int>()));

    [Fact]
    public async Task AC03_MoreThanThreeServices_IsRejected()
    {
        var all = new[] { await ServiceIdAsync(Cbc), await ServiceIdAsync(Ua), await ServiceIdAsync(Cxr), await ServiceIdAsync(Ecg) };
        await AssertRejectedAsync(new CreateBookingRequest(await PatientIdAsync(), Future, all));
    }

    [Fact]
    public async Task AC03_DuplicateService_IsRejected()
    {
        var cbc = await ServiceIdAsync(Cbc);
        await AssertRejectedAsync(new CreateBookingRequest(await PatientIdAsync(), Future, new[] { cbc, cbc }));
    }

    [Fact]
    public async Task AC03_PastDate_IsRejected()
    {
        var past = DateOnly.FromDateTime(DateTime.Now).AddDays(-1);
        await AssertRejectedAsync(new CreateBookingRequest(await PatientIdAsync(), past, new[] { await ServiceIdAsync(Cbc) }));
    }

    [Fact]
    public async Task AC03_UnknownPatient_IsRejected()
        => await AssertRejectedAsync(new CreateBookingRequest(999999, Future, new[] { await ServiceIdAsync(Cbc) }));

    // ---------- AC-04 ----------
    [Fact]
    public async Task AC04_StaleAvailability_IsRevalidated_AndOverbookingIsBlocked()
    {
        var svc = NewService();
        var p = await PatientIdAsync();
        var ecg = await ServiceIdAsync(Ecg);

        // 1. Staff opens the screen: ECG shows 2 slots left.
        var staleView = await svc.GetAvailabilityAsync(Future);
        Assert.Equal(2, staleView.Single(a => a.ServiceId == ecg).Remaining);

        // 2. Meanwhile, other bookings take both ECG slots.
        await BookOkAsync(svc, p, Future, ecg);
        await BookOkAsync(svc, p, Future, ecg);

        // 3. Staff submits based on the old screen. The service must recheck and refuse.
        var result = await svc.CreateBookingAsync(new CreateBookingRequest(p, Future, new[] { ecg }));

        Assert.False(result.Success);
        Assert.Contains("ECG", result.Message);
        Assert.Equal((2, 2), await CountsAsync());       // still exactly the 2 earlier bookings
        var fresh = await svc.GetAvailabilityAsync(Future);
        Assert.Equal(0, fresh.Single(a => a.ServiceId == ecg).Remaining);
    }

    // ---------- AC-05 ----------
    [Fact]
    public async Task AC05_DatabaseFailureBeforeCommit_LeavesNoPartialBooking()
    {
        // The booking and its items ARE written inside the transaction, then the commit is forced to fail.
        var failing = NewService(inner => new FailOnCommitFactory(inner));
        var p = await PatientIdAsync();
        var ids = new[] { await ServiceIdAsync(Cbc), await ServiceIdAsync(Ua), await ServiceIdAsync(Ecg) };

        var result = await failing.CreateBookingAsync(new CreateBookingRequest(p, Future, ids));

        Assert.False(result.Success);                    // never presented as success
        Assert.Contains("No changes were made", result.Message);
        Assert.Equal((0, 0), await CountsAsync());       // rolled back: no booking, no items

        var day = await NewService().GetAvailabilityAsync(Future);
        Assert.All(day, a => Assert.Equal(0, a.Booked)); // capacity unchanged
    }

    // ---------- AC-06 (supporting check; the real app restart is verified manually) ----------
    [Fact]
    public async Task AC06_ConfirmedBookings_AreReadableFromBrandNewServiceInstances()
    {
        var p = await PatientIdAsync();
        var ids = new[] { await ServiceIdAsync(Cbc), await ServiceIdAsync(Ua) };
        var created = await NewService().CreateBookingAsync(new CreateBookingRequest(p, Future, ids));
        Assert.True(created.Success, created.Message);

        var fresh = NewService();                        // simulates a restart: nothing carried over in memory
        var list = await fresh.GetBookingsAsync();
        var summary = Assert.Single(list);
        Assert.Equal(2, summary.ServiceCount);

        var detail = await fresh.GetBookingDetailAsync(summary.Id);
        Assert.Equal(new[] { Cbc, Ua }, detail!.Services);
    }

    // ---------- controlled failure: writes inside the transaction, then fails at commit ----------
    private sealed class FailOnCommitFactory : IBookingUnitOfWorkFactory
    {
        private readonly IBookingUnitOfWorkFactory _inner;
        public FailOnCommitFactory(IBookingUnitOfWorkFactory inner) => _inner = inner;
        public async Task<IBookingUnitOfWork> BeginAsync(CancellationToken ct)
            => new FailOnCommitUow(await _inner.BeginAsync(ct));
    }

    private sealed class FailOnCommitUow : IBookingUnitOfWork
    {
        private readonly IBookingUnitOfWork _inner;
        public FailOnCommitUow(IBookingUnitOfWork inner) => _inner = inner;

        public Task<bool> PatientExistsAsync(int id, CancellationToken ct) => _inner.PatientExistsAsync(id, ct);
        public Task<IReadOnlyList<DiagnosticService>> GetServicesAsync(IReadOnlyCollection<int> ids, CancellationToken ct) => _inner.GetServicesAsync(ids, ct);
        public Task<IReadOnlyDictionary<int, int>> GetBookedCountsAsync(DateOnly d, IReadOnlyCollection<int> ids, CancellationToken ct) => _inner.GetBookedCountsAsync(d, ids, ct);
        public Task<int> AddBookingAsync(Booking b, CancellationToken ct) => _inner.AddBookingAsync(b, ct);

        public Task CommitAsync(CancellationToken ct)
            => throw new InvalidOperationException("Simulated database failure at commit.");

        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }
}

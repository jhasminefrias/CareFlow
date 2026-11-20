using CareFlow.Business;
using CareFlow.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace CareFlow.Tests;

// Seed: patients 1-5 | services 1=CBC(5) 2=Urinalysis(5) 3=Chest X-Ray(3) 4=ECG(2)
public class BookingTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 11, 17);
    private readonly TestDb _db = new();
    private BookingService Svc => new(_db, new FixedClock(Today));

    public void Dispose() => _db.Dispose();

    [Fact] // AC-01 + AC-02
    public async Task Multi_service_booking_is_saved_and_reduces_capacity()
    {
        var r = await Svc.CreateBookingAsync(1, Today, new[] { 1, 2, 4 });
        Assert.True(r.Success);

        var ecg = (await Svc.GetAvailabilityAsync(Today)).Single(a => a.ServiceId == 4);
        Assert.Equal(1, ecg.Remaining);
        Assert.Equal(3, (await Svc.GetBookingsAsync()).Single().Services.Count);
    }

    [Fact] // AC-03: one full service rejects the WHOLE request
    public async Task Full_service_rejects_entire_booking()
    {
        Assert.True((await Svc.CreateBookingAsync(1, Today, new[] { 4 })).Success);
        Assert.True((await Svc.CreateBookingAsync(2, Today, new[] { 4 })).Success);   // ECG now full

        var r = await Svc.CreateBookingAsync(3, Today, new[] { 1, 2, 4 });
        Assert.False(r.Success);

        await using var c = _db.CreateDbContext();
        Assert.Equal(2, await c.Bookings.CountAsync());
        Assert.Equal(0, await c.BookingItems.CountAsync(i => i.DiagnosticServiceId == 1));   // CBC not saved separately
    }

    [Theory] // AC-03: invalid input
    [InlineData(new int[0])]
    [InlineData(new[] { 1, 2, 3, 4 })]
    [InlineData(new[] { 1, 1 })]
    public async Task Invalid_service_selection_is_rejected(int[] ids)
    {
        Assert.False((await Svc.CreateBookingAsync(1, Today, ids)).Success);
        await using var c = _db.CreateDbContext();
        Assert.Equal(0, await c.Bookings.CountAsync());
    }

    [Fact]
    public async Task Past_date_is_rejected() =>
        Assert.False((await Svc.CreateBookingAsync(1, Today.AddDays(-1), new[] { 1 })).Success);

    [Fact]
    public async Task Unknown_patient_is_rejected() =>
        Assert.False((await Svc.CreateBookingAsync(999, Today, new[] { 1 })).Success);

    [Fact] // AC-04: stale availability
    public async Task Stale_availability_is_rechecked_on_confirm()
    {
        await Svc.CreateBookingAsync(1, Today, new[] { 4 });
        var shown = (await Svc.GetAvailabilityAsync(Today)).Single(a => a.ServiceId == 4);
        Assert.Equal(1, shown.Remaining);                                   // what the screen displayed

        await Svc.CreateBookingAsync(2, Today, new[] { 4 });                // someone else takes the last slot

        var r = await Svc.CreateBookingAsync(3, Today, new[] { 4 });        // user confirms using stale info
        Assert.False(r.Success);

        await using var c = _db.CreateDbContext();
        Assert.Equal(2, await c.BookingItems.CountAsync(i => i.DiagnosticServiceId == 4));   // never overbooked
    }

    [Fact] // AC-05: failure after the header insert, before commit
    public async Task Failure_mid_transaction_leaves_nothing_behind()
    {
        _db.Interceptor.Armed = true;                                       // 2nd SaveChanges will throw
        var r = await Svc.CreateBookingAsync(1, Today, new[] { 1, 2 });
        Assert.False(r.Success);

        await using var c = _db.CreateDbContext();
        Assert.Equal(0, await c.Bookings.CountAsync());
        Assert.Equal(0, await c.BookingItems.CountAsync());
    }
}

public sealed record FixedClock(DateOnly Today) : IClock;

public sealed class FailOnSecondSave : SaveChangesInterceptor
{
    private int _count;
    public bool Armed { get; set; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (Armed && ++_count == 2) throw new InvalidOperationException("Injected failure");
        return base.SavingChangesAsync(eventData, result, ct);
    }
}

public sealed class TestDb : IDbContextFactory<CareFlowDbContext>, IDisposable
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    public FailOnSecondSave Interceptor { get; } = new();

    public TestDb()
    {
        _conn.Open();
        using var c = Create();
        c.Database.EnsureCreated();   // schema + seed data
    }

    private CareFlowDbContext Create()
    {
        var o = new DbContextOptionsBuilder<CareFlowDbContext>().UseSqlite(_conn).AddInterceptors(Interceptor).Options;
        return new CareFlowDbContext(o);
    }

    public CareFlowDbContext CreateDbContext() => Create();
    public void Dispose() => _conn.Dispose();
}

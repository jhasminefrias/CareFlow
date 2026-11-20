using System.Diagnostics;
using CareFlow.Business;
using CareFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Data;

public sealed class BookingService : IBookingService
{
    private readonly IDbContextFactory<CareFlowDbContext> _factory;
    private readonly IClock _clock;

    public BookingService(IDbContextFactory<CareFlowDbContext> factory, IClock clock)
    {
        _factory = factory;
        _clock = clock;
    }

    public async Task<IReadOnlyList<PatientItem>> GetPatientsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Patients.AsNoTracking().OrderBy(p => p.FullName)
            .Select(p => new PatientItem(p.Id, p.FullName)).ToListAsync(ct);
    }

    // AC-01: remaining capacity is computed from persisted bookings only.
    public async Task<IReadOnlyList<ServiceAvailability>> GetAvailabilityAsync(DateOnly date, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var booked = await CountBookedAsync(db, date, null, ct);
        var services = await db.DiagnosticServices.AsNoTracking().OrderBy(s => s.Id).ToListAsync(ct);
        return services.Select(s => new ServiceAvailability(s.Id, s.Name, s.DailyCapacity, booked.GetValueOrDefault(s.Id))).ToList();
    }

    public async Task<BookingResult> CreateBookingAsync(int patientId, DateOnly date, IReadOnlyCollection<int> serviceIds, CancellationToken ct = default)
    {
        // 1) Rules that need no database (BR-02, BR-03, BR-06)
        var errors = BookingRules.ValidateRequest(date, _clock.Today, serviceIds);
        if (errors.Count > 0) return BookingResult.Fail(errors);

        try
        {
            await using var db = await _factory.CreateDbContextAsync(ct);

            // 2) ONE transaction for the whole booking (BR-07). Leaving scope without Commit rolls everything back.
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            if (!await db.Patients.AnyAsync(p => p.Id == patientId, ct))                       // BR-01
                return BookingResult.Fail("The selected patient does not exist.");

            var services = await db.DiagnosticServices.Where(s => serviceIds.Contains(s.Id)).ToListAsync(ct);
            if (services.Count != serviceIds.Count)
                return BookingResult.Fail("One or more selected services do not exist.");

            // 3) Re-check capacity at confirmation time, inside the transaction (BR-04, BR-05, BR-08)
            var booked = await CountBookedAsync(db, date, serviceIds, ct);
            var full = services.Where(s => booked.GetValueOrDefault(s.Id) >= s.DailyCapacity).Select(s => s.Name).ToList();
            if (full.Count > 0)
                return BookingResult.Fail($"Fully booked on {date:MMM dd, yyyy}: {string.Join(", ", full)}. No booking was created.");

            // 4) Write header + items, then commit together
            var booking = new Booking { PatientId = patientId, BookingDate = date, CreatedAtUtc = DateTime.UtcNow };
            db.Bookings.Add(booking);
            await db.SaveChangesAsync(ct);

            db.BookingItems.AddRange(serviceIds.Select(id => new BookingItem { BookingId = booking.Id, DiagnosticServiceId = id }));
            await db.SaveChangesAsync(ct);

            await tx.CommitAsync(ct);
            return BookingResult.Ok(booking.Id);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return BookingResult.Fail("The booking could not be saved, so nothing was recorded. Please try again.");  // BR-10
        }
    }

    public async Task<IReadOnlyList<BookingSummary>> GetBookingsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var rows = await db.Bookings.AsNoTracking()
            .Include(b => b.Patient).Include(b => b.Items).ThenInclude(i => i.DiagnosticService)
            .OrderByDescending(b => b.Id).ToListAsync(ct);
        return rows.Select(b => new BookingSummary(
            b.Id, b.Patient!.FullName, b.BookingDate,
            b.Items.Select(i => i.DiagnosticService!.Name).OrderBy(n => n).ToList(), b.CreatedAtUtc)).ToList();
    }

    private static Task<Dictionary<int, int>> CountBookedAsync(CareFlowDbContext db, DateOnly date, IReadOnlyCollection<int>? ids, CancellationToken ct)
    {
        var q = db.BookingItems.Where(i => i.Booking!.BookingDate == date);
        if (ids is not null) q = q.Where(i => ids.Contains(i.DiagnosticServiceId));
        return q.GroupBy(i => i.DiagnosticServiceId)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count, ct);
    }
}

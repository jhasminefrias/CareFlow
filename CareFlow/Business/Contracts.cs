namespace CareFlow.Business;

public interface IClock { DateOnly Today { get; } }
public sealed class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now); // current LOCAL date (BR-03)
}

public interface IDatabaseInitializer { Task InitializeAsync(); }

public record PatientItem(int Id, string FullName);

public record ServiceAvailability(int ServiceId, string Name, int DailyCapacity, int Booked)
{
    public int Remaining => Math.Max(0, DailyCapacity - Booked);
}

public record BookingSummary(int Id, string PatientName, DateOnly BookingDate, IReadOnlyList<string> Services, DateTime CreatedAtUtc)
{
    public string Title => $"#{Id}  |  {PatientName}  |  {BookingDate:yyyy-MM-dd}";
    public string ServicesText => string.Join(", ", Services);
}

public record BookingResult(bool Success, int? BookingId, IReadOnlyList<string> Errors)
{
    public static BookingResult Ok(int id) => new(true, id, Array.Empty<string>());
    public static BookingResult Fail(params string[] errors) => new(false, null, errors);
    public static BookingResult Fail(IEnumerable<string> errors) => new(false, null, errors.ToList());
}

public interface IBookingService
{
    Task<IReadOnlyList<PatientItem>> GetPatientsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ServiceAvailability>> GetAvailabilityAsync(DateOnly date, CancellationToken ct = default);
    Task<BookingResult> CreateBookingAsync(int patientId, DateOnly date, IReadOnlyCollection<int> serviceIds, CancellationToken ct = default);
    Task<IReadOnlyList<BookingSummary>> GetBookingsAsync(CancellationToken ct = default);
}

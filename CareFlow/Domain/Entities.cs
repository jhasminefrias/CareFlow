namespace CareFlow.Domain;

public class Patient
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
}

public class DiagnosticService
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int DailyCapacity { get; set; }
}

public class Booking
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }
    public DateOnly BookingDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<BookingItem> Items { get; set; } = new();
}

// One row per service in a booking. Composite PK (BookingId, DiagnosticServiceId) also enforces BR-06 in the database.
public class BookingItem
{
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }
    public int DiagnosticServiceId { get; set; }
    public DiagnosticService? DiagnosticService { get; set; }
}

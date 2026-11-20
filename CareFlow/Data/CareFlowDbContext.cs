using CareFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CareFlow.Data;

public class CareFlowDbContext : DbContext
{
    public CareFlowDbContext(DbContextOptions<CareFlowDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<DiagnosticService> DiagnosticServices => Set<DiagnosticService>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingItem> BookingItems => Set<BookingItem>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Patient>(e =>
        {
            e.Property(p => p.FullName).IsRequired().HasMaxLength(100);
            e.HasData(
                new Patient { Id = 1, FullName = "Maria Santos",    DateOfBirth = new DateOnly(1990, 3, 14) },
                new Patient { Id = 2, FullName = "Juan Dela Cruz",  DateOfBirth = new DateOnly(1985, 7, 2) },
                new Patient { Id = 3, FullName = "Ana Reyes",       DateOfBirth = new DateOnly(2001, 11, 23) },
                new Patient { Id = 4, FullName = "Jose Garcia",     DateOfBirth = new DateOnly(1975, 1, 9) },
                new Patient { Id = 5, FullName = "Liza Mendoza",    DateOfBirth = new DateOnly(1998, 5, 30) });
        });

        b.Entity<DiagnosticService>(e =>
        {
            e.Property(s => s.Name).IsRequired().HasMaxLength(80);
            e.HasIndex(s => s.Name).IsUnique();
            e.ToTable(t => t.HasCheckConstraint("CK_DiagnosticService_DailyCapacity", "DailyCapacity > 0"));
            e.HasData(   // small capacities so the "fully booked" case is easy to demo
                new DiagnosticService { Id = 1, Name = "Complete Blood Count", DailyCapacity = 5 },
                new DiagnosticService { Id = 2, Name = "Urinalysis",           DailyCapacity = 5 },
                new DiagnosticService { Id = 3, Name = "Chest X-Ray",          DailyCapacity = 3 },
                new DiagnosticService { Id = 4, Name = "ECG",                  DailyCapacity = 2 });
        });

        b.Entity<Booking>(e =>
        {
            e.HasIndex(x => x.BookingDate);
            e.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Items).WithOne(i => i.Booking).HasForeignKey(i => i.BookingId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<BookingItem>(e =>
        {
            e.HasKey(i => new { i.BookingId, i.DiagnosticServiceId });
            e.HasOne(i => i.DiagnosticService).WithMany().HasForeignKey(i => i.DiagnosticServiceId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

// Lets `dotnet ef migrations add` work without launching the UI.
public class CareFlowDesignTimeFactory : IDesignTimeDbContextFactory<CareFlowDbContext>
{
    public CareFlowDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CareFlowDbContext>().UseSqlite(DatabasePaths.ConnectionString).Options;
        return new CareFlowDbContext(options);
    }
}

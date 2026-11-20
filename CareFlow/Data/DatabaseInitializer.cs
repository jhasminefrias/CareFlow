using CareFlow.Business;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Data;

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<CareFlowDbContext> _factory;
    public DatabaseInitializer(IDbContextFactory<CareFlowDbContext> factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        await db.Database.MigrateAsync();   // applies EF Core migrations + seed data
    }
}

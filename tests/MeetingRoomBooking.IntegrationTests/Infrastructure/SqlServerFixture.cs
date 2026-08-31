using MeetingRoomBooking.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace MeetingRoomBooking.IntegrationTests.Infrastructure;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer container =
        new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
            .Build();

    public string ConnectionString { get; private set; }
        = string.Empty;

    public async Task InitializeAsync()
    {
        try
        {
            await container.StartAsync();

            var connectionStringBuilder =
                new SqlConnectionStringBuilder(
                    container.GetConnectionString())
                {
                    InitialCatalog = "MeetingRoomBookingTests"
                };

            ConnectionString =
                connectionStringBuilder.ConnectionString;

            await using var dbContext = CreateDbContext();

            await dbContext.Database.MigrateAsync();
        }
        catch
        {
            await container.DisposeAsync();
            throw;
        }
    }

    public ApplicationDbContext CreateDbContext()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException(
                "The test database has not been initialized.");
        }

        var options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

        return new ApplicationDbContext(options);
    }

    public async Task DisposeAsync()
    {
        await container.DisposeAsync();
    }
}
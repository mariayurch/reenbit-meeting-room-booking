using MeetingRoomBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MeetingRoomBooking.IntegrationTests.Infrastructure;

public sealed class BookingWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly string connectionString;

    public BookingWebApplicationFactory(
        SqlServerFixture database)
    {
        if (string.IsNullOrWhiteSpace(database.ConnectionString))
        {
            throw new InvalidOperationException(
                "Initialize the test database before starting the application.");
        }

        connectionString = database.ConnectionString;
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            connectionString);

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        connectionString,

                    ["SeedAdmin:Email"] = string.Empty,
                    ["SeedAdmin:Password"] = string.Empty,
                    ["SeedAdmin:DisplayName"] = string.Empty
                });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();

            services.RemoveAll<
                DbContextOptions<ApplicationDbContext>>();

            services.RemoveAll<
                IDbContextOptionsConfiguration<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
        });
    }
}
using System.Globalization;
using System.Net;
using MeetingRoomBooking.Domain.Bookings;
using MeetingRoomBooking.Domain.MeetingRooms;
using MeetingRoomBooking.Domain.TimeSlots;
using MeetingRoomBooking.Infrastructure.Identity;
using MeetingRoomBooking.Infrastructure.Persistence;
using MeetingRoomBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MeetingRoomBooking.IntegrationTests;

public sealed class BookingFlowTests(SqlServerFixture database)
    : IClassFixture<SqlServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Free_slot_can_be_booked_but_cannot_be_booked_again(
        bool repeatBooking)
    {
        using var timeout =
            new CancellationTokenSource(TimeSpan.FromMinutes(2));

        var cancellationToken = timeout.Token;

        using var factory =
            new BookingWebApplicationFactory(database);

        // Disposable account in the isolated test database.
        const string password = "Booking-Test-123!";

        var email = $"booking-{Guid.NewGuid():N}@example.com";

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Booking test user"
        };

        var room = new MeetingRoom(
            $"Booking-{Guid.NewGuid():N}",
            6);

        var startUtc = new DateTimeOffset(
            DateTime.UtcNow.Date.AddDays(2).AddHours(9),
            TimeSpan.Zero);

        var slot = new TimeSlot(
            room.Id,
            startUtc,
            startUtc.AddMinutes(30));

        await using (var scope =
            factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var creation = await userManager.CreateAsync(
                user,
                password);

            Assert.True(
                creation.Succeeded,
                string.Join(
                    "; ",
                    creation.Errors.Select(error => error.Description)));

            var assignment = await userManager.AddToRoleAsync(
                user,
                "User");

            Assert.True(
                assignment.Succeeded,
                string.Join(
                    "; ",
                    assignment.Errors.Select(error => error.Description)));

            var dbContext = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            dbContext.MeetingRooms.Add(room);
            dbContext.TimeSlots.Add(slot);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true,
                BaseAddress = new Uri("https://localhost")
            });

        using var loginPage = await client.GetAsync(
            "/Identity/Account/Login",
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, loginPage.StatusCode);

        var loginHtml = await loginPage.Content
            .ReadAsStringAsync(cancellationToken);

        using var loginForm = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Input.Email"] = email,
                ["Input.Password"] = password,
                ["__RequestVerificationToken"] =
                    TestFormHelper.ExtractToken(loginHtml, "Input.Email")
            });

        using var loginResponse = await client.PostAsync(
            "/Identity/Account/Login",
            loginForm,
            cancellationToken);

        Assert.Equal(
            HttpStatusCode.Redirect,
            loginResponse.StatusCode);

        var date = startUtc.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        var scheduleUrl = $"/meeting-rooms/{room.Id}?date={date}";
        var bookingUrl = $"/meeting-rooms/{room.Id}/bookings";

        using var schedule = await client.GetAsync(
            scheduleUrl,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, schedule.StatusCode);

        var scheduleHtml = await schedule.Content
            .ReadAsStringAsync(cancellationToken);

        var bookingToken = TestFormHelper.ExtractToken(
            scheduleHtml,
            "TimeSlotIds");

        var attemptCount = repeatBooking ? 2 : 1;

        for (var attempt = 0; attempt < attemptCount; attempt++)
        {
            using var form = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["TimeSlotIds"] = slot.Id.ToString(),
                    ["date"] = date,
                    ["__RequestVerificationToken"] = bookingToken
                });

            using var response = await client.PostAsync(
                bookingUrl,
                form,
                cancellationToken);

            var expectedStatus = attempt == 0
                ? HttpStatusCode.Redirect
                : HttpStatusCode.Conflict;

            Assert.Equal(expectedStatus, response.StatusCode);

            if (attempt == 0)
            {
                var location = response.Headers.Location;

                Assert.NotNull(location);

                var redirectUrl = new Uri(
                    client.BaseAddress!,
                    location!);

                Assert.Equal(
                    $"/meeting-rooms/{room.Id}",
                    redirectUrl.AbsolutePath);
            }

            // Check persistence after every attempt, including rejection.
            await using var verificationDb =
                database.CreateDbContext();

            var bookings = await verificationDb.Bookings
                .AsNoTracking()
                .Include(booking => booking.TimeSlots)
                .Where(booking => booking.MeetingRoomId == room.Id)
                .ToListAsync(cancellationToken);

            var savedBooking = Assert.Single(bookings);

            Assert.Equal(user.Id, savedBooking.UserId);
            Assert.Equal(BookingStatus.Confirmed, savedBooking.Status);
            Assert.Equal(slot.StartUtc, savedBooking.StartUtc);
            Assert.Equal(slot.EndUtc, savedBooking.EndUtc);

            var savedSlot = Assert.Single(savedBooking.TimeSlots);

            Assert.Equal(slot.Id, savedSlot.Id);
        }
    }
}
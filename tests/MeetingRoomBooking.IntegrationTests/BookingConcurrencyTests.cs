using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
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

public sealed class BookingConcurrencyTests(
    SqlServerFixture database)
    : IClassFixture<SqlServerFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Concurrent_requests_for_overlapping_slots_create_one_booking(
        bool useMultipleSlots)
    {
        var requestCount = useMultipleSlots ? 2 : 5;

        // Disposable test accounts in the isolated container only.
        const string password = "Concurrency-Test-123!";

        using var timeout =
            new CancellationTokenSource(TimeSpan.FromMinutes(2));

        var cancellationToken = timeout.Token;

        using var factory =
            new BookingWebApplicationFactory(database);

        var room = new MeetingRoom(
            $"Concurrency-{Guid.NewGuid():N}",
            6);

        var startUtc = new DateTimeOffset(
            DateTime.UtcNow.Date.AddDays(2).AddHours(16).AddMinutes(30),
            TimeSpan.Zero);

        var slots = Enumerable.Range(0, useMultipleSlots ? 3 : 1)
            .Select(index => new TimeSlot(
                room.Id,
                startUtc.AddMinutes(index * 30),
                startUtc.AddMinutes((index + 1) * 30)))
            .ToArray();

        var requestedSlots = Enumerable.Range(0, requestCount)
            .Select(index => useMultipleSlots
                ? slots.Skip(index).Take(2).ToArray()
                : new[] { slots[0] })
            .ToArray();

        var users = new List<ApplicationUser>();

        await using (var scope =
            factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            for (var index = 0; index < requestCount; index++)
            {
                var email = $"test-{Guid.NewGuid():N}@example.com";

                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DisplayName = $"Concurrency user {index + 1}"
                };

                var creation = await userManager.CreateAsync(
                    user,
                    password);

                Assert.True(
                    creation.Succeeded,
                    string.Join(
                        "; ",
                        creation.Errors.Select(error =>
                            error.Description)));

                var roleAssignment =
                    await userManager.AddToRoleAsync(user, "User");

                Assert.True(
                    roleAssignment.Succeeded,
                    string.Join(
                        "; ",
                        roleAssignment.Errors.Select(error =>
                            error.Description)));

                users.Add(user);
            }

            dbContext.MeetingRooms.Add(room);
            dbContext.TimeSlots.AddRange(slots);

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var date = startUtc.ToString(
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture);

        var scheduleUrl =
            $"/meeting-rooms/{room.Id}?date={date}";

        var bookingUrl =
            $"/meeting-rooms/{room.Id}/bookings";

        var clients = new List<HttpClient>();
        var bookingTokens = new List<string>();

        try
        {
            foreach (var user in users)
            {
                var client = factory.CreateClient(
                    new WebApplicationFactoryClientOptions
                    {
                        AllowAutoRedirect = false,
                        HandleCookies = true,
                        BaseAddress = new Uri("https://localhost")
                    });

                clients.Add(client);

                using var loginPage = await client.GetAsync(
                    "/Identity/Account/Login",
                    cancellationToken);

                Assert.Equal(HttpStatusCode.OK, loginPage.StatusCode);

                var loginHtml = await loginPage.Content
                    .ReadAsStringAsync(cancellationToken);

                using var loginForm = new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["Input.Email"] = user.Email!,
                        ["Input.Password"] = password,
                        ["__RequestVerificationToken"] = ExtractToken(loginHtml, "Input.Email")
                    });

                using var loginResponse = await client.PostAsync(
                    "/Identity/Account/Login",
                    loginForm,
                    cancellationToken);

                Assert.Equal(
                    HttpStatusCode.Redirect,
                    loginResponse.StatusCode);

                using var schedule = await client.GetAsync(
                    scheduleUrl,
                    cancellationToken);

                Assert.Equal(HttpStatusCode.OK, schedule.StatusCode);

                var scheduleHtml = await schedule.Content
                    .ReadAsStringAsync(cancellationToken);

                bookingTokens.Add(ExtractToken(scheduleHtml, "TimeSlotIds"));
            }

            var startGate = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var requests = clients.Select(
                async (client, index) =>
                {
                    await startGate.Task.WaitAsync(cancellationToken);

                    var fields = new List<KeyValuePair<string, string>>
                    {
                        new("date", date),
                        new("__RequestVerificationToken", bookingTokens[index])
                    };

                    fields.AddRange(
                        requestedSlots[index].Select(selectedSlot =>
                            new KeyValuePair<string, string>(
                                "TimeSlotIds",
                                selectedSlot.Id.ToString())));

                    using var form = new FormUrlEncodedContent(fields);

                    using var response = await client.PostAsync(
                        bookingUrl,
                        form,
                        cancellationToken);

                    return response.StatusCode;
                }).ToArray();

            startGate.SetResult(true);

            var statuses = await Task.WhenAll(requests);

            Assert.Equal(
                1,
                statuses.Count(status =>
                    status == HttpStatusCode.Redirect));

            Assert.Equal(
                requestCount - 1,
                statuses.Count(status =>
                    status == HttpStatusCode.Conflict));

            var winnerIndex = Array.FindIndex(
                statuses,
                status => status == HttpStatusCode.Redirect);

            await using var verificationDb =
                database.CreateDbContext();

            var bookings = await verificationDb.Bookings
                .AsNoTracking()
                .Include(booking => booking.TimeSlots)
                .Where(booking => booking.MeetingRoomId == room.Id)
                .ToListAsync(cancellationToken);

            var savedBooking = Assert.Single(bookings);

            Assert.Equal(
                BookingStatus.Confirmed,
                savedBooking.Status);

            Assert.Equal(
                users[winnerIndex].Id,
                savedBooking.UserId);

            var winnerSlots = requestedSlots[winnerIndex];

            Assert.Equal(
                winnerSlots[0].StartUtc,
                savedBooking.StartUtc);

            Assert.Equal(
                winnerSlots[^1].EndUtc,
                savedBooking.EndUtc);

            var expectedSlotIds = winnerSlots
                .Select(selectedSlot => selectedSlot.Id)
                .OrderBy(id => id)
                .ToArray();

            var actualSlotIds = savedBooking.TimeSlots
                .Select(selectedSlot => selectedSlot.Id)
                .OrderBy(id => id)
                .ToArray();

            Assert.Equal(expectedSlotIds, actualSlotIds);

            // Only the winner's slot links may remain in the database.
            var allSlotIds = slots
                .Select(selectedSlot => selectedSlot.Id)
                .ToArray();

            var savedLinksCount = await verificationDb
                .Set<Dictionary<string, object>>("BookingTimeSlots")
                .CountAsync(
                    link => allSlotIds.Contains(
                        EF.Property<Guid>(link, "TimeSlotId")),
                    cancellationToken);

            Assert.Equal(winnerSlots.Length, savedLinksCount);
        }
        finally
        {
            foreach (var client in clients)
            {
                client.Dispose();
            }
        }
    }

    private static string ExtractToken(
    string html,
    string uniqueFieldName)
    {
        var regexTimeout = TimeSpan.FromSeconds(1);

        var forms = Regex.Matches(
            html,
            @"<form\b[^>]*>(?<content>.*?)</form\s*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            regexTimeout);

        var targetForm = forms
            .Cast<Match>()
            .Single(form => Regex.IsMatch(
                form.Groups["content"].Value,
                $@"\bname=""{Regex.Escape(uniqueFieldName)}""",
                RegexOptions.IgnoreCase,
                regexTimeout));

        var tokenInput = Regex.Matches(
                targetForm.Groups["content"].Value,
                @"<input\b[^>]*>",
                RegexOptions.IgnoreCase,
                regexTimeout)
            .Cast<Match>()
            .Single(input => Regex.IsMatch(
                input.Value,
                @"\bname=""__RequestVerificationToken""",
                RegexOptions.IgnoreCase,
                regexTimeout));

        var value = Regex.Match(
            tokenInput.Value,
            @"\bvalue=""([^""]*)""",
            RegexOptions.IgnoreCase,
            regexTimeout);

        Assert.True(
            value.Success,
            "Antiforgery token value was not found.");

        return WebUtility.HtmlDecode(value.Groups[1].Value);
    }
}
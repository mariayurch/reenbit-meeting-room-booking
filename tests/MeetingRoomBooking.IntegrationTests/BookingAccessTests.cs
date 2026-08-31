using System.Globalization;
using System.Net;
using MeetingRoomBooking.Domain.MeetingRooms;
using MeetingRoomBooking.Domain.TimeSlots;
using MeetingRoomBooking.Infrastructure.Persistence;
using MeetingRoomBooking.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MeetingRoomBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace MeetingRoomBooking.IntegrationTests;

public sealed class BookingAccessTests(SqlServerFixture database)
    : IClassFixture<SqlServerFixture>
{
    [Fact]
    public async Task Anonymous_user_cannot_create_booking()
    {
        using var timeout =
            new CancellationTokenSource(TimeSpan.FromMinutes(2));

        var cancellationToken = timeout.Token;

        using var factory =
            new BookingWebApplicationFactory(database);

        var room = new MeetingRoom(
            $"Access-{Guid.NewGuid():N}",
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

        using var form = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["TimeSlotIds"] = slot.Id.ToString(),
                ["date"] = startUtc.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture)
            });

        using var response = await client.PostAsync(
            $"/meeting-rooms/{room.Id}/bookings",
            form,
            cancellationToken);

        Assert.Equal(
            HttpStatusCode.Redirect,
            response.StatusCode);

        var location = response.Headers.Location;

        Assert.NotNull(location);

        var redirectUrl = new Uri(
            client.BaseAddress!,
            location!);

        Assert.Equal(
            "/Identity/Account/Login",
            redirectUrl.AbsolutePath);

        await using var verificationDb =
            database.CreateDbContext();

        var hasBooking = await verificationDb.Bookings
            .AnyAsync(
                booking => booking.MeetingRoomId == room.Id,
                cancellationToken);

        Assert.False(hasBooking);
    }

    [Theory]
    [InlineData("Admin", "/admin/meeting-rooms")]
    [InlineData("Admin", "/admin/meeting-rooms/create")]
    [InlineData("User", "/admin/meeting-rooms")]
    [InlineData("User", "/admin/meeting-rooms/create")]
    public async Task Admin_room_management_requires_admin_role(
        string role,
        string url)
    {
        using var timeout =
            new CancellationTokenSource(TimeSpan.FromMinutes(2));

        var cancellationToken = timeout.Token;

        using var factory =
            new BookingWebApplicationFactory(database);

        // Disposable account in the isolated test database.
        const string password = "Access-Test-123!";

        var email = $"access-{Guid.NewGuid():N}@example.com";

        await using (var scope =
            factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = $"Test {role}"
            };

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
                role);

            Assert.True(
                assignment.Succeeded,
                string.Join(
                    "; ",
                    assignment.Errors.Select(error => error.Description)));
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

        using var response = await client.GetAsync(
            url,
            cancellationToken);

        if (role == "Admin")
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        else
        {
            Assert.Equal(
                HttpStatusCode.Redirect,
                response.StatusCode);

            var location = response.Headers.Location;

            Assert.NotNull(location);

            var redirectUrl = new Uri(
                client.BaseAddress!,
                location!);

            Assert.Equal(
                "/Identity/Account/AccessDenied",
                redirectUrl.AbsolutePath);
        }

        // Check room creation only in the create-page test cases.
        if (url == "/admin/meeting-rooms/create")
        {
            // Get a token for the currently authenticated user.
            // The home page contains the Logout form with an antiforgery token.
            using var homePage = await client.GetAsync(
                "/",
                cancellationToken);

            Assert.Equal(HttpStatusCode.OK, homePage.StatusCode);

            var homeHtml = await homePage.Content
                .ReadAsStringAsync(cancellationToken);

            var token = TestFormHelper.ExtractToken(
                homeHtml,
                "__RequestVerificationToken");

            var roomName = $"Role-check-{Guid.NewGuid():N}";

            using var createForm = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Name"] = roomName,
                    ["Capacity"] = "6",
                    ["Description"] = "Created by an authorization test.",
                    ["__RequestVerificationToken"] = token
                });

            using var createResponse = await client.PostAsync(
                "/admin/meeting-rooms/create",
                createForm,
                cancellationToken);

            Assert.Equal(
                HttpStatusCode.Redirect,
                createResponse.StatusCode);

            var createLocation = createResponse.Headers.Location;

            Assert.NotNull(createLocation);

            var createRedirectUrl = new Uri(
                client.BaseAddress!,
                createLocation!);

            var expectedPath = role == "Admin"
                ? "/admin/meeting-rooms"
                : "/Identity/Account/AccessDenied";

            Assert.Equal(
                expectedPath,
                createRedirectUrl.AbsolutePath);

            await using var verificationDb =
                database.CreateDbContext();

            var savedRooms = await verificationDb.MeetingRooms
                .AsNoTracking()
                .Where(room => room.Name == roomName)
                .ToListAsync(cancellationToken);

            if (role == "Admin")
            {
                var savedRoom = Assert.Single(savedRooms);

                Assert.Equal(6, savedRoom.Capacity);
                Assert.Equal(
                    "Created by an authorization test.",
                    savedRoom.Description);
                Assert.True(savedRoom.IsActive);
            }
            else
            {
                Assert.Empty(savedRooms);
            }
        }
    }
}
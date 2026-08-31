using System.Globalization;
using System.Security.Claims;
using MeetingRoomBooking.Application.Bookings;
using MeetingRoomBooking.Application.Bookings.Models;
using MeetingRoomBooking.Application.MeetingRooms;
using MeetingRoomBooking.Application.TimeSlots;
using MeetingRoomBooking.Web.Models.TimeSlots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MeetingRoomBooking.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MeetingRoomBooking.Web.Controllers;

[Authorize]
[Route("meeting-rooms/{meetingRoomId:guid}/bookings")]
public sealed class BookingsController(
    IBookingCommands bookingCommands,
    IMeetingRoomQueries meetingRoomQueries,
    ITimeSlotQueries timeSlotQueries,
    TimeProvider timeProvider,
    IHubContext<RoomScheduleHub> hubContext,
    ILogger<BookingsController> logger) : Controller
{
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromRoute] Guid meetingRoomId,
        [FromForm] CreateBookingRequest request,
        [FromForm] DateOnly? date,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid booking request.");
        }

        if (!Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId) ||
            userId == Guid.Empty)
        {
            return Forbid();
        }

        var selectedDate = date
            ?? DateOnly.FromDateTime(
                timeProvider.GetUtcNow().UtcDateTime);

        if (selectedDate == DateOnly.MaxValue)
        {
            return BadRequest("Date is outside the supported range.");
        }

        // The room comes from the route, not from a form field.
        request.MeetingRoomId = meetingRoomId;

        var result = await bookingCommands.CreateAsync(
            userId,
            request,
            cancellationToken);

        if (result == BookingCreationResult.Success)
        {
            await NotifySlotsBookedAsync(
                meetingRoomId,
                request.TimeSlotIds.ToArray());

            TempData["SuccessMessage"] =
                "Your booking was created successfully.";

            return RedirectToRoute(
                "MeetingRoomSchedule",
                new
                {
                    id = meetingRoomId,
                    date = selectedDate.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture)
                });
        }

        if (result == BookingCreationResult.MeetingRoomNotFound)
        {
            return NotFound();
        }

        var errorMessage = result switch
        {
            BookingCreationResult.InvalidSlotSelection =>
                "Select valid time slots from this room without duplicates.",

            BookingCreationResult.SlotsNotConsecutive =>
                "Select consecutive slots without gaps between them.",

            BookingCreationResult.SlotAlreadyStarted =>
                "You cannot book a slot that has already started.",

            BookingCreationResult.Conflict =>
                "One or more selected slots are already booked. "
                + "Please choose other slots.",

            _ => throw new InvalidOperationException(
                $"Unexpected booking creation result: {result}")
        };

        var room = await meetingRoomQueries.GetActiveByIdAsync(
            meetingRoomId,
            cancellationToken);

        if (room is null)
        {
            return NotFound();
        }

        var slots = await timeSlotQueries.GetByRoomAndDateAsync(
            meetingRoomId,
            selectedDate,
            cancellationToken);

        Response.StatusCode =
            result == BookingCreationResult.Conflict
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;

        ViewData["BookingError"] = errorMessage;

        return View(
            "~/Views/MeetingRooms/Schedule.cshtml",
            new RoomScheduleViewModel
            {
                Room = room,
                Date = selectedDate,
                Slots = slots
            });
    }

    private async Task NotifySlotsBookedAsync(
    Guid meetingRoomId,
    Guid[] timeSlotIds)
    {
        try
        {
            using var timeout = new CancellationTokenSource(
                TimeSpan.FromSeconds(5));

            await hubContext.Clients
                .Group(RoomScheduleHub.GetGroupName(meetingRoomId))
                .SendAsync(
                    "SlotsBooked",
                    new
                    {
                        MeetingRoomId = meetingRoomId,
                        TimeSlotIds = timeSlotIds
                    },
                    timeout.Token);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Booking was saved, but the SignalR notification "
                    + "failed for room {MeetingRoomId}.",
                meetingRoomId);
        }
    }
}
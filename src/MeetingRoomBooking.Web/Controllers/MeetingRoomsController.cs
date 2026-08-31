using MeetingRoomBooking.Application.MeetingRooms;
using MeetingRoomBooking.Application.TimeSlots;
using MeetingRoomBooking.Web.Models.TimeSlots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRoomBooking.Web.Controllers;

[Authorize]
[Route("meeting-rooms")]
public sealed class MeetingRoomsController(
    IMeetingRoomQueries meetingRoomQueries,
    ITimeSlotQueries timeSlotQueries) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var rooms = await meetingRoomQueries.GetActiveAsync(
            cancellationToken);

        return View(rooms);
    }

    [HttpGet("{id:guid}", Name = "MeetingRoomSchedule")]
    public async Task<IActionResult> Schedule(
        Guid id,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid date.");
        }

        var room = await meetingRoomQueries.GetActiveByIdAsync(
            id,
            cancellationToken);

        if (room is null)
        {
            return NotFound();
        }

        var selectedDate = date
            ?? DateOnly.FromDateTime(DateTime.UtcNow);

        if (selectedDate == DateOnly.MaxValue)
        {
            return BadRequest("Date is outside the supported range.");
        }

        var slots = await timeSlotQueries.GetByRoomAndDateAsync(
            id,
            selectedDate,
            cancellationToken);

        return View(new RoomScheduleViewModel
        {
            Room = room,
            Date = selectedDate,
            Slots = slots
        });
    }
}
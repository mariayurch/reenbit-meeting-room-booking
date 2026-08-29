using MeetingRoomBooking.Application.MeetingRooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRoomBooking.Web.Controllers;

[Authorize]
[Route("meeting-rooms")]
public sealed class MeetingRoomsController(
    IMeetingRoomQueries meetingRoomQueries) : Controller
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
        CancellationToken cancellationToken)
    {
        var room = await meetingRoomQueries.GetActiveByIdAsync(
            id,
            cancellationToken);

        if (room is null)
        {
            return NotFound();
        }

        return View(room);
    }
}
using MeetingRoomBooking.Application.MeetingRooms;
using MeetingRoomBooking.Application.MeetingRooms.Models;
using MeetingRoomBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRoomBooking.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
[Route("admin/meeting-rooms")]
public sealed class AdminMeetingRoomsController(
    IMeetingRoomQueries meetingRoomQueries,
    IMeetingRoomCommands meetingRoomCommands) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var rooms = await meetingRoomQueries.GetAllAsync(
            cancellationToken);

        return View(rooms);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new CreateMeetingRoomRequest());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateMeetingRoomRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var created = await meetingRoomCommands.CreateAsync(
            request,
            cancellationToken);

        if (!created)
        {
            ModelState.AddModelError(
                nameof(request.Name),
                "A meeting room with this name already exists.");

            return View(request);
        }

        TempData["SuccessMessage"] =
            "Meeting room was created successfully.";

        return RedirectToAction(nameof(Index));
    }
}
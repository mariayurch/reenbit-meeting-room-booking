using MeetingRoomBooking.Application.MeetingRooms;
using MeetingRoomBooking.Application.TimeSlots;
using MeetingRoomBooking.Application.TimeSlots.Models;
using MeetingRoomBooking.Infrastructure.Identity;
using MeetingRoomBooking.Web.Models.TimeSlots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRoomBooking.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
[Route("admin/meeting-rooms/{meetingRoomId:guid}/time-slots")]
public sealed class AdminTimeSlotsController(
    IMeetingRoomQueries meetingRoomQueries,
    ITimeSlotCommands timeSlotCommands) : Controller
{
    [HttpGet("create")]
    public async Task<IActionResult> Create(
        [FromRoute] Guid meetingRoomId,
        CancellationToken cancellationToken)
    {
        var room = await meetingRoomQueries.GetActiveByIdAsync(
            meetingRoomId,
            cancellationToken);

        if (room is null)
        {
            return NotFound();
        }

        return View(new CreateTimeSlotViewModel
        {
            MeetingRoomId = room.Id,
            RoomName = room.Name,
            Date = DateOnly.FromDateTime(DateTime.UtcNow)
        });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromRoute] Guid meetingRoomId,
        CreateTimeSlotViewModel model,
        CancellationToken cancellationToken)
    {
        var room = await meetingRoomQueries.GetActiveByIdAsync(
            meetingRoomId,
            cancellationToken);

        if (room is null)
        {
            return NotFound();
        }

        model.MeetingRoomId = room.Id;
        model.RoomName = room.Name;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new CreateTimeSlotRequest
        {
            MeetingRoomId = room.Id,
            Date = model.Date!.Value,
            StartTime = model.StartTime!.Value,
            EndTime = model.EndTime!.Value
        };

        var result = await timeSlotCommands.CreateAsync(
            request,
            cancellationToken);

        switch (result)
        {
            case TimeSlotCreationResult.Success:
                TempData["SuccessMessage"] =
                    "Time slot was created successfully.";

                return RedirectToAction(
                    nameof(Create),
                    new { meetingRoomId });

            case TimeSlotCreationResult.MeetingRoomNotFound:
                return NotFound();

            case TimeSlotCreationResult.InvalidTimeRange:
                ModelState.AddModelError(
                    string.Empty,
                    "Choose a valid date and an end time after the start time.");
                break;

            case TimeSlotCreationResult.OverlappingSlot:
                ModelState.AddModelError(
                    string.Empty,
                    "This time slot overlaps an existing slot.");
                break;

            default:
                throw new InvalidOperationException(
                    $"Unexpected time slot creation result: {result}");
        }

        return View(model);
    }
}
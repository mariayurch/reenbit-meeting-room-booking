using MeetingRoomBooking.Application.MeetingRooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MeetingRoomBooking.Application.TimeSlots;
using System.Globalization;

namespace MeetingRoomBooking.Web.Hubs;

[Authorize]
public sealed class RoomScheduleHub(
    IMeetingRoomQueries meetingRoomQueries,
    ITimeSlotQueries timeSlotQueries) : Hub
{
    public async Task JoinRoom(Guid meetingRoomId)
    {
        var room = await meetingRoomQueries.GetActiveByIdAsync(
            meetingRoomId,
            Context.ConnectionAborted);

        if (room is null)
        {
            throw new HubException("Meeting room was not found.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetGroupName(meetingRoomId),
            Context.ConnectionAborted);
    }

    public async Task<IReadOnlyList<Guid>> GetBookedSlotIds(
        Guid meetingRoomId,
        string date)
    {
        var room = await meetingRoomQueries.GetActiveByIdAsync(
            meetingRoomId,
            Context.ConnectionAborted);

        if (room is null)
        {
            throw new HubException("Meeting room was not found.");
        }

        if (!DateOnly.TryParseExact(
                date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var selectedDate))
        {
            throw new HubException("Invalid schedule date.");
        }

        var slots = await timeSlotQueries.GetByRoomAndDateAsync(
            meetingRoomId,
            selectedDate,
            Context.ConnectionAborted);

        return slots
            .Where(slot => slot.IsBooked)
            .Select(slot => slot.Id)
            .ToList();
    }

    public Task LeaveRoom(Guid meetingRoomId)
    {
        return Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GetGroupName(meetingRoomId),
            Context.ConnectionAborted);
    }

    public static string GetGroupName(Guid meetingRoomId)
    {
        return $"room:{meetingRoomId:D}";
    }
}
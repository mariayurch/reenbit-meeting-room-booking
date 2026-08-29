using MeetingRoomBooking.Application.MeetingRooms.Models;

namespace MeetingRoomBooking.Application.MeetingRooms;

public interface IMeetingRoomCommands
{
    Task<bool> CreateAsync(
        CreateMeetingRoomRequest request,
        CancellationToken cancellationToken = default);
}
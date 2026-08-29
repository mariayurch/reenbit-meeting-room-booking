namespace MeetingRoomBooking.Application.MeetingRooms.Models;

public sealed record MeetingRoomListItem(
    Guid Id,
    string Name,
    string? Description,
    int Capacity,
    bool IsActive);
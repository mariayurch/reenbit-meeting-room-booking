namespace MeetingRoomBooking.Application.MeetingRooms.Models;

public sealed record MeetingRoomDetails(
    Guid Id,
    string Name,
    string? Description,
    int Capacity);
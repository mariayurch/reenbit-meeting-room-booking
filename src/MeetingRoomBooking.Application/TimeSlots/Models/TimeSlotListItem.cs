namespace MeetingRoomBooking.Application.TimeSlots.Models;

public sealed record TimeSlotListItem(
    Guid Id,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    bool IsBooked);
namespace MeetingRoomBooking.Application.TimeSlots;

public enum TimeSlotCreationResult
{
    Success,
    MeetingRoomNotFound,
    InvalidTimeRange,
    OverlappingSlot
}
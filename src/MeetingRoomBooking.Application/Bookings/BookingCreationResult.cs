namespace MeetingRoomBooking.Application.Bookings;

public enum BookingCreationResult
{
    Success,
    MeetingRoomNotFound,
    InvalidSlotSelection,
    SlotsNotConsecutive,
    SlotAlreadyStarted,
    Conflict
}
namespace MeetingRoomBooking.Application.Bookings.Models;

public sealed class CreateBookingRequest
{
    public Guid MeetingRoomId { get; set; }

    public List<Guid> TimeSlotIds { get; set; } = [];
}
using MeetingRoomBooking.Domain.Bookings;

namespace MeetingRoomBooking.Application.Bookings.Models;

public sealed record AdminBookingListItem(
    Guid Id,
    string RoomName,
    string UserEmail,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    BookingStatus Status);
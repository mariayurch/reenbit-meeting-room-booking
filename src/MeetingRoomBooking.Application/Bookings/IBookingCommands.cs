using MeetingRoomBooking.Application.Bookings.Models;

namespace MeetingRoomBooking.Application.Bookings;

public interface IBookingCommands
{
    Task<BookingCreationResult> CreateAsync(
        Guid userId,
        CreateBookingRequest request,
        CancellationToken cancellationToken = default);
}
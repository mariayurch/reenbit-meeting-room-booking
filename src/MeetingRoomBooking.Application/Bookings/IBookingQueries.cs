using MeetingRoomBooking.Application.Bookings.Models;

namespace MeetingRoomBooking.Application.Bookings;

public interface IBookingQueries
{
    Task<IReadOnlyList<AdminBookingListItem>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
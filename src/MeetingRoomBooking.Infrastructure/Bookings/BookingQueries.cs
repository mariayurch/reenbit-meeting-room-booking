using MeetingRoomBooking.Application.Bookings;
using MeetingRoomBooking.Application.Bookings.Models;
using MeetingRoomBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MeetingRoomBooking.Infrastructure.Bookings;

public sealed class BookingQueries(
    ApplicationDbContext dbContext) : IBookingQueries
{
    public async Task<IReadOnlyList<AdminBookingListItem>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await (
            from booking in dbContext.Bookings.AsNoTracking()
            join room in dbContext.MeetingRooms.AsNoTracking()
                on booking.MeetingRoomId equals room.Id
            join user in dbContext.Users.AsNoTracking()
                on booking.UserId equals user.Id
            orderby booking.StartUtc descending
            select new AdminBookingListItem(
                booking.Id,
                room.Name,
                user.Email ?? "Unknown user",
                booking.StartUtc,
                booking.EndUtc,
                booking.Status))
            .ToListAsync(cancellationToken);
    }
}
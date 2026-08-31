using MeetingRoomBooking.Application.TimeSlots;
using MeetingRoomBooking.Application.TimeSlots.Models;
using MeetingRoomBooking.Domain.Bookings;
using MeetingRoomBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MeetingRoomBooking.Infrastructure.TimeSlots;

public sealed class TimeSlotQueries(
    ApplicationDbContext dbContext) : ITimeSlotQueries
{
    public async Task<IReadOnlyList<TimeSlotListItem>> GetByRoomAndDateAsync(
        Guid meetingRoomId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var dayStartUtc = new DateTimeOffset(
            date.ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero);

        var nextDayStartUtc = dayStartUtc.AddDays(1);

        return await dbContext.TimeSlots
            .AsNoTracking()
            .Where(slot =>
                slot.MeetingRoomId == meetingRoomId &&
                slot.StartUtc >= dayStartUtc &&
                slot.StartUtc < nextDayStartUtc)
            .OrderBy(slot => slot.StartUtc)
            .Select(slot => new TimeSlotListItem(
                slot.Id,
                slot.StartUtc,
                slot.EndUtc,
                slot.Bookings.Any(booking =>
                    booking.Status == BookingStatus.Confirmed)))
            .ToListAsync(cancellationToken);
    }
}
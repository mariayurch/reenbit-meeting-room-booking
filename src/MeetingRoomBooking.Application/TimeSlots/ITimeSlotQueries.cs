using MeetingRoomBooking.Application.TimeSlots.Models;

namespace MeetingRoomBooking.Application.TimeSlots;

public interface ITimeSlotQueries
{
    Task<IReadOnlyList<TimeSlotListItem>> GetByRoomAndDateAsync(
        Guid meetingRoomId,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
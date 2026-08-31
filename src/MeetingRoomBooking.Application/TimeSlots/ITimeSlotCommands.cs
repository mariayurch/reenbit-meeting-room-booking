using MeetingRoomBooking.Application.TimeSlots.Models;

namespace MeetingRoomBooking.Application.TimeSlots;

public interface ITimeSlotCommands
{
    Task<TimeSlotCreationResult> CreateAsync(
        CreateTimeSlotRequest request,
        CancellationToken cancellationToken = default);
}
using MeetingRoomBooking.Application.MeetingRooms.Models;

namespace MeetingRoomBooking.Application.MeetingRooms;

public interface IMeetingRoomQueries
{
    Task<IReadOnlyList<MeetingRoomListItem>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<EditMeetingRoomRequest?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeetingRoomListItem>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<MeetingRoomDetails?> GetActiveByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
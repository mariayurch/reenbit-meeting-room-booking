using MeetingRoomBooking.Application.MeetingRooms;
using MeetingRoomBooking.Application.MeetingRooms.Models;
using MeetingRoomBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MeetingRoomBooking.Infrastructure.MeetingRooms;

public sealed class MeetingRoomQueries(
    ApplicationDbContext dbContext) : IMeetingRoomQueries
{
    public async Task<IReadOnlyList<MeetingRoomListItem>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.MeetingRooms
            .AsNoTracking()
            .OrderBy(room => room.Name)
            .Select(room => new MeetingRoomListItem(
                room.Id,
                room.Name,
                room.Description,
                room.Capacity,
                room.IsActive))
            .ToListAsync(cancellationToken);
    }
}
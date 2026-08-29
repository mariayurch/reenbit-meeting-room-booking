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

    public async Task<IReadOnlyList<MeetingRoomListItem>> GetActiveAsync(
    CancellationToken cancellationToken = default)
    {
        return await dbContext.MeetingRooms
            .AsNoTracking()
            .Where(room => room.IsActive)
            .OrderBy(room => room.Name)
            .Select(room => new MeetingRoomListItem(
                room.Id,
                room.Name,
                room.Description,
                room.Capacity,
                room.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<EditMeetingRoomRequest?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        return await dbContext.MeetingRooms
            .AsNoTracking()
            .Where(room => room.Id == id)
            .Select(room => new EditMeetingRoomRequest
            {
                Id = room.Id,
                Name = room.Name,
                Description = room.Description,
                Capacity = room.Capacity
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<MeetingRoomDetails?> GetActiveByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        return await dbContext.MeetingRooms
            .AsNoTracking()
            .Where(room => room.Id == id && room.IsActive)
            .Select(room => new MeetingRoomDetails(
                room.Id,
                room.Name,
                room.Description,
                room.Capacity))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
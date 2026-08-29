using MeetingRoomBooking.Application.MeetingRooms;
using MeetingRoomBooking.Application.MeetingRooms.Models;
using MeetingRoomBooking.Domain.MeetingRooms;
using MeetingRoomBooking.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MeetingRoomBooking.Infrastructure.MeetingRooms;

public sealed class MeetingRoomCommands(
    ApplicationDbContext dbContext) : IMeetingRoomCommands
{
    public async Task<bool> CreateAsync(
        CreateMeetingRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = request.Name.Trim();

        var roomAlreadyExists = await dbContext.MeetingRooms
            .AnyAsync(
                room => room.Name == normalizedName,
                cancellationToken);

        if (roomAlreadyExists)
        {
            return false;
        }

        var meetingRoom = new MeetingRoom(
            normalizedName,
            request.Capacity,
            request.Description);

        await dbContext.MeetingRooms.AddAsync(
            meetingRoom,
            cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException
            {
                Number: 2601 or 2627
            })
        {
            dbContext.Entry(meetingRoom).State = EntityState.Detached;
            return false;
        }
    }
}
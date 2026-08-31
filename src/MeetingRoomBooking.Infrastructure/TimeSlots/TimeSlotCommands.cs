using System.Data;
using MeetingRoomBooking.Application.TimeSlots;
using MeetingRoomBooking.Application.TimeSlots.Models;
using MeetingRoomBooking.Domain.TimeSlots;
using MeetingRoomBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MeetingRoomBooking.Infrastructure.TimeSlots;

public sealed class TimeSlotCommands(
    ApplicationDbContext dbContext) : ITimeSlotCommands
{
    public async Task<TimeSlotCreationResult> CreateAsync(
        CreateTimeSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Date == default ||
            request.EndTime <= request.StartTime)
        {
            return TimeSlotCreationResult.InvalidTimeRange;
        }

        var startUtc = new DateTimeOffset(
            request.Date.ToDateTime(request.StartTime),
            TimeSpan.Zero);

        var endUtc = new DateTimeOffset(
            request.Date.ToDateTime(request.EndTime),
            TimeSpan.Zero);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        // Serialize slot creation for the same room.
        // Every slot-creation path must acquire this lock first.
        var room = await dbContext.MeetingRooms
            .FromSqlInterpolated($"""
                SELECT *
                FROM [MeetingRooms] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {request.MeetingRoomId}
                """)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (room is null || !room.IsActive)
        {
            return TimeSlotCreationResult.MeetingRoomNotFound;
        }

        var overlaps = await dbContext.TimeSlots
            .AnyAsync(
                slot =>
                    slot.MeetingRoomId == request.MeetingRoomId &&
                    slot.StartUtc < endUtc &&
                    slot.EndUtc > startUtc,
                cancellationToken);

        if (overlaps)
        {
            return TimeSlotCreationResult.OverlappingSlot;
        }

        var timeSlot = new TimeSlot(
            request.MeetingRoomId,
            startUtc,
            endUtc);

        dbContext.TimeSlots.Add(timeSlot);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return TimeSlotCreationResult.Success;
    }
}
using System.Data;
using MeetingRoomBooking.Application.Bookings;
using MeetingRoomBooking.Application.Bookings.Models;
using MeetingRoomBooking.Domain.Bookings;
using MeetingRoomBooking.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MeetingRoomBooking.Infrastructure.Bookings;

public sealed class BookingCommands(
    ApplicationDbContext dbContext,
    TimeProvider timeProvider) : IBookingCommands
{
    public async Task<BookingCreationResult> CreateAsync(
        Guid userId,
        CreateBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User identifier is required.",
                nameof(userId));
        }

        if (request.TimeSlotIds is null ||
            request.TimeSlotIds.Count == 0)
        {
            return BookingCreationResult.InvalidSlotSelection;
        }

        var slotIds = request.TimeSlotIds.ToArray();

        if (slotIds.Contains(Guid.Empty) ||
            slotIds.Distinct().Count() != slotIds.Length)
        {
            return BookingCreationResult.InvalidSlotSelection;
        }

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        // All booking writers must acquire the room lock first.
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
            return BookingCreationResult.MeetingRoomNotFound;
        }

        var slots = await dbContext.TimeSlots
            .Where(slot =>
                slot.MeetingRoomId == request.MeetingRoomId &&
                slotIds.Contains(slot.Id))
            .OrderBy(slot => slot.StartUtc)
            .ToListAsync(cancellationToken);

        if (slots.Count != slotIds.Length)
        {
            return BookingCreationResult.InvalidSlotSelection;
        }

        for (var index = 1; index < slots.Count; index++)
        {
            if (slots[index - 1].EndUtc != slots[index].StartUtc)
            {
                return BookingCreationResult.SlotsNotConsecutive;
            }
        }

        if (slots[0].StartUtc <= timeProvider.GetUtcNow())
        {
            return BookingCreationResult.SlotAlreadyStarted;
        }

        var hasConflict = await dbContext.TimeSlots
            .AnyAsync(
                slot =>
                    slotIds.Contains(slot.Id) &&
                    slot.Bookings.Any(booking =>
                        booking.Status == BookingStatus.Confirmed),
                cancellationToken);

        if (hasConflict)
        {
            return BookingCreationResult.Conflict;
        }

        var booking = new Booking(userId, slots);

        dbContext.Bookings.Add(booking);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsSlotUniquenessViolation(exception))
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            // Remove the failed booking and join entries from tracking.
            // This command owns the current persistence operation.
            dbContext.ChangeTracker.Clear();

            return BookingCreationResult.Conflict;
        }

        await transaction.CommitAsync(cancellationToken);

        return BookingCreationResult.Success;
    }

    private static bool IsSlotUniquenessViolation(
        DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
            && (sqlException.Number is 2601 or 2627)
            && sqlException.Message.Contains(
                "UX_BookingTimeSlots_TimeSlotId",
                StringComparison.OrdinalIgnoreCase);
    }
}
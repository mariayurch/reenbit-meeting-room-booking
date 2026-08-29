using MeetingRoomBooking.Domain.TimeSlots;

namespace MeetingRoomBooking.Domain.Bookings;

public sealed class Booking
{
    private Booking()
    {
    }

    public Booking(Guid userId, IEnumerable<TimeSlot> timeSlots)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User identifier is required.",
                nameof(userId));
        }

        ArgumentNullException.ThrowIfNull(timeSlots);

        var orderedSlots = timeSlots
            .OrderBy(slot => slot.StartUtc)
            .ToList();

        if (orderedSlots.Count == 0)
        {
            throw new ArgumentException(
                "A booking must contain at least one time slot.",
                nameof(timeSlots));
        }

        var meetingRoomId = orderedSlots[0].MeetingRoomId;

        if (orderedSlots.Any(slot =>
                slot.MeetingRoomId != meetingRoomId))
        {
            throw new ArgumentException(
                "All time slots must belong to the same meeting room.",
                nameof(timeSlots));
        }

        for (var index = 1; index < orderedSlots.Count; index++)
        {
            var previousSlot = orderedSlots[index - 1];
            var currentSlot = orderedSlots[index];

            if (previousSlot.EndUtc != currentSlot.StartUtc)
            {
                throw new ArgumentException(
                    "All time slots must be consecutive.",
                    nameof(timeSlots));
            }
        }

        Id = Guid.NewGuid();
        UserId = userId;
        MeetingRoomId = meetingRoomId;
        StartUtc = orderedSlots[0].StartUtc;
        EndUtc = orderedSlots[^1].EndUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        Status = BookingStatus.Confirmed;
        TimeSlots = orderedSlots;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid MeetingRoomId { get; private set; }

    public DateTimeOffset StartUtc { get; private set; }

    public DateTimeOffset EndUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public BookingStatus Status { get; private set; }

    public ICollection<TimeSlot> TimeSlots { get; private set; }
        = new List<TimeSlot>();

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
        {
            return;
        }

        Status = BookingStatus.Cancelled;
        TimeSlots.Clear();
    }
}
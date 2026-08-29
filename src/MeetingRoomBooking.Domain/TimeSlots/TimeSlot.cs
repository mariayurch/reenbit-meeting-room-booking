using MeetingRoomBooking.Domain.MeetingRooms;
using MeetingRoomBooking.Domain.Bookings;

namespace MeetingRoomBooking.Domain.TimeSlots;

public sealed class TimeSlot
{
    private TimeSlot()
    {
    }

    public TimeSlot(
        Guid meetingRoomId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc)
    {
        if (meetingRoomId == Guid.Empty)
        {
            throw new ArgumentException(
                "Meeting room identifier is required.",
                nameof(meetingRoomId));
        }

        startUtc = startUtc.ToUniversalTime();
        endUtc = endUtc.ToUniversalTime();

        if (endUtc <= startUtc)
        {
            throw new ArgumentException(
                "The end of a time slot must be later than its start.",
                nameof(endUtc));
        }

        Id = Guid.NewGuid();
        MeetingRoomId = meetingRoomId;
        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    public Guid Id { get; private set; }

    public Guid MeetingRoomId { get; private set; }

    public DateTimeOffset StartUtc { get; private set; }

    public DateTimeOffset EndUtc { get; private set; }

    public MeetingRoom MeetingRoom { get; private set; } = null!;

    public ICollection<Booking> Bookings { get; private set; }
    = new List<Booking>();
}
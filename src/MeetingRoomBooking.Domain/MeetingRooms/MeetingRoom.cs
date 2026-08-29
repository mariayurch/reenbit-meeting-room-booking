namespace MeetingRoomBooking.Domain.MeetingRooms;
using MeetingRoomBooking.Domain.TimeSlots;

public sealed class MeetingRoom
{
    private MeetingRoom()
    {
    }

    public MeetingRoom(string name, int capacity, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Meeting room name is required.",
                nameof(name));
        }

        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                "Meeting room capacity must be greater than zero.");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Capacity = capacity;
        Description = description?.Trim();
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int Capacity { get; private set; }

    public bool IsActive { get; private set; }

    public ICollection<TimeSlot> TimeSlots { get; private set; }
    = new List<TimeSlot>();
}
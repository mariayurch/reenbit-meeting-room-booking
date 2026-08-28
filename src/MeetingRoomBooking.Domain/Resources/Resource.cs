namespace MeetingRoomBooking.Domain.Resources;

public sealed class Resource
{
    private Resource()
    {
    }

    public Resource(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Resource name cannot be empty.",
                nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
}
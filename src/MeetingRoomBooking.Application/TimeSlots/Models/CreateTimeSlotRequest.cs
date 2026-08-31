using System.ComponentModel.DataAnnotations;

namespace MeetingRoomBooking.Application.TimeSlots.Models;

public sealed class CreateTimeSlotRequest
{
    public Guid MeetingRoomId { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public TimeOnly StartTime { get; set; }

    [Required]
    public TimeOnly EndTime { get; set; }
}
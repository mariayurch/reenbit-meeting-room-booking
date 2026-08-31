using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MeetingRoomBooking.Web.Models.TimeSlots;

public sealed class CreateTimeSlotViewModel
{
    [BindNever]
    public Guid MeetingRoomId { get; set; }

    [BindNever]
    public string RoomName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateOnly? Date { get; set; }

    [Required]
    [DataType(DataType.Time)]
    public TimeOnly? StartTime { get; set; }

    [Required]
    [DataType(DataType.Time)]
    public TimeOnly? EndTime { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace MeetingRoomBooking.Application.MeetingRooms.Models;

public sealed class CreateMeetingRoomRequest
{
    [Required(ErrorMessage = "Room name is required.")]
    [StringLength(
        100,
        ErrorMessage = "Room name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(
        500,
        ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Capacity must be greater than zero.")]
    public int Capacity { get; set; } = 1;
}
using System.ComponentModel.DataAnnotations;
using MeetingRoomBooking.Application.MeetingRooms.Models;
using MeetingRoomBooking.Application.TimeSlots.Models;

namespace MeetingRoomBooking.Web.Models.TimeSlots;

public sealed class RoomScheduleViewModel
{
    public required MeetingRoomDetails Room { get; init; }

    [DataType(DataType.Date)]
    public DateOnly Date { get; init; }

    public IReadOnlyList<TimeSlotListItem> Slots { get; init; }
        = Array.Empty<TimeSlotListItem>();
}
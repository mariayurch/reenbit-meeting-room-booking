using MeetingRoomBooking.Application.Bookings;
using MeetingRoomBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRoomBooking.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
[Route("admin/bookings")]
public sealed class AdminBookingsController(
    IBookingQueries bookingQueries) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var bookings = await bookingQueries.GetAllAsync(
            cancellationToken);

        return View(bookings);
    }
}
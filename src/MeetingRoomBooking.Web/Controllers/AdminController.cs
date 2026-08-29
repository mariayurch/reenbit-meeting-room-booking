using MeetingRoomBooking.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRoomBooking.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Admin)]
public sealed class AdminController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
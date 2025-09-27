using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBookDashboard.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.UserName = User.Identity?.Name ?? "User";
            ViewBag.UserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
            ViewBag.AuthorName = User.FindFirst("AuthorName")?.Value ?? "";
            ViewBag.Genre = User.FindFirst("Genre")?.Value ?? "";
            return View();
        }

        public IActionResult EBook()
        {
            ViewBag.UserName = User.Identity?.Name ?? "User";
            return View();
        }

        public IActionResult CoverDesign()
        {
            ViewBag.UserName = User.Identity?.Name ?? "User";
            return View();
        }

        public IActionResult AudioBook()
        {
            ViewBag.UserName = User.Identity?.Name ?? "User";
            return View();
        }

        public IActionResult EditingFormatting()
        {
            ViewBag.UserName = User.Identity?.Name ?? "User";
            return View();
        }

        public IActionResult Proofreading()
        {
            ViewBag.UserName = User.Identity?.Name ?? "User";
            return View();
        }

        public IActionResult Publishing()
        {
            ViewBag.UserName = User.Identity?.Name ?? "User";
            return View();
        }

        public IActionResult Marketing()
        {
            ViewBag.UserName = User.Identity?.Name ?? "User";
            return View();
        }

        public IActionResult Royalties()
        {
            ViewBag.UserName = User.Identity?.Name ?? "User";
            return View();
        }

        public IActionResult OrderAuthorCopy()
        {
            ViewBag.UserName = User.Identity?.Name ?? "User";
            return View();
        }

        public IActionResult Copyright()
        {
            ViewBag.UserName = User.Identity?.Name ?? "User";
            return View();
        }
    }
}
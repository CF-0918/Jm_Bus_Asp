using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;

public class HomeController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;

    public HomeController(DB db, IWebHostEnvironment en, Helper hp)
    {

        this.db = db;
        this.en = en;
        this.hp = hp;
    }

    public IActionResult Index()
    {
        if (User.IsInRole("Member"))
        {
            // Check if the user is subscribed
            bool isSubscribed = db.Members.Any(m => m.Id == User.Identity.Name && m.IsSubscribedToNewsletter);

            // Check if the user has dismissed the modal (stored in session)
            string sessionKey = $"close_{User.Identity.Name}";
            bool isModalDismissed = HttpContext.Session.GetString(sessionKey) == "true";

            // Set ViewBag.IsSubscriber based on the subscription and modal state
            ViewBag.IsSubscriber = !isSubscribed && !isModalDismissed;
        }
        else
        {
            // Non-members should not see the modal
            ViewBag.IsSubscriber = false;
        }

        return View();
    }

    [HttpPost]
    public IActionResult Index(HomeView vm)
    {
        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Member")]
    public IActionResult SetCloseNewsLetterSession()
    {
        if (User.Identity.IsAuthenticated)
        {
            HttpContext.Session.SetString($"close_{User.Identity.Name}", "true");
        }
        return Json(new { success = true });
    }

    [Authorize(Roles = "Member")]
    public IActionResult RentBusService()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }
    // New Chat action
    public IActionResult Chat()
    {
        return View();
    }
}

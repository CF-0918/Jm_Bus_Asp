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
            // Check if the user is not subscribed and hasn't dismissed the modal
            ViewBag.IsSubscriber = HttpContext.Session.GetString($"close_{User.Identity.Name}") != "true";
        }

        return View();
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

}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
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

using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers
{
    public class MaintenanceController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }
        public IActionResult MemberMaintenance()
        {
            return View();
        }
    }
}

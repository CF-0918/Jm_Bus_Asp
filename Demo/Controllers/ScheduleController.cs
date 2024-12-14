using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;

public class ScheduleController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;

    public ScheduleController(DB db, IWebHostEnvironment en, Helper hp)
    {

        this.db = db;
        this.en = en;
        this.hp = hp;
    }

    public IActionResult AddSchedule()
    {
        return View();
    }
}

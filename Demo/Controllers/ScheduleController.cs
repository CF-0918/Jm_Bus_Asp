using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

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

    public string GetNextPrefixId(string tableName)
    {
        string currentMaxId = "";

        if (tableName == "Schedules")
        {
            currentMaxId = db.Schedules.Max(m => m.Id) ?? "OW0000"; // Default if table is empty
        }
        // Add more table cases as required
        else
        {
            throw new ArgumentException("Invalid table name.");
        }

        // Extract the prefix and numeric part
        string prefix = new string(currentMaxId.TakeWhile(char.IsLetter).ToArray()); // Extract prefix (e.g., "OW", "TW")
        string numericPart = currentMaxId.Substring(prefix.Length); // Extract numeric part (e.g., "0000")

        // Increment the numeric part
        int nextNumericPart = int.Parse(numericPart) + 1;

        // Format the next ID
        string nextId = $"{prefix}{nextNumericPart.ToString($"D{numericPart.Length}")}";

        return nextId;
    }

    public bool CheckBusAvailableSlot_After(string busId, DateOnly departDate, TimeOnly departTime, out string validationMessage)
    {
        validationMessage = string.Empty; // Initialize the validation message

        // Step 1: Fetch the current bus schedule and route information
        var theCurrentSelectedBus = db.Schedules
            .Where(s => s.BusId == busId && s.DepartDate == departDate) // Filter only by bus and date
            .Include(s => s.RouteLocation) // Assuming the schedule has a navigation property to RouteLocation
            .ToList(); // Fetch all schedules for the bus on the selected date

        // Step 2: Check each schedule's end time + buffer
        foreach (var schedule in theCurrentSelectedBus)
        {
            // Calculate the route duration (in minutes) from RouteLocation (e.g., 1 hour 30 minutes)
            var routeDurationInMinutes = schedule.RouteLocation.Hour * 60 + schedule.RouteLocation.Min;

            // Calculate the end time of the current schedule (depart time + route duration)
            var endTime = schedule.DepartTime.AddMinutes(routeDurationInMinutes);

            // Add the fixed buffer time (3 hours = 180 minutes) after the route completes
            var availableTime = endTime.AddMinutes(180); // 3 hours buffer

            // Step 3: Check if the user-selected time is before the available time
            if (departTime < availableTime)
            {
                // Suggest a new available time if the selected time is too early
                var suggestedTime = availableTime; // The next available time after buffer
                validationMessage = $"The bus will be available only after {availableTime:HH:mm} on {departDate:dd/MM/yyyy}. " +
                    $"Please select a time after this or try scheduling at {suggestedTime:HH:mm} for the next available slot.";
                return false; // Bus is not available at the selected time
            }
        }

        // If no conflicts found, bus is available at the selected time
        return true;
    }


    public IActionResult Index()
    {
        // Retrieve the schedule data and related entities (RouteLocation, Bus, and CategoryBus of Bus)
        var schedules = db.Schedules
            .Where(s => s.Status == "Active")
            .Include(s => s.RouteLocation)
            .Include(s => s.Bus)
            .Include(s => s.Bus.CategoryBus)  // Make sure CategoryBus is included
            .ToList();

        // Convert the schedules data into ScheduleDetailsVM
        var scheduleDetails = schedules.Select(s => new ScheduleDetailsVM
        {
            ScheduleId = s.Id.ToString(),
            DepartDate = s.DepartDate,
            DepartTime = s.DepartTime,
            Price = s.Price,
            DiscountPrice = s.DiscountPrice,
            Depart = s.RouteLocation.Depart,
            Destination = s.RouteLocation.Destination,
            Hour = s.RouteLocation.Hour,
            Min = s.RouteLocation.Min,

            // Populate Bus-related details
            BusId = s.Bus.Id.ToString(),
            BusName = s.Bus.Name,
            BusCapacity = s.Bus.Capacity.ToString(),
            BusPlate = s.Bus.BusPlate,
            CategoryBusName = s.Bus.CategoryBus != null ? s.Bus.CategoryBus.Name : "Unknown", // Assuming CategoryBus has a Name property
            PhotoURL = s.Bus.PhotoURL  // Assuming PhotoURL exists on the Bus model
        }).ToList();

        // Store the data in ViewBag
        ViewBag.ScheduleDetails = scheduleDetails;

        return View();
    }




    public IActionResult AddSchedule()
    {
       ViewBag.Buses= db.Buses.Where(b => b.Status == "Active");
       ViewBag.Routes = db.RouteLocations;
        return View();
    }

    [HttpPost]
    public IActionResult AddSchedule(ScheduleVM vm)
    {
        if (vm.Price <= 0)
        {
            ModelState.AddModelError("Price", "Price Should Not Be 0 Or Less Than.");
        }

        if (vm.DiscountPrice <0)
        {
            ModelState.AddModelError("Price", "Discount Price Should Not Be Negative.");
        }
        if (vm.DepartDate < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError("DepartDate", "Depart Date Should Not Be Past");
        }
        // Check if the bus is available at the selected time
        string busAvailabilityMessage;
        bool isBusAvailable = CheckBusAvailableSlot_After(vm.BusId, vm.DepartDate, vm.DepartTime, out busAvailabilityMessage);

        if (!isBusAvailable)
        {
            // Add the error message returned by the CheckBusAvailableSlot method
            ModelState.AddModelError("", busAvailabilityMessage+"Ops");
        }

        if (ModelState.IsValid)
        {
                string SchedulesId = GetNextPrefixId("Schedules");
                db.Schedules.Add(new()
                {
                    Id = SchedulesId,
                    DepartDate = vm.DepartDate,
                    DepartTime = vm.DepartTime,
                    Status = vm.Status,
                    Price = vm.Price,
                    DiscountPrice = vm.DiscountPrice,
                    Remark = vm.Remark,
                    BusId = vm.BusId,
                    RouteLocationId = vm.RouteId,
                });
                db.SaveChanges();
                TempData["Info"] = "One-Way Schedule Has Been Added.";
            return RedirectToAction("ShowScheduleList", "Schedule");

        }
        //if fail valdiate
        ViewBag.Buses = db.Buses.Where(b => b.Status == "Active");
        ViewBag.Routes = db.RouteLocations;
        return View(vm);
    }

    public IActionResult ShowScheduleList(DateOnly? name, string? sort, string? dir, int page = 1)
    {

        // (1) Searching ------------------------
        // If 'name' is null, set it to an empty string or a default value.
        ViewBag.Name = name.HasValue ? name.Value.ToString() : ""; // Use empty string if null

        // Filter schedules based on DepartDate if name has a value
        // Include the RouteLocation when fetching the schedules
        var searched = db.Schedules
            .Where(s => !name.HasValue || s.DepartDate == name.Value)
            .Include(s => s.RouteLocation); // Ensure RouteLocation is included



        // (2) Sorting --------------------------
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        Func<Schedule, object> fn = sort switch
        {
            "Id" => s => s.Id,
            "DepartDate" => s => s.DepartDate,
            "DepartTime" => s => s.DepartTime,
            "Status" => s => s.Status,
            _ => s => s.Id,
        };

        var sorted = dir == "des" ?
                     searched.OrderByDescending(fn) :
                     searched.OrderBy(fn);

        // (3) Paging ---------------------------
        if (page < 1)
        {
            return RedirectToAction(null, new { name, sort, dir, page = 1 });
        }

        var m = sorted.ToPagedList(page, 10);

        if (page > m.PageCount && m.PageCount > 0)
        {
            return RedirectToAction(null, new { name, sort, dir, page = m.PageCount });
        }


        if (Request.IsAjax())
        {
            return PartialView("_ScheduleList", m);
        }


        return View(m);

    }
}

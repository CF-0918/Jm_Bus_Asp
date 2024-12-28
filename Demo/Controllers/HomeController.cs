using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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

        // Retrieve the top 4 schedules with the highest DiscountPrice where the status is "Active"
        var ValueableSchedules = db.Schedules
       .Where(s => s.Status == "Active" && s.DiscountPrice > 0) // Exclude schedules with DiscountPrice of 0
       .OrderByDescending(s => s.DiscountPrice) // Sort by DiscountPrice in descending order
       .Take(4) // Limit the result to 4
       .Include(s => s.Bus)
       .Include(s => s.RouteLocation) // Adjust for your model structure
       .ToList(); // Execute the query and materialize the results

        // Pass the result to the ViewBag
        ViewBag.ValueableSchedules = ValueableSchedules;


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

    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult RentBusService(AddRentVM model)
    {
        if (ModelState.IsValid)
        {
            // Validate Start Date - ensure it is in the future
            if (model.Start_Date <= DateOnly.FromDateTime(DateTime.Now))
            {
                ModelState.AddModelError("Start_Date", "Start Date must be in the future.");
            }

            // Validate End Date - ensure it is greater than or equal to Start Date
            if (model.End_Date < model.Start_Date)
            {
                ModelState.AddModelError("End_Date", "End Date must be greater than or equal to Start Date.");
            }

            // Validate End Date - ensure it is in the future
            if (model.End_Date <= DateOnly.FromDateTime(DateTime.Now))
            {
                ModelState.AddModelError("End_Date", "End Date must be in the future.");
            }

            // Validate Number of People
            if (model.Numppl > 40)
            {
                ModelState.AddModelError("Numppl", "Number of people cannot exceed 40.");
            }

            if (ModelState.IsValid)
            {
                // Save the data
                Rent newRent = new Rent
                {
                    Id = GenerateRentId(),
                    Start_Date = model.Start_Date,
                    End_Date = model.End_Date,
                    DepTime = model.DepTime,
                    ArrTime = model.ArrTime,
                    Location = model.Location,
                    Destination = model.Destination,
                    Purpose = model.Purpose,
                    Numppl = model.Numppl,
                    PerIC = model.PerIC,
                    Phone = model.Phone,
                    Email = model.Email,
                    Req = model.Req,
                    status = "Pending",
                    MemberId = User.Identity.Name // Assuming you're fetching the user ID correctly
                };

                db.Rents.Add(newRent);
                db.SaveChanges();

                TempData["Info"] = "Rent booking successfully submitted!";
                return RedirectToAction("Index", "Home");
            }
        }

        // If validation fails, return the view with errors
        TempData["Info"] = "There were errors with your submission.";
        return View(model);
    }



    public IActionResult About()
    {
        return View();
    }
    // New Chat action
    [Authorize(Roles = "Member")]
    public IActionResult Chat()
    {
        return View();
    }

    // Jayden Partttttttt
    [Authorize(Roles = "Staff,Admin")]
    [HttpGet]
    public IActionResult RentList(string search, string sortBy, string sortDirection, string sortStatus)
    {
        var query = db.Rents.AsQueryable();

        // Apply search filter if search term is provided
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r => r.MemberId.Contains(search));
        }

        // Apply sorting based on sortBy and sortDirection
        if (!string.IsNullOrEmpty(sortBy))
        {
            if (sortDirection == "desc")
            {
                if (sortBy == "MemberId")
                    query = query.OrderByDescending(r => r.MemberId);
                else if (sortBy == "RentId")
                    query = query.OrderByDescending(r => r.Id);
            }
            else
            {
                if (sortBy == "MemberId")
                    query = query.OrderBy(r => r.MemberId);
                else if (sortBy == "RentId")
                    query = query.OrderBy(r => r.Id);
            }
        }

        // Apply additional filters like sortStatus
        if (!string.IsNullOrEmpty(sortStatus))
        {
            query = query.Where(r => r.status == sortStatus);
        }

        var rentList = query.Select(r => new RentHistoryVM
        {
            Id = r.Id,
            MemberId = r.MemberId,
            Start_Date = r.Start_Date,
            End_Date = r.End_Date,
            DepTime = r.DepTime,
            ArrTime = r.ArrTime,
            Numppl = r.Numppl,
            Status = r.status ?? "Pending"
        }).ToList();

        // If no results after search and sort
        if (!rentList.Any())
        {
            var noResultsMessage = string.IsNullOrEmpty(search) ? "No Record Yet" : "No Current Member";
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { message = noResultsMessage }); // Return as JSON for AJAX
            }

            ViewBag.Message = noResultsMessage; // Set for the full page view
            return View(rentList);
        }

        // Return rentList normally if there are results
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView("RentList", rentList);
        }

        return View(rentList);
    }


    [Authorize(Roles = "Staff,Admin")]
    public IActionResult RentDetails(string rentId)
    {
        if (rentId == null)
        {
            TempData["Info"] = "Please Provide A Rent Id";
            return RedirectToAction("RentList", "Home");
        }
        var rentDetail = db.Rents?
            .Where(r => r.Id == rentId)
            .Select(r => new RentHistoryVM
            {
                Id = r.Id,
                MemberId = r.MemberId,
                Start_Date = r.Start_Date,
                End_Date = r.End_Date,
                DepTime = r.DepTime,
                ArrTime = r.ArrTime,
                Location = r.Location,
                Destination = r.Destination,
                Purpose = r.Purpose,
                Numppl = r.Numppl,
                PerIC = r.PerIC,
                Phone = r.Phone,
                Email = r.Email,
                Req = r.Req,
                Status = r.status ?? "Pending"
            })
            .FirstOrDefault();

        if (rentDetail == null)
        {
            TempData["Info"] = "No Rent Founded";
            return RedirectToAction("RentList", "Home");
        }

        return View(rentDetail);
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPost]
    public IActionResult UpdateRentStatus(string rentId, string status)
    {
        // Fetch the rent by ID
        var rent = db.Rents.FirstOrDefault(r => r.Id == rentId);
        if (rent == null)
        {
            TempData["Message"] = "Rent not found.";
            return RedirectToAction("RentList");
        }

        // Update the status
        rent.status = status;
        db.SaveChanges();

        TempData["Message"] = $"Rent status updated to {status}.";
        return RedirectToAction("RentDetails", new { rentId = rent.Id });
    }

   

    [Authorize(Roles = "Member,Staff,Admin")]
    [HttpPost]
    public IActionResult CancelBooking(string rentId)
    {
        // Fetch the rent booking by its ID
        var rent = db.Rents.FirstOrDefault(r => r.Id == rentId);

        if (rent != null)
        {
            // Update the status to "Cancelled"
            rent.status = "Cancelled";

            // Save changes to the database
            db.SaveChanges();

            // Set a success message using TempData
            TempData["Cancelled"] = "Rent booking Cancelled!";

            if (User.IsInRole("Member"))
            {
                // Redirect to the same page or the Rent History page
                return RedirectToAction("MemberRentBookingHistory"); // Adjust to your action if needed
            }
            else
            {
                // Redirect to the same page or the Rent History page
                return RedirectToAction("RentHistory"); // Adjust to your action if needed
            }
        
        }

        // If rent not found, return an error message or a different page
        TempData["Cancelled"] = "Booking Not Found";
        return RedirectToAction("Index"); // Adjust to your action if needed
    }


    [Authorize(Roles ="Member")]
    public IActionResult MemberRentBookingHistory()
    {
        var rentHistory = db.Rents.Where(r=>r.MemberId==User.Identity.Name)
       .Select(r => new RentHistoryVM
       {
           Id = r.Id,
           Start_Date = r.Start_Date,
           End_Date = r.End_Date,
           DepTime = r.DepTime,
           ArrTime = r.ArrTime,
           Location = r.Location,
           Destination = r.Destination,
           Purpose = r.Purpose,
           Numppl = r.Numppl,
           PerIC = r.PerIC,
           Phone = r.Phone,
           Email = r.Email,
           Req = r.Req,
           Status = r.status ?? "Pending" // Ensure "Pending" is used if status is null
       })
       .OrderBy(r => r.Status == "Pending" ? 0 : 1)  // This will sort "Pending" entries first
       .ThenBy(r => r.Start_Date)  // Optionally, you can add more sorting (e.g., by Start Date)
       .ToList();

        return View(rentHistory);
    }


    // Handle form submission and save to database
    private string GenerateRentId()
    {
        // Get the last Rent record's Id
        var lastRent = db.Rents.OrderByDescending(r => r.Id).FirstOrDefault();

        if (lastRent != null && !string.IsNullOrWhiteSpace(lastRent.Id))
        {
            // Validate the format of the last ID
            if (lastRent.Id.StartsWith("R") && int.TryParse(lastRent.Id.Substring(1), out int lastIdNumber))
            {
                // Increment the numeric part and generate the new ID
                return "R" + (lastIdNumber + 1);
            }
            else
            {
                throw new InvalidOperationException("The last Rent ID is not in the expected format (e.g., 'R1').");
            }
        }

        // If no records exist, start from R1
        return "R1";
    }

}

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using X.PagedList.Extensions;
using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Demo.Migrations;
using System.Numerics;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

    private void SendScheduleSubscribeMail(string email, string firstName, string lastName, int price, int discountPrice, string depart, string destination, string scheduleId)
    {
        var fullName = $"{firstName} {lastName}";
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(email, fullName));

        mail.Subject = "Wow,Latest Schedule Bus Here Can Buy !";

        mail.IsBodyHtml = true;


        var UrlSchedule = Url.Action("SelectSeats", "Schedule", new { id = scheduleId }, "https");

        string discountSection = discountPrice > 0
            ? $@"
            <p style='font-size: 16px; line-height: 1.8; color: #555555; text-align: justify;'>
                We’re excited to bring you an amazing deal on your next journey! Travel from <strong>{depart}</strong> to <strong>{destination}</strong> at a special price of just 
                <span style='color: #d9534f; font-weight: bold;'>RM {discountPrice}</span> (original price: <span style='text-decoration: line-through;'>RM {price}</span>).
            </p>
            "
            : $@"
            <p style='font-size: 16px; line-height: 1.8; color: #555555; text-align: justify;'>
                Check out the latest schedule for your trip from <strong>{depart}</strong> to <strong>{destination}</strong>. Plan your journey with ease and book your tickets today!
                <span style='color: #d9534f; font-weight: bold;'>Original price: <span>RM {price}</span>).
            </p>
            ";


        // Email Body
        mail.Body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px; background-color: #ffffff;'>
                <div style='text-align: center; margin-bottom: 20px;'>
                    <img src='cid:logo' alt='JM_Bus Logo' style='width: 120px; height: auto;'>
                </div>
                <h1 style='color: #d9534f; text-align: center; font-size: 24px;'>Exciting Bus Schedules Just for You!</h1>
                <p style='font-size: 16px; line-height: 1.8; color: #555555; text-align: justify;'>
                    Hello <strong>{fullName}</strong>,
                </p>
                {discountSection}
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{UrlSchedule}' style='background-color: #5cb85c; color: white; text-decoration: none; padding: 12px 30px; border-radius: 5px; font-size: 16px;'>View Schedules</a>
                </div>
                <p style='font-size: 14px; line-height: 1.6; color: #777777; text-align: justify;'>
                    We look forward to serving you on your next journey. If you have any questions, please feel free to contact our support team.
                </p>
                <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 20px 0;'>
                <div style='font-size: 12px; color: #999999; text-align: center;'>
                    <p>
                        This email was sent to you as a subscribed member of JM_Bus. If you no longer wish to receive updates, you can unsubscribe at any time.
                    </p>
                    <p>
                        &copy; 2024 JM_Bus. All rights reserved.
                    </p>
                </div>
            </div>";

        //  Attach logo
        var logoPath = Path.Combine(en.WebRootPath, "photo/images", "logo_JMBus.png");
        mail.Attachments.Add(new Attachment(logoPath) { ContentId = "logo" });

        // Send the email
        hp.SendEmail(mail);
    }

    public string GetNextPrefixId(string tableName)
    {
        string currentMaxId = "";

        if (tableName == "Schedules")
        {
            currentMaxId = db.Schedules.Max(m => m.Id) ?? "OW0000"; // Default if table is empty
        }
        else if (tableName == "Bookings")
        {
            currentMaxId = db.Bookings.Max(m => m.Id) ?? "BK0000"; // Default if table is empty
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


    public IActionResult Index(HomeView vm, int page = 1)
    {
        // Define page size
        int pageSize = 10;

        // Retrieve the schedule data and related entities (RouteLocation, Bus, and CategoryBus of Bus)
        var schedulesQuery = db.Schedules
            .Where(s => s.Status == "Active")
            .Include(s => s.RouteLocation) // Ensure RouteLocation is included
            .Include(s => s.Bus)           // Ensure Bus is included
            .Include(s => s.Bus.CategoryBus) // Ensure CategoryBus is included inside Bus
            .Include(s => s.Bookings)
            .ThenInclude(b => b.BookingSeats)
            .AsQueryable(); // Ensure it's IQueryable for later LINQ operations

        // Apply Price Range filter if provided
        if (!string.IsNullOrEmpty(Request.Query["minPrice"]) && decimal.TryParse(Request.Query["minPrice"], out decimal minPrice))
        {
            schedulesQuery = schedulesQuery.Where(s => s.Price >= minPrice);
        }

        if (!string.IsNullOrEmpty(Request.Query["maxPrice"]) && decimal.TryParse(Request.Query["maxPrice"], out decimal maxPrice))
        {
            schedulesQuery = schedulesQuery.Where(s => s.Price <= maxPrice);
        }

        // Apply Depart filter if a valid value is selected
        var departFilter = Request.Query["Depart"].FirstOrDefault();
        if (!string.IsNullOrEmpty(departFilter) && departFilter != "Choose...")
        {
            schedulesQuery = schedulesQuery.Where(s => s.RouteLocation.Depart == departFilter);
        }

        // Apply Destination filter if a valid value is selected
        var destinationFilter = Request.Query["Destination"].FirstOrDefault();
        if (!string.IsNullOrEmpty(destinationFilter) && destinationFilter != "Choose...")
        {
            schedulesQuery = schedulesQuery.Where(s => s.RouteLocation.Destination == destinationFilter);
        }

        // Apply Travel Date filter (fromDate) if provided
        if (!string.IsNullOrEmpty(Request.Query["fromDate"]) && DateOnly.TryParse(Request.Query["fromDate"], out DateOnly fromDate))
        {
            schedulesQuery = schedulesQuery.Where(s => s.DepartDate >= fromDate);
        }

        // Apply Travel Date filter (toDate) if provided
        if (!string.IsNullOrEmpty(Request.Query["toDate"]) && DateOnly.TryParse(Request.Query["toDate"], out DateOnly toDate))
        {
            schedulesQuery = schedulesQuery.Where(s => s.DepartDate <= toDate);
        }



        //// Apply Return Date filter if provided
        //if (!string.IsNullOrEmpty(Request.Query["returnDate"]) && DateTime.TryParse(Request.Query["returnDate"], out DateTime returnDate))
        //{
        //    schedulesQuery = schedulesQuery.Where(s => s.ReturnDate <= returnDate);  // Assuming there's a ReturnDate property in the model
        //}

        // Check for sort options
        var sortOption = Request.Query["sortOption"].FirstOrDefault();

        if (!string.IsNullOrEmpty(sortOption))
        {
            switch (sortOption)
            {
                case "Cheapest":
                    schedulesQuery = schedulesQuery.OrderBy(s => s.Price);
                    break;

                case "HighestDiscount":
                    schedulesQuery = schedulesQuery.OrderByDescending(s => s.DiscountPrice);
                    break;

                case "Earliest":
                    schedulesQuery = schedulesQuery.OrderBy(s => s.DepartTime); // Assuming DepartTime is a TimeOnly or DateTime
                    break;

                case "Latest":
                    schedulesQuery = schedulesQuery.OrderByDescending(s => s.DepartTime);
                    break;

                default:
                    break; // No sorting if no valid option is selected
            }
        }

        var schedulesPaged = schedulesQuery.ToPagedList(page, pageSize);

        // Convert the schedules data into ScheduleDetailsVM
        var scheduleDetails = schedulesPaged.Select(s => new ScheduleDetailsVM
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
            BusCapacity = s.Bus.Capacity,
            BusPlate = s.Bus.BusPlate,
            CategoryBusName = s.Bus.CategoryBus != null ? s.Bus.CategoryBus.Name : "Unknown",
            PhotoURL = s.Bus.PhotoURL,

            // Calculate booked seats across all bookings for this schedule
            SeatsBooked = s.Bookings
                .SelectMany(b => b.BookingSeats) // Flatten the BookingSeats lists from all bookings
                .Count(bs => bs.Status == "Booked" || bs.Status == "Pending") // Count the relevant seats
        }).ToList();

        // Build query string for pagination links
        var queryParams = new Dictionary<string, string>(Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString()));

        // Store the data in ViewBag
        ViewBag.ScheduleDetails = scheduleDetails;
        ViewBag.CurrentPage = schedulesPaged.PageNumber;
        ViewBag.TotalPages = schedulesPaged.PageCount;

        ViewBag.QueryParams = queryParams;

        // Set up ViewBag for filters and sorting options
        ViewBag.MinPrice = Request.Query["minPrice"];
        ViewBag.MaxPrice = Request.Query["maxPrice"];
        ViewBag.Depart = departFilter;  // Store Depart filter in ViewBag
        ViewBag.Destination = destinationFilter;  // Store Destination filter in ViewBag
        ViewBag.TravelDate = Request.Query["travelDate"]; // Store Travel Date filter
        ViewBag.ReturnDate = Request.Query["toDate"]; // Store Return Date filter

        return View();
    }

    public IActionResult AddSchedule()
    {
        ViewBag.Buses = db.Buses.Where(b => b.Status == "Active");
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

        if (vm.DiscountPrice < 0)
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
            ModelState.AddModelError("", busAvailabilityMessage + "Ops");
        }

        if (ModelState.IsValid)
        {
            string SchedulesId = GetNextPrefixId("Schedules");

            if (vm.SubscribeEmail == "Send")
            {
                // Fetch subscribed members who are subscribed to the newsletter
                var subscribePersons = db.Subscriptionses
                                         .Include(s => s.Member)
                                         .Where(s => s.Member.IsSubscribedToNewsletter == true)
                                         .ToList();

                var RouteLocations = db.RouteLocations.FirstOrDefault(rl => rl.Id == vm.RouteId);
                // Loop through each subscribed person and send the subscription email
                foreach (var subscribedPerson in subscribePersons)
                {
                    SendScheduleSubscribeMail(subscribedPerson.Member.Email, subscribedPerson.Member.FirstName, subscribedPerson.Member.LastName, vm.Price, vm.DiscountPrice, RouteLocations.Depart, RouteLocations.Destination, SchedulesId);
                }
            }

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

    public IActionResult ShowScheduleList(DateOnly? name, string? status, string? sort, string? dir, int page = 1)
    {

        // (1) Searching ------------------------
        // If 'name' is null, set it to an empty string or a default value.
        ViewBag.Name = name.HasValue ? name.Value.ToString() : ""; // Use empty string if null

        // Filter schedules based on DepartDate if name has a value
        // Include the RouteLocation when fetching the schedules
        var searched = db.Schedules
            .Where(s => (!name.HasValue || s.DepartDate == name.Value) &&
                        (string.IsNullOrEmpty(status) || status == "All" || s.Status == status))
            .Include(s => s.RouteLocation); // Ensure RouteLocation is included



        // (2) Sorting --------------------------
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        ViewBag.Status = status ?? "All";

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


    public IActionResult EditSchedule(string id)
    {
        // Retrieve the schedule record based on the ID
        ViewBag.Buses = db.Buses.Where(b => b.Status == "Active");
        ViewBag.Routes = db.RouteLocations;
        var m = db.Schedules.Find(id);
        if (m == null)
            return RedirectToAction("ShowScheduleList", "Schedule"); // Redirect if schedule not found

        // Prepare the view model
        var vm = new EditScheduleVM
        {
            ScheduleId = m.Id, // Include ScheduleId for identifying the record during the update
            DepartDate = m.DepartDate,
            DepartTime = m.DepartTime,
            Status = m.Status,
            Price = m.Price,
            DiscountPrice = m.DiscountPrice,
            Remark = m.Remark,
            BusId = m.BusId,
            RouteId = m.RouteLocationId,
            // Add properties to store buses and routes
        };

        return View(vm);
    }


    

    [HttpPost]
    public IActionResult EditSchedule(EditScheduleVM vm, string id)
    {
        if (vm.Price <= 0)
        {
            ModelState.AddModelError("Price", "Price Should Not Be 0 Or Less Than.");
        }

        if (vm.DiscountPrice < 0)
        {
            ModelState.AddModelError("Price", "Discount Price Should Not Be Negative.");
        }
        if (vm.DepartDate < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError("DepartDate", "Depart Date Should Not Be Past");
        }
        // Check if the bus is available at the selected time

        var currentSchedule = db.Schedules.Find(id);
        if (currentSchedule == null)
        {
            TempData["Error"] = "Schedule not found.";
            return RedirectToAction("ShowScheduleList", "Schedule");
        }
        // Check if the DepartDate or DepartTime has changed
        bool isDateOrTimeChanged = currentSchedule.DepartDate != vm.DepartDate || currentSchedule.DepartTime != vm.DepartTime;

        if (isDateOrTimeChanged)
        {
            // Check if the bus is available at the selected time
            string busAvailabilityMessage;
            bool isBusAvailable = CheckBusAvailableSlot_After(vm.BusId, vm.DepartDate, vm.DepartTime, out busAvailabilityMessage);

            if (!isBusAvailable)
            {
                // Add the error message returned by the CheckBusAvailableSlot method
                ModelState.AddModelError("", busAvailabilityMessage + "Ops");
            }
        }

        if (ModelState.IsValid)
        {
            //if (vm.SubscribeEmail == "Send")
            //{
            //    // Fetch subscribed members who are subscribed to the newsletter
            //    var subscribePersons = db.Subscriptionses
            //                             .Include(s => s.Member)
            //                             .Where(s => s.Member.IsSubscribedToNewsletter == true)
            //                             .ToList();

            //    var RouteLocations = db.RouteLocations.FirstOrDefault(rl => rl.Id == vm.RouteId);
            //    // Loop through each subscribed person and send the subscription email
            //    foreach (var subscribedPerson in subscribePersons)
            //    {
            //        SendScheduleSubscribeMail(subscribedPerson.Member.Email, subscribedPerson.Member.FirstName, subscribedPerson.Member.LastName, vm.Price, vm.DiscountPrice, RouteLocations.Depart, RouteLocations.Destination,vm.ScheduleId);
            //    }
            //}

            // Modify the existing schedule record
            var m = db.Schedules.Find(id);
            if (m == null)
            {
                TempData["Error"] = "m not found.";
                return RedirectToAction("ShowmList", "m");
            }

            // Update the m details
            m.DepartDate = vm.DepartDate;
            m.DepartTime = vm.DepartTime;
            m.Status = vm.Status;
            m.Price = vm.Price;
            m.DiscountPrice = vm.DiscountPrice;
            m.Remark = vm.Remark;
            m.BusId = vm.BusId;
            m.RouteLocationId = vm.RouteId;

            db.SaveChanges();
            TempData["Info"] = "Schedule has been updated successfully.";
            return RedirectToAction("ShowScheduleList", "Schedule");

        }
        //if fail valdiate
        ViewBag.Buses = db.Buses.Where(b => b.Status == "Active");
        ViewBag.Routes = db.RouteLocations;
        return View(vm);
    }

    public IActionResult ScheduleDetails(string id)
    {
        // Retrieve the schedule record based on the ID
        ViewBag.Buses = db.Buses.Where(b => b.Status == "Active");
        ViewBag.Routes = db.RouteLocations;
        var m = db.Schedules.Find(id);
        if (m == null)
            return RedirectToAction("ShowScheduleList", "Schedule"); // Redirect if schedule not found

        // Prepare the view model
        var vm = new ShowScheduleDetailsVM
        {
            ScheduleId = m.Id, // Include ScheduleId for identifying the record during the update
            DepartDate = m.DepartDate,
            DepartTime = m.DepartTime,
            Status = m.Status,
            Price = m.Price,
            DiscountPrice = m.DiscountPrice,
            Remark = m.Remark,
            BusId = m.BusId,
            RouteId = m.RouteLocationId,
            // Add properties to store buses and routes
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult UpdateScheduleStatus(string id, [FromBody] UpdateStatusVM model)
    {
        if (string.IsNullOrEmpty(id) || model == null)
        {
            return BadRequest("Invalid request.");
        }

        // Fetch the schedule from the database
        var schedule = db.Schedules.Find(id);
        if (schedule == null)
        {
            return NotFound("Schedule not found.");
        }

        // Update the status of the schedule
        schedule.Status = model.Status; // "Active" or "Inactive"
        db.SaveChanges();

        return Ok(new { message = "Schedule status updated successfully." });
    }


    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult DeleteManySchedules(string[] ids)
    {
        if (ids != null && ids.Length > 0)
        {
            // Fetch the schedules based on the provided IDs
            var schedulesToUpdate = db.Schedules.Where(s => ids.Contains(s.Id)).ToList();

            // Check if any of the schedules are associated with a route
            foreach (var schedule in schedulesToUpdate)
            {
                if (schedule.RouteLocationId != null) // Schedule is associated with a route
                {
                    // If the schedule is associated with a route, set its status to "Inactive"
                    schedule.Status = "Inactive";
                }
                else
                {
                    // If the schedule is not associated with any route, add an error
                    ModelState.AddModelError("", $"Schedule with ID {schedule.Id} cannot be deactivated because it is not associated with a route.");
                    return Json(new { success = false, message = "One or more schedules cannot be deactivated because they are not associated with a route." });
                }
            }

            // Save the changes to the database
            db.SaveChanges();

            TempData["Info"] = $"{schedulesToUpdate.Count} schedule(s) set to inactive.";
        }

        return RedirectToAction("ShowScheduleList"); // Redirect to the schedule list page
    }

    [Authorize(Roles = "Member")]
    public IActionResult SelectSeats(string id)
    {

        //additonal notification to let user know
        var pendingPaymentSchedulesRecord = db.Bookings.FirstOrDefault(b => b.ScheduleId == id && b.MemberId == User.Identity.Name && b.Status == "Pending");

        if (pendingPaymentSchedulesRecord != null)
        {
            ViewBag.PendingBookingId = pendingPaymentSchedulesRecord.Id;
        }

        // Retrieve the schedule data by its ID (OW0001)
        var schedule = db.Schedules
        .Include(s => s.Bus)  // Include related Bus
        .ThenInclude(b => b.CategoryBus)  // Include related CategoryBus for the Bus
        .Include(s => s.RouteLocation)  // Include related RouteLocation
        .FirstOrDefault(s => s.Id == id && s.Status == "Active");  // Fetch by ScheduleId

        if (schedule == null)
        {
            TempData["Info"] = "Schedule not found.";
            return RedirectToAction("Index");
        }

        // Check if Bus and RouteLocation are null
        if (schedule.Bus == null || schedule.RouteLocation == null)
        {
            TempData["Info"] = "Bus or RouteLocation data is missing.";
            return RedirectToAction("Index");
        }

        // Retrieve booking seats that are booked based on the selected schedule
        var bookingSeats = db.Bookings
            .Where(b => b.ScheduleId == id)
            .SelectMany(b => b.BookingSeats)
            .Where(bs => bs.Status == "Booked" || bs.Status == "Pending") // Filter 
            .ToList();

        // Create a dictionary for the booked seats
        var dictionaryBookedSeats = bookingSeats.ToDictionary(seat => seat.SeatNo, seat => "Booked");

        // Check if schedule.Bus and schedule.Bus.Seats are not null before selecting seats
        var seatList = db.Schedules
       .Where(s => s.Id == id)  // Ensure you're filtering by the specific 'id'
       .SelectMany(s => s.Bus.Seats.Select(seat => seat.SeatNo))  // Flatten the seats collection
       .ToList();


        if (seatList.Count <= 0 || seatList == null)
        {
            TempData["Info"] = "Seat List Is empty cant find";
        }

        // Convert the schedule data into ScheduleSelectSeatsVM
        var viewModel = new ScheduleSelectSeatsVM
        {
            ScheduleId = schedule.Id,
            BusName = schedule.Bus.Name ?? "Unknown Bus",  // Default value if Bus.Name is null
            CategoryName = schedule.Bus.CategoryBus?.Name ?? "Unknown Category",  // Null check for CategoryBus
            Price = schedule.Price,
            Discount = schedule.DiscountPrice,
            DepartTime = schedule.DepartTime,
            Depart = schedule.RouteLocation.Depart ?? "Unknown Depart Location",  // Default if Depart is null
            Destination = schedule.RouteLocation.Destination ?? "Unknown Destination",  // Default if Destination is null
            Hour = schedule.RouteLocation.Hour,
            Minute = schedule.RouteLocation.Min,
            SeatId = seatList,
            BookedSeats = dictionaryBookedSeats  // Pass the booked seats here
        };

        // Return the view with the populated viewModel
        return View(viewModel);
    }

    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult SelectSeats(string id, string[] seat)
    {
        //check scheudle Id
        if (id == null)
        {
            TempData["Info"] = $"No Id Founded ! ";
            return RedirectToAction("Index");
        }
        //verify seat
        if (seat.Length <= 0)
        {
            TempData["Info"] = $"Schedule : {id},Please Select A Seat Before Proceed ! ";
            ModelState.AddModelError("", "Seat Error");//use for block purpose only none use for here
        }

        var pendingPaymentSchedulesRecord = db.Bookings.FirstOrDefault(b => b.ScheduleId == id && b.MemberId == User.Identity.Name && b.Status == "Pending");
        if (ModelState.IsValid)
        {

            if (pendingPaymentSchedulesRecord != null)
            {
                //TempData["Info"] = "You have a record of this booking in system,Please Complete Booking !";
                //// Redirect to Payment Index with bookingId
                //return RedirectToAction("Index", "Payment", new { bookingId = pendingPaymentSchedulesRecord.Id });


                //if checked user has a previous code and didnt check out still pending then cancelled the booking help him add another new one
                pendingPaymentSchedulesRecord.Status = "Cancelled";
                db.BookingSeats
                .Where(bs => bs.BookingId == pendingPaymentSchedulesRecord.Id)
                .ExecuteUpdate(bs => bs.SetProperty(b => b.Status, "Cancelled"));
                db.SaveChanges();
            }

            var schedules = db.Schedules.Find(id);
            int schedulePrice = 0;

            if (schedules != null)
            {
                if (schedules.DiscountPrice != 0)
                {
                    schedulePrice = schedules.DiscountPrice;
                }
                else
                {
                    schedulePrice = schedules.Price;
                }
                // Generate a new booking ID
                string bookingId = GetNextPrefixId("Bookings");

                // Calculate the subtotal and total
                decimal subtotal = schedulePrice * seat.Length;

                decimal total = (subtotal * 10 / 100) + subtotal;

                var member = db.Members.FirstOrDefault(m => m.Id == User.Identity.Name);
                var rankMember = db.Ranks.FirstOrDefault(r => r.Id == member.RankId);

                if (rankMember.Discounts > 0)
                {
                    var discountAmount = (total * rankMember.Discounts) / 100;
                    total = total - discountAmount;

                    Console.WriteLine($"Discount Applied: {discountAmount}");
                    Console.WriteLine($"Total After Discount: {total}");
                }




                // Create a new Booking entity
                var newBooking = new Booking
                {
                    Id = bookingId,
                    Price = schedulePrice,
                    Qty = seat.Length,
                    Status = "Pending",
                    VoucherId = null,
                    BookingDateTime = DateTime.Now,
                    Subtotal = subtotal,
                    Total = total,
                    ScheduleId = id,
                    MemberId = User.Identity.Name,
                    BookingSeats = new List<BookingSeat>() // Initialize the child collection
                };

                // Populate the BookingSeats collection
                foreach (var seatNo in seat)
                {
                    newBooking.BookingSeats.Add(new BookingSeat
                    {
                        SeatNo = seatNo,
                        Status = "Pending"
                    });
                }

                // Add the new booking to the database
                db.Bookings.Add(newBooking);

                // Save changes to the database
                db.SaveChanges();
                TempData["Info"] = $"Shchedule : {id} , Total Seat : {seat.Length} has been inserted to DB,Status:Pending";

                // Redirect to Payment Index with bookingId
                return RedirectToAction("Index", "Payment", new { bookingId = bookingId });
            }
        }

        //if user exceed error return the  original data back to let them see

        if (pendingPaymentSchedulesRecord != null)
        {
            ViewBag.PendingBookingId = pendingPaymentSchedulesRecord.Id;
        }

        // Retrieve the schedule data by its ID (OW0001)
        var schedule = db.Schedules
                .Include(s => s.Bus)  // Include related Bus
                .ThenInclude(b => b.CategoryBus)  // Include related CategoryBus for the Bus
                .Include(s => s.RouteLocation)  // Include related RouteLocation
                .FirstOrDefault(s => s.Id == id);  // Fetch by ScheduleId

        if (schedule == null)
        {
            TempData["Info"] = "Schedule not found.";
            return RedirectToAction("Index");
        }

        // Check if Bus and RouteLocation are null
        if (schedule.Bus == null || schedule.RouteLocation == null)
        {
            TempData["Info"] = "Bus or RouteLocation data is missing.";
            return RedirectToAction("Index");
        }

        // Retrieve booking seats based on the selected schedule
        var bookingSeats = db.Bookings
            .Where(b => b.ScheduleId == id)
            .SelectMany(b => b.BookingSeats)
             .Where(bs => bs.Status == "Booked" || bs.Status == "Pending") // Filter by Status
            .ToList();

        // Create a dictionary for the booked seats
        var disctornaryBookedSeats = bookingSeats.ToDictionary(seat => seat.SeatNo, seat => seat.Status);

        // Check if schedule.Bus and schedule.Bus.Seats are not null before selecting seats
        var seatList = db.Schedules
       .Where(s => s.Id == id)  // Ensure you're filtering by the specific 'id'
       .SelectMany(s => s.Bus.Seats.Select(seat => seat.SeatNo))  // Flatten the seats collection
       .ToList();


        if (seatList.Count <= 0 || seatList == null)
        {
            TempData["Info"] = "Seat List Is empty cant find";
        }
        // Convert the schedule data into ScheduleSelectSeatsVM
        var viewModel = new ScheduleSelectSeatsVM
        {
            ScheduleId = schedule.Id,
            BusName = schedule.Bus.Name ?? "Unknown Bus",  // Default value if Bus.Name is null
            CategoryName = schedule.Bus.CategoryBus?.Name ?? "Unknown Category",  // Null check for CategoryBus
            Price = schedule.Price,
            Discount = schedule.DiscountPrice,
            DepartTime = schedule.DepartTime,
            Depart = schedule.RouteLocation.Depart ?? "Unknown Depart Location",  // Default if Depart is null
            Destination = schedule.RouteLocation.Destination ?? "Unknown Destination",  // Default if Destination is null
            Hour = schedule.RouteLocation.Hour,
            Minute = schedule.RouteLocation.Min,
            SeatId = seatList,
            BookedSeats = disctornaryBookedSeats  // Pass the booked seats here
        };

        // Return the view with the populated viewModel
        return View(viewModel);
    }

    [Authorize(Roles = "Member")]
    public IActionResult MyBookingList(string? search, string? id, string? sort, DateOnly? departDate, string? dir, int page = 1)
    {
        // (1) Searching ------------------------
        ViewBag.Name = id = id?.Trim() ?? "";
        ViewBag.DepartDate = departDate?.ToString("yyyy-MM-dd"); // Format for consistency in the view

        // Filter bookings by ID and join with Schedule and RouteLocations
        var searched = db.Bookings
            .Where(b => b.Id.Contains(id) && b.MemberId == User.Identity.Name && b.Status != "Cancelled")
            .Join(db.Schedules,
                booking => booking.ScheduleId,
                schedule => schedule.Id,
                (booking, schedule) => new { booking, schedule })
            .Join(db.RouteLocations,
                combined => combined.schedule.RouteLocationId,
                route => route.Id,
                (combined, route) => new
                {
                    combined.booking,
                    combined.schedule,
                    route
                });

        // Apply search filter (ID, Depart, or Arrival)
        if (!string.IsNullOrEmpty(search))
        {
            searched = searched.Where(x =>
                x.booking.Id.Contains(search) ||
                x.route.Depart.Contains(search) ||
                x.route.Destination.Contains(search));
        }

        // Apply depart date filter if provided
        if (departDate.HasValue)
        {
            searched = searched.Where(x => x.schedule.DepartDate == departDate.Value);
        }

        // (2) Sorting --------------------------
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        Func<dynamic, object> fn = sort switch
        {
            "Id" => item => item.booking.Id,
            "Status" => item => item.booking.Status,
            "Qty" => item => item.booking.Qty,
            "BookingDateTime" => item => item.booking.BookingDateTime,
            "Total" => item => item.booking.Total,
            "RouteLocation" => item => item.route.Depart,
            "DepartDate" => item => item.schedule.DepartDate,
            "DepartTime" => item => item.schedule.DepartTime,
            _ => item => item.booking.Id,
        };

        var sorted = dir == "desc" ? searched.OrderByDescending(fn) : searched.OrderBy(fn);

        // (3) Paging ---------------------------
        if (page < 1)
        {
            return RedirectToAction(null, new { id, sort, dir, page = 1 });
        }

        var pagedResult = sorted.ToPagedList(page, 10);

        if (page > pagedResult.PageCount && pagedResult.PageCount > 0)
        {
            return RedirectToAction(null, new { id, sort, dir, page = pagedResult.PageCount });
        }

        // Return partial view for AJAX requests
        if (Request.IsAjax())
        {
            return PartialView("_MyBookingScheduleList", pagedResult);
        }

        // Return full view
        return View(pagedResult);
    }


    public IActionResult EditMyTicketSeats(string id)
    {
        var bookingSchedule = db.Bookings
            .Include(b => b.Schedule)
                .ThenInclude(s => s.RouteLocation)
            .Include(b => b.Schedule)
                .ThenInclude(s => s.Bus)
            .FirstOrDefault(b => b.Id == id && b.Status == "Booked" && b.MemberId == User.Identity.Name);

        if (bookingSchedule == null)
        {
            TempData["Info"] = "Bookings Not Found";
            return RedirectToAction("Index");
        }

        var bookingSeats = db.BookingSeats
            .Where(bs => bs.BookingId == id && bs.Status != "Pending")
            .ToList();

        ViewBag.BookingSchedule = bookingSchedule; // Correctly assigns booking schedule
        ViewBag.BookingSeats = bookingSeats; // Correctly assigns list of booking seats

        return View();
    }


    [HttpPost]
    public IActionResult EditMyTicketSeats(string SeatNo, string BookingId)
    {
        // Fetch the booking details
        var bookingSchedule = db.Bookings
            .Include(b => b.Schedule)
            .FirstOrDefault(b => b.Id == BookingId && b.Status == "Booked" && b.MemberId == User.Identity.Name);

        if (bookingSchedule == null)
        {
            TempData["Info"] = "Unable to find the booking.";
            return RedirectToAction("EditMyTicketSeats");
        }

        // Find the booked seat
        var bookedSeat = db.BookingSeats
            .FirstOrDefault(bs => bs.BookingId == BookingId && bs.Status == "Booked" && bs.SeatNo == SeatNo);

        if (bookedSeat == null)
        {
            TempData["Info"] = "Seat not found or already cancelled.";
            return RedirectToAction("EditMyTicketSeats", new { id = BookingId });
        }

        // Fetch member details
        var member = db.Members.Find(User.Identity.Name);
        if (member == null)
        {
            TempData["Info"] = "Unable to find member details.";
            return RedirectToAction("EditMyTicketSeats");
        }

        var memberRank = db.Ranks.FirstOrDefault(r => r.Id == member.RankId);
        if (memberRank == null)
        {
            TempData["Info"] = "Unable to find member rank details.";
            return RedirectToAction("EditMyTicketSeats");
        }

        // Calculate ticket price with discount
        decimal ticketPrice = bookingSchedule.Price;
        if (memberRank.Discounts > 0)
        {
            decimal afterTaxTicketPrice = ticketPrice + (bookingSchedule.Price * 0.1m); // Use 0.1m for 10% tax
            ticketPrice = afterTaxTicketPrice - ((afterTaxTicketPrice * memberRank.Discounts) / 100); // Discounts applied
            TempData["Info"] = $"+Yes member ticket Price : {ticketPrice}";
        }
        else
        {
            ticketPrice = ticketPrice + (bookingSchedule.Price * 0.1m); // Use 0.1m for 10% tax
            TempData["Info"] = $"No member ticket Price : {ticketPrice}";
        }


        // Mark the seat as cancelled
        bookedSeat.Status = "Cancelled";
        bookingSchedule.Qty -= 1;
        db.SaveChanges(); // Save changes after marking the seat

        // Check if all seats in the booking are cancelled
        var remainingSeats = db.BookingSeats
            .Where(bs => bs.BookingId == BookingId && bs.Status != "Cancelled")
            .ToList();

        // Recalculate the total price
        decimal updatedTotal = bookingSchedule.Total - ticketPrice;


        if (!remainingSeats.Any())
        {
            // If all seats are cancelled, update the booking status
            bookingSchedule.Status = "Cancelled";
            // Adjust member points and spending
            member.Points -= (int)Math.Round(ticketPrice, MidpointRounding.AwayFromZero);
            member.MinSpend -= ticketPrice;
            bookingSchedule.Total = updatedTotal;
            db.SaveChanges(); // Save changes after updating the status
            TempData["Info"] = "All seats have been cancelled. Your booking is now cancelled.";
            return RedirectToAction("MyBookingList");
        }


        if (updatedTotal < 0)
        {
            member.Points -= (int)Math.Round(bookingSchedule.Total, MidpointRounding.AwayFromZero);
            member.MinSpend -= bookingSchedule.Total;

            // Handle negative total
            bookingSchedule.Total = 0; // Reset total to zero

            if (bookingSchedule.VoucherId != null)
            {
                // Return the voucher to the user
                var voucher = db.MemberVouchers.FirstOrDefault(v => v.VoucherId == bookingSchedule.VoucherId && v.MemberId == User.Identity.Name);
                if (voucher != null)
                {
                    voucher.Amount += 1; // Return the voucher
                }

                bookingSchedule.VoucherId = null; // Remove the voucher from the booking
            }


            decimal newSubtotal = bookingSchedule.Subtotal - bookingSchedule.Price;
            decimal newTotal = newSubtotal + (newSubtotal * 0.10M); // Add 10% tax


            if (memberRank.Discounts > 0)
            {
                newTotal = newTotal - ((newTotal * memberRank.Discounts) / 100);
            }

            bookingSchedule.Subtotal = newSubtotal;
            bookingSchedule.Total = newTotal; // Update the total

            if (bookingSchedule.Qty == 0)
            {
                bookingSchedule.Status = "Cancelled";
                // Adjust member points and spending
                member.Points -= (int)Math.Round(bookingSchedule.Total, MidpointRounding.AwayFromZero);
                member.MinSpend -= bookingSchedule.Total;
                db.SaveChanges();
                return RedirectToAction("Index");
            }


            // Update the booking status to "Pending"
            bookingSchedule.Status = "Pending";


            var otherSeats = db.BookingSeats
                .Where(bs => bs.SeatNo != SeatNo && bs.Status == "Booked" && bs.BookingId == BookingId);

            foreach (var seat in otherSeats)
            {
                seat.Status = "Pending";
            }

            db.SaveChanges();
            TempData["Info"] = "Complete The Payment, since the total amount is less than the voucher value.Previous Amount Has Been Refund";
            return RedirectToAction("Index", "Payment", new { bookingId = BookingId });

        }
        else
        {
            // Update the total price after deduction
            bookingSchedule.Total = updatedTotal;

            // Adjust member points and spending
            member.Points -= (int)Math.Round(ticketPrice, MidpointRounding.AwayFromZero);
            member.MinSpend -= ticketPrice;
        }

        // Save changes to the database
        db.SaveChanges();

        TempData["Info"] = $"{SeatNo} has been cancelled.";
        return RedirectToAction("EditMyTicketSeats", new { id = BookingId });
    }



    //Get Request - in here we just want to show result then no need opne another HttpPost
    public IActionResult TicketDetails(string id)
    {
        var booking = db.Bookings
            .Include(b => b.Schedule)
                .ThenInclude(s => s.Bus)
                    .ThenInclude(b => b.CategoryBus)
            .Include(b => b.Schedule)
                .ThenInclude(s => s.RouteLocation)
            .Include(b => b.BookingSeats)
            .Include(b => b.Voucher)
            .FirstOrDefault(b => b.Id == id);

        if (booking == null)
        {
            return NotFound();
        }

        // Create a DateTime combining DepartDate and DepartTime
        var departDateTime = new DateTime(
            booking.Schedule.DepartDate.Year,
            booking.Schedule.DepartDate.Month,
            booking.Schedule.DepartDate.Day,
            booking.Schedule.DepartTime.Hour,
            booking.Schedule.DepartTime.Minute,
            0
        );

        // Calculate arrival DateTime by adding hours and minutes
        var arrivalDateTime = departDateTime
            .AddHours(booking.Schedule.RouteLocation.Hour)
            .AddMinutes(booking.Schedule.RouteLocation.Min);

        // Maps to TicketDetailsVM view model
        var ticketDetails = new TicketDetailsVM
        {
            BookingId = booking.Id,
            BusName = booking.Schedule.Bus.Name,
            BusPlate = booking.Schedule.Bus.BusPlate,
            CategoryName = booking.Schedule.Bus.CategoryBus.Name,
            DepartLocation = booking.Schedule.RouteLocation.Depart,
            Destination = booking.Schedule.RouteLocation.Destination,
            BookingDateTime = booking.BookingDateTime,
            DepartDate = booking.Schedule.DepartDate,
            DepartTime = booking.Schedule.DepartTime,
            ArrivalDate = DateOnly.FromDateTime(arrivalDateTime),
            ArrivalTime = TimeOnly.FromDateTime(arrivalDateTime),
            SeatNumbers = booking.BookingSeats.Select(bs => bs.SeatNo).ToList(),
            Price = booking.Price,
            Quantity = booking.Qty,
            Subtotal = booking.Subtotal,
            Total = booking.Total,
            Status = booking.Status == "Booked" ? "Paid" : booking.Status,
            VoucherApplied = booking.Voucher?.Name
        };

        return View(ticketDetails);
    }


    [Authorize(Roles = "Member")]
    public IActionResult MyBookingHistory(string? id, string? sort, string? dir, int page = 1)
    {
        ViewBag.Name = id = id?.Trim() ?? "";

        var searched = db.Bookings
            .Where(b => b.Id.Contains(id) && b.MemberId == User.Identity.Name && b.Status == "Cancelled")
            .Join(db.Schedules,
                booking => booking.ScheduleId,
                schedule => schedule.Id,
                (booking, schedule) => new { booking, schedule })
            .Join(db.RouteLocations,
                combined => combined.schedule.RouteLocationId,
                route => route.Id,
                (combined, route) => new {
                    combined.booking,
                    combined.schedule,
                    route
                });

        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        Func<dynamic, object> fn = sort switch
        {
            "Id" => item => item.booking.Id,
            "CancelledDateTime" => item => item.booking.BookingDateTime,
            "Qty" => item => item.booking.Qty,
            "Total" => item => item.booking.Total,
            "DepartLocation" => item => item.route.Depart,
            "DepartDate" => item => item.schedule.DepartDate,
            "DepartTime" => item => item.schedule.DepartTime,
            _ => item => item.booking.BookingDateTime
        };

        var sorted = dir == "desc" ? searched.OrderByDescending(fn) : searched.OrderBy(fn);

        if (page < 1)
        {
            return RedirectToAction(null, new { id, sort, dir, page = 1 });
        }

        var pagedResult = sorted.ToPagedList(page, 10);

        if (page > pagedResult.PageCount && pagedResult.PageCount > 0)
        {
            return RedirectToAction(null, new { id, sort, dir, page = pagedResult.PageCount });
        }

        if (Request.IsAjax())
        {
            return PartialView("_HistoryBookingList", pagedResult);
        }

        return View(pagedResult);
    }
}

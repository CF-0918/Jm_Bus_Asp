﻿using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using X.PagedList.Extensions;
using Demo.Models;

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

    private void SendScheduleSubscribeMail(string email, string firstName, string lastName, int price, int discountPrice, string depart,string destination)
    {
        var fullName = $"{firstName} {lastName}";
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(email, fullName));

        mail.Subject = "Wow,Latest Schedule Bus Here Can Buy !";

        mail.IsBodyHtml = true;


        var UrlSchedule = Url.Action("Index", "Schedule","", "https");

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
                    SendScheduleSubscribeMail(subscribedPerson.Member.Email, subscribedPerson.Member.FirstName, subscribedPerson.Member.LastName, vm.Price, vm.DiscountPrice, RouteLocations.Depart, RouteLocations.Destination);
                }
            }

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

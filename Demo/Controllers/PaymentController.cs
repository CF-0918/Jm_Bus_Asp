using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Memory;
using System.Reflection.Metadata;
using System.Security.Principal;

namespace Demo.Controllers;

public class PaymentController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;

    public PaymentController(DB db, IWebHostEnvironment en, Helper hp)
    {

        this.db = db;
        this.en = en;
        this.hp = hp;
    }

    //Get
    [Authorize(Roles = "Member")]
    public IActionResult Index(string bookingId)
    {
        // Now is fetch Booking Type
        // Fetch booking and related booking seats
        var bookingDetails = db.Bookings
             .Where(b => b.Id == bookingId && b.Status == "Pending"&&b.MemberId==User.Identity.Name)
            .Select(b => new
            {
                BookingId = b.Id,
                ScheduleId = b.ScheduleId,
                Price= b.Price,
                Qty= b.Qty,
                Subtotal = b.Subtotal,
                Total = b.Total,
                MemberId = b.MemberId,
                Seats = b.BookingSeats
                    .Where(bs => bs.Status == "Pending") 
                    .Select(bs => new
                    {
                        SeatNo = bs.SeatNo,
                        Status = bs.Status
                    }).ToList()
            })
            .FirstOrDefault();

        if (bookingDetails == null)
        {
            TempData["Info"] = "The Booking is not found here!";
            return RedirectToAction("Index", "Schedule");
        }

        var routeSchedules = db.Schedules
      .Include(s => s.RouteLocation) // Include related RouteLocation entity
      .FirstOrDefault(s => s.Id == bookingDetails.ScheduleId);

        if (routeSchedules == null)
        {
            TempData["Error"] = "Route schedule not found!";
            return RedirectToAction("Index", "Schedule");
        }

        var ranks = db.Members
                   .Include(m => m.Rank) // Include Rank relationship
                   .FirstOrDefault(m => m.Id == User.Identity.Name);

        ViewBag.UserRanks = ranks?.Rank; // Pass only the Rank information
        ViewBag.RouteSchedules = routeSchedules;

        ViewBag.BookingDetails = bookingDetails;
        //Below is used for voucher Fetching

        var currentDate = DateOnly.FromDateTime(DateTime.Now);

        // Get the vouchers list
        var vouchersList = db.MemberVouchers
            .Where(mv => mv.MemberId == User.Identity!.Name &&mv.Amount>0)
            .Include(mv => mv.Voucher)
           .Where(mv => mv.Voucher.Status == "Active" && mv.Voucher.CashDiscount <=bookingDetails.Total) // Compare voucher discount with booking total
            .ToList();

        // Map to ViewModel
        var voucherUserDetailsList = vouchersList.Select(mv => new VoucherUserDetailsVM
        {
            VoucherId = mv.VoucherId,
            VoucherName = mv.Voucher.Name,
            CashDiscount = mv.Voucher.CashDiscount,
            Qty = mv.Amount
        }).ToList();

        // Pass to View
        ViewBag.VouchersList = voucherUserDetailsList;  // Using ViewBag to pass data
        return View();
    }

   [Authorize(Roles ="Member")]
    [HttpPost]
    public IActionResult Index(PaymentVM vm)
    {
        if (vm.TermsCheck!= "checked")
        {
            ModelState.AddModelError("TermsCheck", "Please Check The T&C before Check Out !");
        }
        // Proceed if the model state is valid
        if (ModelState.IsValid)
        {
            // Fetch booking details from the database
            var bookingDetailsDB = db.Bookings
                .Include(b => b.BookingSeats)
                .FirstOrDefault(b => b.Id == vm.BookingId); // Fetch single booking

            if (bookingDetailsDB != null)
            {
                // Update booking status
                bookingDetailsDB.Status = "Booked";
                decimal total = bookingDetailsDB.Total;

                // Find the member and update the points and minSpend property
                var member = db.Members.Find(User.Identity.Name);


                if (vm.VoucherId != null && vm.VoucherId != "0") // Ensure a valid selection
                {
                    bookingDetailsDB.VoucherId = vm.VoucherId;

                    // Fetch the voucher details from the database
                    var vouchersDB = db.Vouchers.Find(vm.VoucherId);
                    if (vouchersDB != null)
                    {
                        // Calculate the discounted total
                        decimal totalSpend =total - vouchersDB.CashDiscount;

                        TempData["Info"] = $"Total Spend = {totalSpend}, cashDiscount : {vouchersDB.CashDiscount} ";
                        // Round the total spend to the nearest integer for points calculation
                        int roundedTotalSpendPoints = (int)Math.Round(totalSpend, MidpointRounding.AwayFromZero);

                        // Update member's minimum spend and points
                        member.MinSpend += totalSpend;
                        member.Points += roundedTotalSpendPoints;
                        bookingDetailsDB.Total = totalSpend;
                       //TempData["Info"] = $"The Points after {roundedTotalSpendPoints} , {totalSpend}  has been completed.";

                        // Update the voucher quantity in MemberVouchers
                       var memberVoucher = db.MemberVouchers.FirstOrDefault(mv => mv.VoucherId == vm.VoucherId);
                        if (memberVoucher != null)
                        {
                            memberVoucher.Amount -= 1;
                        }
                    }
                }
                else
                {
                    decimal totalSpend = bookingDetailsDB.Total;

                    // Round the total spend according to your rules
                    int roundedTotalSpendPoints = (int)Math.Round(totalSpend, MidpointRounding.AwayFromZero);


                    member.MinSpend += totalSpend;
                    member.Points += roundedTotalSpendPoints;
                }


                    // Perform bulk update of BookingSeats status
                    db.BookingSeats
                    .Where(bs => bs.BookingId == vm.BookingId &&bs.Status=="Pending") 
                    .ExecuteUpdate(bs => bs.SetProperty(b => b.Status, "Booked")); 

                db.SaveChanges();

                TempData["Info"] = $"The booking {vm.BookingId} has been completed.";
                return RedirectToAction("MyBookingList", "Schedule");
            }
            else
            {
                // If booking not found, add an error to the ModelState
                TempData["Info"] = "No Booking Founded.";
                return RedirectToAction("Index", "Schedule");
            }
        }

        var bookingDetails = db.Bookings
      .Where(b => b.Id == vm.BookingId &&b.Status=="Pending" && b.MemberId == User.Identity.Name)
      .Select(b => new
      {
          BookingId = b.Id,
          ScheduleId = b.ScheduleId,
          Price = b.Price,
          Qty = b.Qty,
          Subtotal = b.Subtotal,
          Total = b.Total,
          MemberId = b.MemberId,
          Seats = b.BookingSeats
              .Where(bs => bs.Status == "Pending")
              .Select(bs => new
              {
                  SeatNo = bs.SeatNo,
                  Status = bs.Status
              }).ToList()
      })
      .FirstOrDefault();

        if (bookingDetails == null)
        {
            TempData["Info"] = "Booking Details is null here!";
            return RedirectToAction("Index", "Schedule");
        }

        var routeSchedules = db.Schedules
      .Include(s => s.RouteLocation) // Include related RouteLocation entity
      .FirstOrDefault(s => s.Id == bookingDetails.ScheduleId);

        if (routeSchedules == null)
        {
            TempData["Error"] = "Route schedule not found!";
            return RedirectToAction("Index", "Schedule");
        }

        var ranks = db.Members.Include(s => s.Rank).FirstOrDefault(m => m.Id == User.Identity.Name);

        ViewBag.UserRanks = ranks?.Rank; // Pass only the Rank information
        ViewBag.RouteSchedules = routeSchedules;

        ViewBag.BookingDetails = bookingDetails;
        //Below is used for voucher Fetching

        var currentDate = DateOnly.FromDateTime(DateTime.Now);

        // Get the vouchers list
        var vouchersList = db.MemberVouchers
            .Where(mv => mv.MemberId == User.Identity!.Name)
            .Include(mv => mv.Voucher)
            .Where(mv => mv.Voucher.Status == "Active")
            .ToList();

        // Check if vouchersList contains any items and loop through them
        if (vouchersList.Any())
        {
            // Loop through the vouchers and extract the VoucherId
            var voucherIds = string.Join(", ", vouchersList.Select(mv => mv.VoucherId.ToString()));

            // Store the voucherIds in TempData
            TempData["Info"] = $"Vouceher have :{voucherIds}";
        }

        // Map to ViewModel
        var voucherUserDetailsList = vouchersList.Select(mv => new VoucherUserDetailsVM
        {
            VoucherId = mv.VoucherId,
            VoucherName = mv.Voucher.Name,
            CashDiscount = mv.Voucher.CashDiscount,
            Qty = mv.Amount
        }).ToList();

        // Pass to View
        ViewBag.VouchersList = voucherUserDetailsList;  // Using ViewBag to pass data
        return View(vm);
    }

    [Authorize(Roles ="Member")]
    public IActionResult PaidSubscribe()
    {
        if (string.IsNullOrEmpty(User.Identity?.Name))
        {
            return RedirectToAction("Login", "Account");
        }

        var subscription = db.Subscriptionses.FirstOrDefault(s => s.MemberId == User.Identity.Name && s.Paid == false);

        if (subscription == null)
        {
            ViewBag.Message = "No subscription details available.";
        }
        ViewBag.SubscribeDetails = subscription;

        return View();
    }


    [HttpPost]
    [Authorize(Roles = "Member")]
    public IActionResult PaidSubscribe(PaymentVM vm)
    {
        var memberSubscription = db.Subscriptionses.FirstOrDefault(s=>s.MemberId==User.Identity.Name && s.Paid==false);

        if (memberSubscription != null)
        {
            if (ModelState.IsValid)
            {
                // Mark the subscription as paid
                memberSubscription.Paid = true;
                memberSubscription.SubscribeDate = DateOnly.FromDateTime(DateTime.Now);

                // Find the member and update the IsSubscribeNewsletter property
                var member = db.Members.FirstOrDefault(m => m.Id == User.Identity.Name);
                if (member != null)
                {
                    member.MinSpend = +10;
                    member.Points += 10;
                    member.IsSubscribedToNewsletter = true;
                }

                // Save the changes to the database
                db.SaveChanges();

                TempData["Info"] = "You Have Successful Subscribe With Us. Great !";
                return RedirectToAction("UpdateProfile", "Account");
            }
            ViewBag.SubscribeDetails = db.Subscriptionses.FirstOrDefault(s => s.MemberId == User.Identity!.Name && s.Paid == false);
            return View(vm);
        }
        return RedirectToAction("Home", "Index");
    }

}


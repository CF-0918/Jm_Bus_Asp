using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Memory;

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

    public IActionResult Index()
    {
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
            TempData["Info"] = voucherIds;
        }
        else
        {
            // Store the voucherIds in TempData
            TempData["Info"] = "Nothing in the voucher list ";
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
        return View();
    }

   [Authorize]
    [HttpPost]
    public IActionResult Index(PaymentVM vm)
    {

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
                TempData["Info"] = voucherIds;
            }
            else
            {
                // Store the voucherIds in TempData
                TempData["Info"] = "Nothing in the voucher list ";
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
       
        return View();
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


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    [Authorize]
    public IActionResult Index()
    {
        var currentDate = DateOnly.FromDateTime(DateTime.Now);

        // Get the vouchers list
        var vouchersList = db.MemberVouchers
            .Where(mv => mv.MemberId == User.Identity!.Name)
            .Include(mv => mv.Voucher)
            .Where(mv => mv.Voucher.Status == "Active" &&
                         mv.Voucher.StartDate <= currentDate &&
                         mv.Voucher.EndDate >= currentDate)
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



    //[HttpPost]
    //[Authorize(Roles ="Members")]
    //public IActionResult Index()
    //{
    //    if (ModelState.IsValid)
    //    {

    //    }
    //    return View();
    //}
}

﻿using Azure;
using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace Demo.Controllers;

public class MembershipController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;

    public MembershipController(DB db, IWebHostEnvironment en, Helper hp)
    {

        this.db = db;
        this.en = en;
        this.hp = hp;
    }

    public bool CheckRankName(string name)
    {
        return !db.Ranks.Any(r => r.Name == name);
    }

    public bool CheckRangeDate(DateOnly EndDate, DateOnly StartDate)
    {
        return EndDate > StartDate; // Returns true if EndDate is greater than StartDate
    }

    public bool CheckDateIsTodayOrGreaterThan(DateOnly StartDate)
    {
        return StartDate >= DateOnly.FromDateTime(DateTime.Today);
    }




    public string GetNextPrefixId(string tableName)
    {
        string currentMaxId = "";

        if (tableName == "Ranks")
        {
            currentMaxId = db.Ranks.Max(m => m.Id) ?? "R0000"; // Default if table is empty
        }
        else if (tableName == "Vouchers")
        {
            currentMaxId = db.Vouchers.Max(u => u.Id) ?? "V0000"; // Adjust default prefix if needed
        }
        // Add more table cases as required
        else
        {
            throw new ArgumentException("Invalid table name.");
        }

        // Increment the ID
        int numericPart = int.Parse(currentMaxId.Substring(1)); // Assuming IDs start with a prefix
        string nextId = $"{currentMaxId[0]}{(numericPart + 1).ToString("D4")}"; // Format to "MXXXX"

        return nextId;
    }
    //Get
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult AddVoucher()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult AddVoucher(VoucherVM vm)
    {

        if (ModelState.IsValid("Name") && db.Ranks.Any(r => r.Name == vm.Name))
        {
            ModelState.AddModelError("Name", "Duplicated Rank Name.");
        }

        if (vm.PointNeeded < 0)
        {
            ModelState.AddModelError("PointNeeded", "Point must be a positive value.");
        }
        if (vm.CashDiscount < 0 ||vm.CashDiscount>101)
        {
            ModelState.AddModelError("CashDiscount", "Cash Discount  must be between RM 0 and RM 100.");
        }
        if (vm.EndDate < vm.StartDate)
        {
            ModelState.AddModelError("EndDate", "End date must be later than or equal to the start date.");
        }

        if (ModelState.IsValid)
        {
            string newVoucherId = GetNextPrefixId("Vouchers");
            db.Vouchers.Add(new()
            {
                Id = newVoucherId,
                Name = vm.Name,
                Description = vm.Description,
                PointNeeded = vm.PointNeeded,
                CashDiscount = vm.CashDiscount,
                StartDate=vm.StartDate,
                EndDate=vm.EndDate,
                Status=vm.Status,
                Qty=vm.Qty,
            });
            db.SaveChanges();
            TempData["Info"] = $"{newVoucherId} Added.";
            return RedirectToAction("ShowVoucherList");
        }

        return View(vm);
    }

    //Get
    public IActionResult Rank()
    {

        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult Rank(RankVM vm)
    {
        if (ModelState.IsValid("Name") &&db.Ranks.Any(r => r.Name == vm.Name))
        {
            ModelState.AddModelError("Name", "Duplicated Rank Name.");
        }

        if (vm.MinSpend < 0)
        {
            ModelState.AddModelError("MinSpend", "Min Spend must be a positive value.");
        }
        if (vm.Discounts < 0 || vm.Discounts > 100)
        {
            ModelState.AddModelError("Discounts", "Discounts Percentage must be between 0 and 100.");
        }

        if (ModelState.IsValid)
        {
            string newRankId = GetNextPrefixId("Ranks");
            db.Ranks.Add(new()
            {
                Id = newRankId,
                Name = vm.Name,
                Description = vm.Description,
                Discounts = vm.Discounts,
                MinSpend = vm.MinSpend
            });
            db.SaveChanges();
            TempData["Info"] = $"{vm.Name} Added.";
            return RedirectToAction("ShowRankList");
        }

        return View(vm);
    }

    public IActionResult EditRank(string id)
    {

        // Get staff/admin  record based on email (PK)
        var m = db.Ranks.Find(id);
        if (m == null) return RedirectToAction("EditRank", "Membership");

        var vm = new EditRankVM
        {
            Name = m.Name,
            Description = m.Description,
            Discounts = m.Discounts,
            MinSpend = m.MinSpend,
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult EditRank(EditRankVM vm, string id)
    {
        var m = db.Ranks.Find(id);
        if (db.Ranks.Any(r => r.Name == vm.Name && r.Id != id))
        {
            ModelState.AddModelError("Name", "Duplicated Rank Name.");
        }

        // Other validation and logic...


        if (vm.MinSpend < 0)
        {
            ModelState.AddModelError("MinSpend", "Min Spend must be a positive value.");
        }
        if (vm.Discounts < 0 || vm.Discounts > 100)
        {
            ModelState.AddModelError("Discounts", "Discounts Percentage must be between 0 and 100.");
        }

        if (ModelState.IsValid)
        {
            {
                m.Name = vm.Name;
                m.Description = vm.Description;
                m.Discounts = vm.Discounts;
                m.MinSpend = vm.MinSpend;
            };
            db.SaveChanges();
            TempData["Info"] = $"{vm.Name} Edited.";
            return RedirectToAction();
        }

        return View(vm);
    }

    //Get Request
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult ShowRankList(string? name, string? sort, string? dir, int page = 1)
    {
        // (1) Searching ------------------------
        ViewBag.Name = name = name?.Trim() ?? "";

        var searched = db.Ranks.Where(s => s.Name.Contains(name));

        // (2) Sorting --------------------------
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        Func<Rank, object> fn = sort switch
        {
            "Id" => s => s.Id,
            "Name" => s => s.Name,
            "Description" => s => s.Description,
            "MinSpend" => s => s.MinSpend,
            "Discounts" => s => s.Discounts,
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
            return PartialView("_RankList", m);
        }


        return View(m);
    }

    [Authorize(Roles = "Staff,Admin")]
    public IActionResult ShowVoucherList(string? name, string? sort, string? dir, int page = 1)
    {
        // (1) Searching ------------------------
        ViewBag.Name = name = name?.Trim() ?? "";

        var searched = db.Vouchers.Where(s => s.Name.Contains(name));

        // (2) Sorting --------------------------
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        Func<Voucher, object> fn = sort switch
        {
            "Id" => s => s.Id,
            "Name" => s => s.Name,
            "Description" => s => s.Description,
            "PointNeeded" => s => s.PointNeeded,
            "CashDiscount" => s => s.CashDiscount,
            "Qty" => s => s.Qty,
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
            return PartialView("_VoucherList", m);
        }


        return View(m);
    }
    // GET: VoucherRedeem
    [Authorize(Roles = "Member")]
    public IActionResult VoucherRedeem()
    {
        // Get valid vouchers where EndDate is greater than or equal to current date
        var validVouchers = db.Vouchers.Where(v =>
                    v.EndDate >= DateOnly.FromDateTime(DateTime.Now) &&
                    v.Status != "Draft" &&
                    v.Status != "Inactive"
                ).ToList();
        ViewBag.Vouchers = validVouchers;

 
        var member = db.Members.Find(User.Identity!.Name);
        if (member != null)
        {
            ViewBag.Points = member.Points;
        }

        return View();
    }

    public IActionResult EditVoucher(string id)
    {

        // Get staff/admin  record based on email (PK)
        var m = db.Vouchers.Find(id);
        if (m == null) return RedirectToAction("EditVoucher", "Membership");

        var vm = new EditVoucherVM
        {
            Name = m.Name,
            Description = m.Description,
            PointNeeded = m.PointNeeded,
            CashDiscount = m.CashDiscount,
            Qty = m.Qty,
            StartDate = m.StartDate,
            EndDate = m.EndDate,
            Status = m.Status,
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult EditVoucher(EditVoucherVM vm, string id)
    {
        var m = db.Vouchers.Find(id);
        if (db.Vouchers.Any(r => r.Name == vm.Name && r.Id != id))
        {
            ModelState.AddModelError("Name", "Duplicated Voucher Name.");
        }

        if (vm.PointNeeded < 0)
        {
            ModelState.AddModelError("PointNeeded", "Point must be a positive value.");
        }
        if (vm.CashDiscount < 0 || vm.CashDiscount > 101)
        {
            ModelState.AddModelError("CashDiscount", "Cash Discount  must be between RM 0 and RM 100.");
        }
        if (vm.EndDate < vm.StartDate)
        {
            ModelState.AddModelError("EndDate", "End date must be later than or equal to the start date.");
        }

        if (ModelState.IsValid)
        {
            {
                m.Name = vm.Name;
                m.Description = vm.Description;
                m.PointNeeded = vm.CashDiscount;
                m.CashDiscount = vm.CashDiscount;
                m.Qty = vm.Qty;
                m.StartDate = vm.StartDate;
                m.EndDate = vm.EndDate;
                m.Status = vm.Status;
            };
            db.SaveChanges();
            TempData["Info"] = $"{vm.Name} Edited.";
            return RedirectToAction();
        }

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult DeleteVoucher(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            TempData["Error"] = "Invalid voucher ID.";
            return RedirectToAction("ShowVoucherList"); // Adjust as necessary for the bus list view
        }

        var voucher = db.Vouchers.Find(id);

        if (voucher != null)
        {
            // Set the status to "Inactive" to disable the bus (instead of deleting)
            voucher.Status = "Draft";
            db.SaveChanges();

            TempData["Info"] = "Voucher disabled.";
        }
        else
        {
            TempData["Error"] = "Voucher not found.";
        }

        return RedirectToAction("ShowVoucherList"); // Redirect to the bus list page
    }



    // POST: Maintenance/DeleteMany
    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult DeleteManyVoucher(string[] ids)
    {
        if (ids != null && ids.Length > 0)
        {
            // Fetch the buss to update based on the provided IDs
            var voucherToUpdate = db.Vouchers.Where(s => ids.Contains(s.Id)).ToList();

            // Update the Status property to "Inactive"
            foreach (var voucher in voucherToUpdate)
            {
                voucher.Status = "Draft";
            }

            // Save the changes to the database
            db.SaveChanges();

            TempData["Info"] = $"{voucherToUpdate.Count} voucher(s) set to inactive.";
        }

        return RedirectToAction("ShowVoucherList"); // Redirect to the bus list page
    }

    [HttpPost]
    public IActionResult EnableVoucher(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            TempData["Error"] = "Invalid voucher ID.";
            return RedirectToAction("ShowVoucherList"); // Adjust as necessary for the bus list view
        }

        var bus = db.Vouchers.Find(id);

        if (bus != null)
        {
            // Set the status to "Active" to enable the bus
            bus.Status = "Active";
            db.SaveChanges();

            TempData["Info"] = "Voucher enabled.";
        }
        else
        {
            TempData["Error"] = "Voucher not found.";
        }

        return RedirectToAction("ShowVoucherList"); // Redirect to the bus list page
    }

    // POST: VoucherRedeem
    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult VoucherRedeem(VoucherRedeem vm)
    {
        if (ModelState.IsValid)
        {
            // Get current member ID from the logged-in user
            string memberId = User.Identity!.Name;

            // Get the voucher and check its validity
            var voucher = db.Vouchers.FirstOrDefault(v =>
                v.Id == vm.Id &&
                v.EndDate >= DateOnly.FromDateTime(DateTime.Now) &&
                v.Status != "Draft" &&
                v.Status != "Inactive" &&
                v.Qty > 0);

            var member = db.Members.Find(memberId);
            if (member == null) return RedirectToAction("Index", "Home");

            if (voucher == null)
            {
                // Handle case where voucher is invalid
                ViewBag.Vouchers = db.Vouchers.Where(v =>
                    v.EndDate >= DateOnly.FromDateTime(DateTime.Now) &&
                    v.Status != "Draft" &&
                    v.Status != "Inactive").ToList();
                ViewBag.Points = member.Points;
                TempData["Info"] = "Voucher is either expired, inactive, or fully redeemed.";
                return View(vm);
            }

            // Get the member data to check if they have enough points
            if (member.Points < voucher.PointNeeded)
            {
                TempData["Info"] = "You do not have enough points to redeem this voucher.";
                ViewBag.Vouchers = db.Vouchers.Where(v =>
                    v.EndDate >= DateOnly.FromDateTime(DateTime.Now) &&
                    v.Status != "Draft" &&
                    v.Status != "Inactive").ToList();
                ViewBag.Points = member.Points;
                return View(vm);
            }

            // Handle redemption logic (insert or update MemberVoucher record)
            var currentVoucher = db.MemberVouchers.FirstOrDefault(mv => mv.VoucherId == vm.Id && mv.MemberId == memberId);
            if (currentVoucher == null)
            {
                // Insert a new redemption record
                var memberVoucher = new MemberVoucher
                {
                    MemberId = memberId,
                    VoucherId = voucher.Id,
                    Amount = 1 // Assuming one voucher redeemed per action
                };
                db.MemberVouchers.Add(memberVoucher);
            }
            else
            {
                currentVoucher.Amount += 1;
            }

            // Reduce the voucher quantity // user points
            voucher.Qty--;
            member.Points = member.Points - voucher.PointNeeded;
            // Save changes to the database
            db.SaveChanges();

            TempData["Info"] = "Success! You have successfully redeemed your voucher. <a href='/Membership/MyVoucher' class='text-white'>View Voucher</a>";
            return RedirectToAction("VoucherRedeem");
        }

        // If model state is invalid, pass valid vouchers to the view
        ViewBag.Vouchers = db.Vouchers.Where(v =>
                     v.EndDate >= DateOnly.FromDateTime(DateTime.Now) &&
                     v.Status != "Draft" &&
                     v.Status != "Inactive"
                     ).ToList();

        // Get member points for the view
        var memberForPoints = db.Members.FirstOrDefault(m => m.Id == User.Identity!.Name);
        if (memberForPoints != null)
        {
            ViewBag.Points = memberForPoints.Points;
        }

        return View(vm);
    }



    [Authorize(Roles = "Member")]
    public IActionResult MyVoucher()
    {
        var userId = User.Identity.Name;

        var vouchers = db.MemberVouchers
            .Where(mv => mv.MemberId == userId)
            .Include(mv => mv.Voucher)
            .Select(mv => new VoucherViewModel
            {
                VoucherId = mv.VoucherId,
                VoucherName = mv.Voucher.Name,
                Description = mv.Voucher.Description,
                StartDate = mv.Voucher.StartDate,
                EndDate = mv.Voucher.EndDate,
                CashDiscount = mv.Voucher.CashDiscount,
                PointNeeded = mv.Voucher.PointNeeded,
                Status = mv.Voucher.Status,
                Amount = mv.Amount
            }).ToList();

        // Ensure you're passing the model to the view directly
        return View(vouchers);
    }


}

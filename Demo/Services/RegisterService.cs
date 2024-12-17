using Microsoft.AspNetCore.Hosting;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json.Nodes;
namespace Demo.Services;

public class RegisterService
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly IHttpContextAccessor ct;
    private readonly IConfiguration cf;

    public RegisterService(DB db, IWebHostEnvironment en,
                  IHttpContextAccessor ct,
                  IConfiguration cf)
    {
        this.db = db;
        this.en = en;
        this.ct = ct;
        this.cf = cf;
    }


    public void UpdateVoucherStatus()
    {
        // Get today's date using DateOnly
        DateOnly currentDate = DateOnly.FromDateTime(DateTime.Now);

        // Activate vouchers where the start date matches today's date
        var vouchersToActivate = db.Vouchers
            .Where(v => v.Status == "Inactive" && v.StartDate <= currentDate && v.EndDate >= currentDate)
            .ToList();

        foreach (var voucher in vouchersToActivate)
        {
            voucher.Status = "Active";
        }

        // Set expired status for vouchers where the end date has passed and the status is not "Inactive"
        var vouchersToExpire = db.Vouchers
            .Where(v => v.Status != "InActive" && v.EndDate < currentDate)
            .ToList();

        foreach (var voucher in vouchersToExpire)
        {
            voucher.Status = "Expired";
        }

        // Save the changes to the database
        db.SaveChanges();
    }

    public void PreventInvalidAccessPage()
    {
        // Get the user ID from claims
        var userId = ct.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;

        // If the user ID exists, check their status in the database
        if (!string.IsNullOrEmpty(userId))
        {
            var user = db.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null && user.Status == "Blocked") // Assuming 'Status' tracks if the user is banned
            {
                // Log out the user if blocked
                ct.HttpContext.SignOutAsync();
                ct.HttpContext.Response.Redirect("/Account/Login"); // Redirect to login page
            }
        }
    }

}

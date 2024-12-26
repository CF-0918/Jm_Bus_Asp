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
using Microsoft.EntityFrameworkCore;
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

    public void CheckPendingBookingsDurationAndUpdate()
    {
        var pendingBookings = db.Bookings
            .Where(b => b.Status == "Pending") // Select only pending bookings
            .ToList();

        foreach (var booking in pendingBookings)
        {
            // Calculate the time difference between booking creation and now
            if (booking.BookingDateTime.AddMinutes(2) <= DateTime.Now)
            {
                // Update the booking status to "Cancelled"
                booking.Status = "Cancelled";

                // Find all BookingSeats associated with this booking and update their statuses
                var bookingSeats = db.BookingSeats
                    .Where(bs => bs.BookingId == booking.Id)
                    .ToList();

                foreach (var seat in bookingSeats)
                {
                    seat.Status = "Cancelled";
                }
            }
        }

        // Save changes to the database
        db.SaveChanges();
    }

    public void RealTimeUpdateMemberRank()
    {
        var memberList = db.Members.ToList();
        var rankList = db.Ranks.OrderByDescending(r => r.MinSpend).ToList(); // Sort ranks by MinSpend descending

        foreach (var member in memberList)
        {
            foreach (var rank in rankList)
            {
                // Check if the member meets the minimum spend requirement for the rank
                if (member.MinSpend >= rank.MinSpend)
                {
                    // Only update if the new rank is higher than the current rank
                    if (member.RankId == null || rank.MinSpend > rankList.First(r => r.Id == member.RankId).MinSpend)
                    {
                        member.RankId = rank.Id; // Assign the highest eligible rank
                    }
                    break; // Stop checking lower ranks since ranks are sorted by MinSpend
                }
            }
        }

        db.SaveChanges();
    }

    public void UpdateRealTimeSchedulBookingsStatus()
    {
        // Get current date as DateOnly
        var today = DateOnly.FromDateTime(DateTime.Now);

        // Retrieve schedules with DepartDate older than today and status as "Active"
        var scheduleList = db.Schedules
            .Where(s => s.DepartDate < today && s.Status == "Active")
            .ToList();

        foreach (var schedule in scheduleList)
        {
            // Update schedule status to "Expired"
            schedule.Status = "Expired";

            // Find all "Booked" bookings associated with this schedule
            var bookingList = db.Bookings
                .Where(b => b.ScheduleId == schedule.Id && b.Status == "Booked")
                .ToList();

            foreach (var booking in bookingList)
            {
                // Update booking status to "Expired"
                booking.Status = "Expired";
            }
        }

        // Save changes to the database after all updates
        db.SaveChanges();
    }



}

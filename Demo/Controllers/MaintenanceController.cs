﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace Demo.Controllers
{
    public class MaintenanceController : Controller
    {
        private readonly DB db;
        private readonly IWebHostEnvironment en;
        private readonly Helper hp;

        public MaintenanceController(DB db, IWebHostEnvironment en, Helper hp)
        {

            this.db = db;
            this.en = en;
            this.hp = hp;
        }
        public IActionResult Dashboard()
        {
            return View();
        }
        public IActionResult MemberMaintenance()
        {
            return View();
        }

        public IActionResult StaffList(string? name, string? sort, string? dir, int page = 1)
        {
            // (1) Searching ------------------------
            ViewBag.Name = name = name?.Trim() ?? "";

            // Filter staff based on the search term
            var searched = db.Staffs.Where(s => string.IsNullOrEmpty(name) || s.FirstName.Contains(name) || s.LastName.Contains(name));

            // (2) Sorting --------------------------
            ViewBag.Sort = sort;
            ViewBag.Dir = dir;

            // Define sorting logic
            Func<Staff, object> fn = sort switch
            {
                "Id" => s => s.Id,
                "FirstName" => s => s.FirstName,
                "LastName" => s => s.LastName,
                "Email" => s => s.Email,
                _ => s => s.FirstName + " " + s.LastName, // Default to Full Name sorting
            };

            // Apply sorting direction
            var sorted = dir == "des" ?
                         searched.OrderByDescending(fn) :
                         searched.OrderBy(fn);

            // (3) Paging ---------------------------
            if (page < 1)
            {
                return RedirectToAction(null, new { name, sort, dir, page = 1 });
            }

            // Use a package like PagedList.Mvc for pagination
            var paged = sorted.ToPagedList(page, 10); // 10 items per page

            if (page > paged.PageCount && paged.PageCount > 0)
            {
                return RedirectToAction(null, new { name, sort, dir, page = paged.PageCount });
            }

            //// If it's an Ajax request, return a partial view
            //if (Request.IsAjax())
            //{
            //    return PartialView("_StaffListPartial", paged);
            //}

            return View(paged); // Return the paged list to the view
        }

        // Individual Deletion
        [HttpPost]
        public IActionResult DeleteStaff(string? id)
        {
            var staff = db.Staffs.Find(id);

            if (staff != null)
            {
                db.Staffs.Remove(staff);
                db.SaveChanges();

                TempData["Info"] = "Record deleted.";
            }

            return Redirect(Request.Headers.Referer.ToString()); // Redirect to the previous page
        }

        // POST: Maintenance/DeleteMany
        [HttpPost]
        public IActionResult DeleteMany(string[] ids)
        {
            if (ids != null && ids.Length > 0)
            {
                var staffToDelete = db.Staffs.Where(s => ids.Contains(s.Id)).ToList();
                db.Staffs.RemoveRange(staffToDelete);
                db.SaveChanges();

                TempData["Info"] = $"{staffToDelete.Count} staff member(s) deleted.";
            }

            return RedirectToAction("StaffList"); // Redirect to the staff list page
        }

        // GET: Account/UpdateProfile
        //[Authorize(Roles = "Member")]
        public IActionResult StaffDetails(string id)
        {
            // Get member record based on email (PK)
            // TODO
            var staff = db.Staffs.FirstOrDefault(s => s.Id == id);
            if (staff == null) return RedirectToAction("Index", "Home");

            var vm = new StaffDetailsVM
            {
                Id = staff.Id,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                Age = staff.Age,
                IcNo = staff.IcNo,
                Gender = staff.Gender,
 
                Email = staff.Email,
                PhoneNo = staff.Phone,

                Status = staff.Status,
                PhotoURL = staff.PhotoURL,
            };

            return View(vm);
        }

        //// POST: Account/UpdateProfile
        //// TODO
        //[Authorize(Roles = "Member")]
        [HttpPost]
        public IActionResult StaffDetails(StaffDetailsVM vm)
        {
            var staff = db.Staffs.FirstOrDefault(s => s.Id == vm.Id);
            if (staff == null) return RedirectToAction("Index", "Home");

            if (vm.Photo != null)
            {
                var err = hp.ValidatePhoto(vm.Photo);
                if (err != "") ModelState.AddModelError("Photo", err);
            }

            if (ModelState.IsValid)
            {

                staff.FirstName = vm.FirstName;
                staff.LastName = vm.LastName;
                staff.Gender = vm.Gender;

                staff.Phone = vm.PhoneNo;



                if (vm.Photo != null)
                {
                    hp.DeletePhoto(staff.PhotoURL, "photo/users");
                    staff.PhotoURL = hp.SavePhoto(vm.Photo, "photo/users");
                }

                db.SaveChanges();

                TempData["Info"] = "Profile updated.";
                return RedirectToAction();
            }
            else
            {
                TempData["Info"] = "Profile Not updated.";
                // TODO
                vm.Id = staff.Id;
                vm.Email = staff.Email;
                vm.Age = staff.Age;
                vm.IcNo = staff.IcNo;
                vm.Status = staff.Status;
                vm.PhotoURL = staff.PhotoURL;

            }
            return View(vm);
        }
        public IActionResult MemberList(string? name, string? sort, string? dir, int page = 1)
        {
            // (1) Searching ------------------------
            ViewBag.Name = name = name?.Trim() ?? "";

            // Filter member based on the search term
            var searched = db.Members.Where(s => string.IsNullOrEmpty(name) || s.FirstName.Contains(name) || s.LastName.Contains(name));

            // (2) Sorting --------------------------
            ViewBag.Sort = sort;
            ViewBag.Dir = dir;

            // Define sorting logic
            Func<Member, object> fn = sort switch
            {
                "Id" => s => s.Id,
                "FirstName" => s => s.FirstName,
                "LastName" => s => s.LastName,
                "Email" => s => s.Email,
                _ => s => s.FirstName + " " + s.LastName, // Default to Full Name sorting
            };

            // Apply sorting direction
            var sorted = dir == "des" ?
                         searched.OrderByDescending(fn) :
                         searched.OrderBy(fn);

            // (3) Paging ---------------------------
            if (page < 1)
            {
                return RedirectToAction(null, new { name, sort, dir, page = 1 });
            }

            // Use a package like PagedList.Mvc for pagination
            var paged = sorted.ToPagedList(page, 10); // 10 items per page

            if (page > paged.PageCount && paged.PageCount > 0)
            {
                return RedirectToAction(null, new { name, sort, dir, page = paged.PageCount });
            }

            // If it's an Ajax request, return a partial view
            if (Request.IsAjax())
            {
                return PartialView("_MemberListPartial", paged);
            }

            return View(paged); // Return the paged list to the view
        }
        [HttpPost]
        public IActionResult DeleteMember(string? id)
        {
            var member = db.Members.Find(id);

            if (member != null)
            {
                db.Members.Remove(member);
                db.SaveChanges();

                TempData["Info"] = "Record deleted.";
            }

            return Redirect(Request.Headers.Referer.ToString()); // Redirect to the previous page
        }

        // POST: Maintenance/DeleteMany
        [HttpPost]
        public IActionResult DeleteManyMembers(string[] ids)
        {
            if (ids != null && ids.Length > 0)
            {
                var memberToDelete = db.Members.Where(s => ids.Contains(s.Id)).ToList();
                db.Members.RemoveRange(memberToDelete);
                db.SaveChanges();

                TempData["Info"] = $"{memberToDelete.Count} member member(s) deleted.";
            }

            return RedirectToAction("MemberList"); // Redirect to the member list page
        }

        // GET: Account/UpdateProfile
        //[Authorize(Roles = "Member")]
        public IActionResult MemberDetails(string id)
        {
            // Get member record based on email (PK)
            // TODO
            var member = db.Members.FirstOrDefault(s => s.Id == id);
            if (member == null) return RedirectToAction("Index", "Home");

            var vm = new MemberDetailsVM
            {
                Id = member.Id,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Age = member.Age,
                IcNo = member.IcNo,
                Gender = member.Gender,

                Email = member.Email,
                PhoneNo = member.Phone,

                Status = member.Status,
                PhotoURL = member.PhotoURL,
            };

            return View(vm);
        }

        //// POST: Account/UpdateProfile
        //// TODO
        //[Authorize(Roles = "Member")]
        [HttpPost]
        public IActionResult MemberDetails(MemberDetailsVM vm)
        {
            var member = db.Members.FirstOrDefault(s => s.Id == vm.Id);
            if (member == null) return RedirectToAction("Index", "Home");

            if (vm.Photo != null)
            {
                var err = hp.ValidatePhoto(vm.Photo);
                if (err != "") ModelState.AddModelError("Photo", err);
            }

            if (ModelState.IsValid)
            {

                member.FirstName = vm.FirstName;
                member.LastName = vm.LastName;
                member.Gender = vm.Gender;

                member.Phone = vm.PhoneNo;



                if (vm.Photo != null)
                {
                    hp.DeletePhoto(member.PhotoURL, "photo/users");
                    member.PhotoURL = hp.SavePhoto(vm.Photo, "photo/users");
                }

                db.SaveChanges();

                TempData["Info"] = "Profile updated.";
                return RedirectToAction();
            }
            else
            {
                TempData["Info"] = "Profile Not updated.";
                // TODO
                vm.Id = member.Id;
                vm.Email = member.Email;
                vm.Age = member.Age;
                vm.IcNo = member.IcNo;
                vm.Status = member.Status;
                vm.PhotoURL = member.PhotoURL;

            }
            return View(vm);
        }




    }


}

using Azure;
using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        [Authorize(Roles = "Staff,Admin")]
        public IActionResult Dashboard()
        {
            return View();
        }
        [Authorize(Roles = "Staff,Admin")]
        public IActionResult MemberMaintenance()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
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

            // If it's an Ajax request, return a partial view
            if (Request.IsAjax())
            {
                return PartialView("_StaffListPartial", paged);
            }

            return View(paged); // Return the paged list to the view
        }

        // Individual Deletion
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteStaff(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Invalid staff ID.";
                return RedirectToAction("StaffList");
            }

            var staff = db.Staffs.Find(id);

            if (staff != null)
            {
                // Set the status to "Inactive" to disable the staff member
                staff.Status = "Inactive";
                db.SaveChanges();

                TempData["Info"] = "Staff member disabled.";
            }
            else
            {
                TempData["Error"] = "Staff member not found.";
            }

            return RedirectToAction("StaffList");
        }


        // POST: Maintenance/DeleteMany
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteMany(string[] ids)
        {
            if (ids != null && ids.Length > 0)
            {
                // Fetch staff records to disable
                var staffToDisable = db.Staffs.Where(s => ids.Contains(s.Id)).ToList();

                // Set their status to "Inactive"
                foreach (var staff in staffToDisable)
                {
                    staff.Status = "Inactive";
                }

                // Save changes to the database
                db.SaveChanges();

                TempData["Info"] = $"{staffToDisable.Count} staff member(s) disabled.";
            }

            return RedirectToAction("StaffList"); // Redirect to the staff list page
        }

        [HttpPost]
        public IActionResult EnableStaff(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Invalid staff ID.";
                return RedirectToAction("StaffList");
            }

            var staff = db.Staffs.Find(id);

            if (staff != null)
            {
                // Set the status to "Active" to enable the staff member
                staff.Status = "Active";
                db.SaveChanges();

                TempData["Info"] = "Staff member enabled.";
            }
            else
            {
                TempData["Error"] = "Staff member not found.";
            }

            return RedirectToAction("StaffList");
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

        [HttpPost]
        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Staff,Admin")]
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
        [Authorize(Roles = "Staff,Admin")]
        public IActionResult DeleteMember(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Invalid member ID.";
                return RedirectToAction("MemberList"); // Adjust as necessary for the member list view
            }

            var member = db.Members.Find(id);

            if (member != null)
            {
                // Set the status to "Inactive" to disable the member (instead of deleting)
                member.Status = "Inactive";
                db.SaveChanges();

                TempData["Info"] = "Member disabled.";
            }
            else
            {
                TempData["Error"] = "Member not found.";
            }

            return RedirectToAction("MemberList"); // Redirect to the member list page
        }



        // POST: Maintenance/DeleteMany
        [HttpPost]
        [Authorize(Roles = "Staff,Admin")]
        public IActionResult DeleteManyMembers(string[] ids)
        {
            if (ids != null && ids.Length > 0)
            {
                // Fetch the members to update based on the provided IDs
                var membersToUpdate = db.Members.Where(s => ids.Contains(s.Id)).ToList();

                // Update the Status property to "Inactive"
                foreach (var member in membersToUpdate)
                {
                    member.Status = "Inactive";
                }

                // Save the changes to the database
                db.SaveChanges();

                TempData["Info"] = $"{membersToUpdate.Count} member(s) set to inactive.";
            }

            return RedirectToAction("MemberList"); // Redirect to the member list page
        }

        [HttpPost]
        public IActionResult EnableMember(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Invalid member ID.";
                return RedirectToAction("MemberList"); // Adjust as necessary for the member list view
            }

            var member = db.Members.Find(id);

            if (member != null)
            {
                // Set the status to "Active" to enable the member
                member.Status = "Active";
                db.SaveChanges();

                TempData["Info"] = "Member enabled.";
            }
            else
            {
                TempData["Error"] = "Member not found.";
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

       
        [HttpPost]
        [Authorize(Roles = "Staff,Admin")]
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

        [Authorize(Roles = "Staff,Admin")]
        public IActionResult AddBus()
        {
            // Fetch the list of categories from the database
            ViewBag.CategoryList = new SelectList(db.CategoryBuses, "Id", "Name");
            return View();
        }


        [Authorize(Roles = "Staff,Admin")]
        [HttpPost]
        public IActionResult AddBus(AddBusVM vm)
        {
            if (vm.Photo != null)
            {
                var err = hp.ValidatePhoto(vm.Photo);
                if (err != "") ModelState.AddModelError("Photo", err);
            }


            if (ModelState.IsValid)
            {
                // Generate Unique Bus ID
                string newBusId;
                var lastBus = db.Buses.OrderByDescending(b => b.Id).FirstOrDefault();
                if (lastBus != null)
                {
                    int numericPart = int.Parse(lastBus.Id.Substring(1)); // Extract numeric part after 'B'
                    newBusId = "B" + (numericPart + 1).ToString("D3"); // Increment and format as B001, B002, etc.
                }
                else
                {
                    newBusId = "B001"; // First bus ID
                }

                // Create Bus Entity
                var newBus = new Bus
                {
                    Id = newBusId,
                    Name=vm.Name,
                    BusPlate = vm.BusPlate,
                    Status = vm.Status,
                    Capacity = vm.Capacity,
                    PhotoURL = hp.SavePhoto(vm.Photo, "photo/users"),
                    CategoryBusId = vm.CategoryBusId
                };

                db.Buses.Add(newBus);
                db.SaveChanges();

                //save the seat 
                for (int i = 1; i <= vm.Capacity; i++)
                {
                    var seat = new Seat
                    {
                        BusId = newBus.Id,              // Foreign key linking the seat to the bus
                        SeatNo = "Seat" + i.ToString("D2") // Format seat number as S01, S02, etc.
                    };
                    db.Seats.Add(seat);
                }
                db.SaveChanges();


                TempData["Info"] = "Bus Added Successfully.";
                return RedirectToAction("ShowBusList", "Maintenance");
            }
            // Fetch the list of categories from the database
            ViewBag.CategoryList = new SelectList(db.CategoryBuses, "Id", "Name");
            return View(vm);
        }

        //Get Request [Show Bus List]
        public IActionResult ShowBusList(string? name, string? sort, string? dir, int page = 1)
        {
            // (1) Searching ------------------------
            ViewBag.Name = name = name?.Trim() ?? "";

            var searched = db.Buses.Where(s => s.Name.Contains(name));

            // (2) Sorting --------------------------
            ViewBag.Sort = sort;
            ViewBag.Dir = dir;

            Func<Bus, object> fn = sort switch
            {
                "Id" => s => s.Id,
                "Name" => s => s.Name,
                "Status" => s => s.Status,
                "Capacity" => s => s.Capacity,
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
                return PartialView("_BusList", m);
            }


            return View(m);
        }

        [Authorize(Roles = "Staff,Admin")]
        public IActionResult AddCategoryBus()
        {
            // Pass existing categories to the view for display
            ViewBag.CategoryBuses = db.CategoryBuses;
            return View();
        }

        [Authorize(Roles = "Staff,Admin")]
        [HttpPost]
        public IActionResult AddCategoryBus(AddCategoryBus vm)
        {
            if (ModelState.IsValid)
            {
                // Determine the new ID
                string newId;
                var lastCategory = db.CategoryBuses.OrderByDescending(c => c.Id).FirstOrDefault();
                if (lastCategory != null)
                {
                    // Extract the numeric part, increment it, and format as CB###
                    int lastNumber = int.Parse(lastCategory.Id.Substring(2));
                    newId = "CB" + (lastNumber + 1).ToString("D3");
                }
                else
                {
                    // First entry, start with CB001
                    newId = "CB001";
                }

                // Create and add the new category
                var newCategory = new CategoryBus
                {
                    Id = newId,
                    Name = vm.Name
                };
                db.CategoryBuses.Add(newCategory);
                db.SaveChanges();

                // Provide feedback to the user
                TempData["Info"] = $"{vm.Name} has been added!";
                return RedirectToAction();
            }
            else
            {
                TempData["Error"] = "Invalid input. Please correct the errors and try again.";
            }

            // Reload categories for the view
            ViewBag.CategoryBuses = db.CategoryBuses;
            return View(vm);
        }

        public bool CheckDepartAndDestination(string destination, string depart)
        {
             return destination != depart;
        }
        public IActionResult AddRoute()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddRoute(RouteVM vm)
        {
            if (vm.Depart == vm.Destination)
            {
                ModelState.AddModelError("", "Depart and Destination Should Not Be The Same !");
            }
            if (vm.EstimatedTimeHour <= 0 || vm.EstimatedTimeHour > 12)
            {
                ModelState.AddModelError("EstimatedTimeHour", "Hour should not more than 12 h or less than 1");
            }
            if (vm.EstimatedTimeMin <= 0 || vm.EstimatedTimeMin > 60)
            {
                ModelState.AddModelError("EstimatedTimeHour", "Hour should not more than 60 m or less than 1");
            }
           bool DuplicatedRouteRecord=db.RouteLocations.Any(rl => rl.Depart == vm.Depart && rl.Destination == vm.Destination);
            if (DuplicatedRouteRecord)
            {
                ModelState.AddModelError("", "Duplicated Same Record For Depart & Destinatio Locations");
            }
            if (ModelState.IsValid)
            {
                // Generate Unique Route ID
                string newRouteId;
                var lastRoute = db.RouteLocations.OrderByDescending(b => b.Id).FirstOrDefault();
                if (lastRoute != null)
                {
                    int numericPart = int.Parse(lastRoute.Id.Substring(2));
                    newRouteId = "RT" + (numericPart + 1).ToString("D3");
                }
                else
                {
                    newRouteId = "RT001"; // First Route ID
                }

                // Create Bus Entity
                var newRoute = new RouteLocation
                {
                    Id = newRouteId,
                    Depart = vm.Depart,
                    Destination = vm.Destination,
                    Hour = vm.EstimatedTimeHour,
                    Min = vm.EstimatedTimeMin,
                };

                db.RouteLocations.Add(newRoute);
                db.SaveChanges();
                // Provide feedback to the user
                TempData["Info"] = $"{newRouteId} has been added!";
                return RedirectToAction("ShowRouteList");
            }
            return View(vm);
        }

        public IActionResult ShowRouteList(string? name, string? sort, string? dir, int page = 1)
        {
        
            // (1) Searching ------------------------
            ViewBag.Name = name = name?.Trim() ?? "";

            var searched = db.RouteLocations.Where(s => s.Depart.Contains(name));

            // (2) Sorting --------------------------
            ViewBag.Sort = sort;
            ViewBag.Dir = dir;

            Func<RouteLocation, object> fn = sort switch
            {
                "Id" => s => s.Id,
                "Depart" => s => s.Depart,
                "Destination" => s => s.Destination,
                "Hour" => s => s.Hour,
                "Min" => s => s.Min,
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
                return PartialView("_RouteList", m);
            }


            return View(m);
        
    }

    }

}

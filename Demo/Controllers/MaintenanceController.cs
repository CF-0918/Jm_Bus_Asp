using Azure;
using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList.Extensions;
using static Demo.Models.AddCategoryBus;

namespace Demo.Controllers;

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

    //[Authorize(Roles = "Admin")]
    public IActionResult StaffList(string? name, string? sort, string? dir, string? status, int page = 1)
    {
        // (1) Searching ------------------------
        ViewBag.Name = name = name?.Trim() ?? "";

        // Filter staff based on the search term
        var searched = db.Staffs.Where(s => string.IsNullOrEmpty(name) || s.FirstName.Contains(name) || s.LastName.Contains(name));

        ViewBag.Status = status = status?.Trim() ?? "All";

        if (status != "All")
        {
            searched = searched.Where(s => s.Status == status);
        }

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
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult UpdateStaffStatus(string id, [FromBody] UpdateStatusVM model)
    {
        if (string.IsNullOrEmpty(id) || model == null)
        {
            return BadRequest("Invalid request.");
        }

        var staff = db.Staffs.Find(id);
        if (staff == null)
        {
            return NotFound("Staff member not found.");
        }

        staff.Status = model.Status; // Update the status
        db.SaveChanges();

        return Ok(new { message = "Status updated successfully." });
    }


    // POST: Maintenance/DeleteMany
    [HttpPost]
    //[Authorize(Roles = "Admin")]
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

    //[Authorize(Roles = "Staff,Admin")]
    public IActionResult EditStaff(string id)
    {
        // Get staff/admin  record based on email (PK)
        var m = db.Staffs.Find(id);
        if (m == null) return RedirectToAction("EditStaff", "Maintenance");

        var vm = new EditStaffVM
        {
            Id = m.Id,
            FirstName = m.FirstName,
            LastName = m.LastName,
            Age = m.Age,
            IcNo = m.IcNo,
            Gender = m.Gender,
            Email = m.Email,
            PhoneNo = m.Phone,
            Status = m.Status,
            PhotoURL = m.PhotoURL,
        };

        return View(vm);
    }
    //[Authorize(Roles = "Staff,Admin")]

    [HttpPost]
    public IActionResult EditStaff(EditStaffVM vm, string id)
    {
        var m = db.Staffs.Find(id);
        if (m == null) return RedirectToAction("EditStaff", "Maintenance");

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {

            m.FirstName = vm.FirstName;
            m.LastName = vm.LastName;
            m.Age = vm.Age;
            m.Gender = vm.Gender;
            m.Phone = vm.PhoneNo;

            if (vm.Photo != null)
            {
                hp.DeletePhoto(m.PhotoURL, "photo/users");
                m.PhotoURL = hp.SavePhoto(vm.Photo, "photo/users");
            }

            db.SaveChanges();

            TempData["Info"] = "Profile updated.";
            return RedirectToAction("StaffList", "Maintenance");
        }
        else
        {
            TempData["Info"] = "Profile Not updated.";
            vm.Id = m.Id;
            vm.Email = m.Email;
            vm.Age = m.Age;
            vm.IcNo = m.IcNo;
            vm.Status = m.Status;
            vm.PhotoURL = m.PhotoURL;

        }
        return View(vm);
    }

    [Authorize(Roles = "Staff,Admin")]
    public IActionResult MemberList(string? name, string? sort, string? status, string? dir, int page = 1)
    {
        // (1) Searching ------------------------
        ViewBag.Name = name = name?.Trim() ?? "";

        // Filter member based on the search term
        var searched = db.Members.Where(s => string.IsNullOrEmpty(name) || s.FirstName.Contains(name) || s.LastName.Contains(name));

        // (2) Sorting --------------------------
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        ViewBag.Status = status = status?.Trim() ?? "All";

        if (status != "All")
        {
            searched = searched.Where(s => s.Status == status);
        }

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
    public IActionResult UpdateMemberStatus(string id, [FromBody] UpdateStatusVM model)
    {
        if (string.IsNullOrEmpty(id) || model == null)
        {
            return BadRequest("Invalid request.");
        }

        var member = db.Members.Find(id);
        if (member == null)
        {
            return NotFound("Bus not found.");
        }

        member.Status = model.Status; // Update the status
        db.SaveChanges();

        return Ok(new { message = "Status updated successfully." });
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
    public IActionResult EditMember(string id)
    {
        // Get staff/admin  record based on email (PK)
        var m = db.Members.Find(id);
        if (m == null) return RedirectToAction("EditMember", "Maintenance");

        var vm = new EditMemberVM
        {
            Id = m.Id,
            FirstName = m.FirstName,
            LastName = m.LastName,
            Age = m.Age,
            IcNo = m.IcNo,
            Gender = m.Gender,
            Email = m.Email,
            PhoneNo = m.Phone,
            Status = m.Status,
            PhotoURL = m.PhotoURL,
        };

        return View(vm);
    }
    //[Authorize(Roles = "Staff,Admin")]

    [HttpPost]
    public IActionResult EditMember(EditMemberVM vm, string id)
    {
        var m = db.Members.Find(id);
        if (m == null) return RedirectToAction("EditMember", "Maintenance");

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {

            m.FirstName = vm.FirstName;
            m.LastName = vm.LastName;
            m.Age = vm.Age;
            m.Gender = vm.Gender;
            m.Phone = vm.PhoneNo;
            m.Status = vm.Status;

            if (vm.Photo != null)
            {
                hp.DeletePhoto(m.PhotoURL, "photo/users");
                m.PhotoURL = hp.SavePhoto(vm.Photo, "photo/users");
            }

            db.SaveChanges();

            TempData["Info"] = "Profile updated.";
            return RedirectToAction("MemberList", "Maintenance");
        }
        else
        {
            TempData["Info"] = "Profile Not updated.";
            vm.Id = m.Id;
            vm.Email = m.Email;
            vm.Age = m.Age;
            vm.IcNo = m.IcNo;
            vm.Status = m.Status;
            vm.PhotoURL = m.PhotoURL;

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
                PhotoURL = hp.SavePhoto(vm.Photo, "photo/buses"),
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
    public IActionResult ShowBusList(string? name, string? sort, string? status, string? dir, int page = 1)
    {
        // (1) Searching ------------------------
        ViewBag.Name = name = name?.Trim() ?? "";

        var searched = db.Buses.Where(s => s.Name.Contains(name));

        ViewBag.Status = status = status?.Trim() ?? "All";

        if (status != "All")
        {
            searched = searched.Where(s => s.Status == status);
        }

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

    //[Authorize(Roles = "Staff,Admin")]
    public IActionResult EditBus(string id)
    {
        // Get staff/admin  record based on email (PK)
        var m = db.Buses.Find(id);
        ViewBag.CategoryList = new SelectList(db.CategoryBuses, "Id", "Name");
        if (m == null) return RedirectToAction("EditBus", "Maintenance");

        var vm = new EditBusVM
        {
            Name = m.Name,
            BusPlate = m.BusPlate,
            Capacity = m.Capacity,
            CategoryBusId = m.CategoryBusId,
            PhotoURL = m.PhotoURL,
        };

        return View(vm);
    }
    //[Authorize(Roles = "Staff,Admin")]

    [HttpPost]
    public IActionResult EditBus(EditBusVM vm, string id)
    {
        var m = db.Buses.Find(id);
        if (m == null) return RedirectToAction("EditBus", "Maintenance");

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {

            m.Name = vm.Name;
            m.BusPlate = vm.BusPlate;
            m.Capacity = vm.Capacity;
            m.CategoryBusId = vm.CategoryBusId;

            if (vm.Photo != null)
            {
                hp.DeletePhoto(m.PhotoURL, "photo/users");
                m.PhotoURL = hp.SavePhoto(vm.Photo, "photo/users");
            }

            db.SaveChanges();

            TempData["Info"] = "Bus updated.";
            return RedirectToAction("ShowBusList", "Maintenance");
        }
        else
        {
            TempData["Info"] = "Bus Not updated.";
            vm.Name = m.Name;
            vm.BusPlate = m.BusPlate;
            vm.Capacity = m.Capacity;
            vm.CategoryBusId = m.CategoryBusId;
            vm.PhotoURL = m.PhotoURL;

        }
        return View(vm);
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

    //[Authorize(Roles = "Staff,Admin")]
    public IActionResult EditCategoryBus(string id)
    {
        // Get staff/admin  record based on email (PK)
        var m = db.CategoryBuses.Find(id);
        if (m == null) return RedirectToAction("EditCategoryBus", "Maintenance");

        var vm = new EditCategoryBusVM
        {
            Name = m.Name,
        };

        return View(vm);
    }
    //[Authorize(Roles = "Staff,Admin")]

    [HttpPost]
    public IActionResult EditCategoryBus(EditCategoryBusVM vm, string id)
    {
        var m = db.CategoryBuses.Find(id);
        if (m == null) return RedirectToAction("EditCategoryBus", "Maintenance");


        if (ModelState.IsValid)
        {

            m.Name = vm.Name;

            db.SaveChanges();

            TempData["Info"] = "Bus Category updated.";
            return RedirectToAction();
        }
        else
        {
            TempData["Info"] = "Bus Category Not updated.";
            vm.Name = m.Name;

        }
        return View(vm);
    }

    // Individual Deletion
    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult UpdateBusStatus(string id, [FromBody] BusStatusUpdateVM model)
    {
        if (string.IsNullOrEmpty(id) || model == null)
        {
            return BadRequest("Invalid request.");
        }

        var bus = db.Buses.Find(id);
        if (bus == null)
        {
            return NotFound("Bus not found.");
        }

        bus.Status = model.Status; // Update the status
        db.SaveChanges();

        return Ok(new { message = "Status updated successfully." });
    }





    // POST: Maintenance/DeleteMany
    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult DeleteManyBus(string[] ids)
    {
        if (ids != null && ids.Length > 0)
        {
            // Fetch the buss to update based on the provided IDs
            var busesToUpdate = db.Buses.Where(s => ids.Contains(s.Id)).ToList();

            // Update the Status property to "Inactive"
            foreach (var bus in busesToUpdate)
            {
                bus.Status = "Inactive";
            }

            // Save the changes to the database
            db.SaveChanges();

            TempData["Info"] = $"{busesToUpdate.Count} bus(s) set to inactive.";
        }

        return RedirectToAction("ShowBusList"); // Redirect to the bus list page
    }

    [HttpPost]
    public IActionResult EnableBus(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            TempData["Error"] = "Invalid bus ID.";
            return RedirectToAction("ShowBusList"); // Adjust as necessary for the bus list view
        }

        var bus = db.Buses.Find(id);

        if (bus != null)
        {
            // Set the status to "Active" to enable the bus
            bus.Status = "Active";
            db.SaveChanges();

            TempData["Info"] = "Bus enabled.";
        }
        else
        {
            TempData["Error"] = "Bus not found.";
        }

        return RedirectToAction("ShowBusList"); // Redirect to the bus list page
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

    public IActionResult EditRoute(string id)
    {

        // Get staff/admin  record based on email (PK)
        var m = db.RouteLocations.Find(id);
        if (m == null) return RedirectToAction("EditRoute", "Membership");

        var vm = new EditRouteVM
        {
            Destination = m.Destination,
            Depart = m.Depart,
            Hour = m.Hour,
            Min = m.Min,
        };

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult EditRoute(EditRouteVM vm, string id)
    {
        var m = db.RouteLocations.Find(id);
        if (vm.Depart == vm.Destination)
        {
            ModelState.AddModelError("", "Depart and Destination Should Not Be The Same !");
        }
        if (vm.Hour <= 0 || vm.Hour > 12)
        {
            ModelState.AddModelError("EstimatedTimeHour", "Hour should not more than 12 h or less than 1");
        }
        if (vm.Min <= 0 || vm.Min > 60)
        {
            ModelState.AddModelError("EstimatedTimeHour", "Hour should not more than 60 m or less than 1");
        }
        var isRouteInUse = db.Schedules.Any(s => s.RouteLocationId == id);
        if (isRouteInUse)
        {
            ModelState.AddModelError("", "This route cannot be edited because it is associated with existing schedules.");
        }

        if (ModelState.IsValid)
        {
            {
                m.Destination = vm.Destination;
                m.Depart = vm.Depart;
                m.Hour = vm.Hour;
                m.Min = vm.Min;
            };
            db.SaveChanges();
            TempData["Info"] = "Route updated.";
            return RedirectToAction("ShowRouteList", "Maintenance");
        }

        return View(vm);
    }

    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult DeleteRoute(string routeId)
    {
        // Check if the rank exists
        var route = db.RouteLocations.FirstOrDefault(r => r.Id == routeId);
        if (route == null)
        {
            TempData["Error"] = "Route not found.";
            return Json(new { success = false, message = "Route not found." });
        }

        // Check if any member or staff is using this rank
        bool isRouteInUse = db.Schedules.Any(s => s.RouteLocationId == routeId);
        if (isRouteInUse)
        {
            ModelState.AddModelError("", "This route cannot be deleted because it is associated with existing schedules.");
            return Json(new { success = false, message = "This route is currently in use by one or more schedules." });
        }

        // If not in use, delete the rank
        db.RouteLocations.Remove(route);
        db.SaveChanges();

        TempData["Info"] = "Route deleted successfully.";
        return Json(new { success = true, message = "Route deleted successfully." });
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
    //Get
    [Authorize(Roles ="Admin")]
    public IActionResult ITSupport()
    {
        return View();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public IActionResult ITSupport(ITSupportVM vm)
    {
        if (ModelState.IsValid)
        {
            TempData["Info"] = $"Your Ticket Has Submitted.Priority Level : {vm.Priority}";
            return RedirectToAction("Dashboard", "Maintenance");
        }

        return View(vm);
    }

}

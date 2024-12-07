using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;
using System.Net.Mail;
using System.Reflection;

namespace Demo.Controllers;

public class AccountController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;

    public AccountController(DB db, IWebHostEnvironment en, Helper hp)
    {

        this.db = db;
        this.en = en;
        this.hp = hp;
    }

    // GET: Account/Login
    public IActionResult Login()
    {
        if (User.Identity.IsAuthenticated)
        {
            TempData["Info"] = "You are already logged in.";
            return RedirectToAction("Index", "Home"); // Redirect to a default page like Home
        }
        return View();
    }

    //// POST: Account/Login
    [HttpPost]
    public IActionResult Login(LoginVM vm, string? returnURL)
    {
        const int maxAttempts = 5;
        const int lockoutDurationMinutes = 1;

        // Retrieve login attempt details from TempData
        int failedAttempts = TempData.ContainsKey("FailedAttempts") ? (int)TempData["FailedAttempts"] : 0;
        DateTime? lockoutEnd = TempData.ContainsKey("LockoutEnd") ? (DateTime?)TempData["LockoutEnd"] : null;

        // Check if the account is locked
        if (lockoutEnd != null && DateTime.Now < lockoutEnd)
        {
            TempData["Info"] = $"Account locked. Try again after {lockoutEnd.Value.ToString("HH:mm:ss")}.";
            return View(vm);
        }

        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);

        if (u == null || !hp.VerifyPassword(u.Hash, vm.Password))
        {
            failedAttempts++;

            if (failedAttempts >= maxAttempts)
            {
                lockoutEnd = DateTime.Now.AddMinutes(lockoutDurationMinutes);
                TempData["LockoutEnd"] = lockoutEnd;
                TempData["FailedAttempts"] = 0; // Reset attempts after lockout
                TempData["Info"] = $"Account locked for {lockoutDurationMinutes} minute(s) due to too many failed attempts.";
            }
            else
            {
                TempData["FailedAttempts"] = failedAttempts;
                TempData["Info"] = $"Login failed. {maxAttempts - failedAttempts} attempt(s) remaining.";
            }

            return View(vm);
        }

        // Reset attempts on successful login
        TempData["FailedAttempts"] = 0;
        TempData["LockoutEnd"] = null;

        // Handle user status and email verification
        if (u.EmailVerified == 1 && u.Status == "Active")
        {
            TempData["Info"] = "Login successfully.";
            hp.SignIn(u.Email, u.Role, vm.RememberMe);
        }
        else if (u.EmailVerified == 0 && u.Status == "Active")
        {
            TempData["Info"] = "Your account has not been activated.";
            ViewBag.ResendEmailLink = Url.Action("ResendActivation", "Account", new { email = u.Email }, "https");
            return View(vm);
        }
        else if (u.Status == "Block")
        {
            TempData["Info"] = "Your account is currently blocked. Please contact support.";
            return View(vm);
        }
        else
        {
            TempData["Info"] = "Your account access is restricted. Please contact support.";
            return View(vm);
        }


        // Handle return URL
        if (string.IsNullOrEmpty(returnURL))
        {
            return RedirectToAction("Index", "Home");
        }

        return Redirect(returnURL);
    }

    // GET: Account/Logout
    public IActionResult Logout(string? returnURL)
    {
        TempData["Info"] = "Logout successfully.";

        // Sign out
        hp.SignOut();

        return RedirectToAction("Index", "Home");
    }

    // GET: Account/AccessDenied
    public IActionResult AccessDenied(string? returnURL)
    {
        return View();
    }



    // ------------------------------------------------------------------------
    // Others
    // ------------------------------------------------------------------------

   // GET: Account/CheckEmail
    public bool CheckEmail(string email)
    {
        return !db.Users.Any(u => u.Email == email);
    }

    // GET: Account/Register
    public IActionResult Register()
    {
        if (User.Identity.IsAuthenticated)
        {
            TempData["Info"] = "You are already logged in.";
            return RedirectToAction("Index", "Home"); // Redirect to a default page like Home
        }
        return View();
    }

    // POST: Account/Register
    [HttpPost]
    public IActionResult Register(RegisterVM vm)
    {
        if (ModelState.IsValid("Email") && db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
        }

        if (ModelState.IsValid("Photo"))
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
            string nextIdForMembers = GetNextPrefixId("Members"); // Generates "M0001" if empty

            // Insert the new member
            var newMember = new Member
            {
                Id = nextIdForMembers,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Age = vm.Age,
                IcNo = vm.IcNo,
                Gender = vm.Gender,
                Position = vm.Position,
                Email = vm.Email,
                Phone = vm.PhoneNo,
                Hash = hp.HashPassword(vm.Password),
                Country = vm.Country,
                PhotoURL = hp.SavePhoto(vm.Photo, "photo/users"),
                Status="Active",
                EmailVerified=0,
            };
            db.Members.Add(newMember);
            db.SaveChanges();

            // Generate and save token
            string randomTokenId = GenerateRandomToken();
            var token = new Token
            {
                Id = randomTokenId,
                UserId = nextIdForMembers,
                Expired = DateTime.Now.AddMinutes(5) // Token valid for 5 minutes
            };
            db.Tokens.Add(token);
            db.SaveChanges();

            // Send activation email
            SendActivationLink(newMember, randomTokenId);

            TempData["Info"] = "Registered successfully. Please verify your email within 5 minutes.";
            return RedirectToAction("Login");
        }

        return View(vm);
    }


    private void SendActivationLink(User u, string tokenId,bool resend=false)
    {
        var fullName = $"{u.FirstName} {u.LastName}";
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(u.Email, fullName));
        if (!resend)
        {
            mail.Subject = "Account Activation - Secure Your Access";
        }
        else
        {
            mail.Subject = "Resend Account Activation - Secure Your Access";
        }
        mail.IsBodyHtml = true;

        // Generate the activation URL
        var activationUrl = Url.Action("Activate", "Account", new { token = tokenId }, "https");

        // Email Body
        mail.Body = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
            <div style='text-align: center; margin-bottom: 20px;'>
                <img src='cid:logo' alt='Company Logo' style='width: 100px; height: auto;'>
            </div>
            <h2 style='color: #4CAF50; text-align: center;'>Welcome to Our Platform, {fullName}!</h2>
            <p style='font-size: 16px; line-height: 1.6; color: #333;'>
                Thank you for registering with us. To activate your account and access all features, please verify your email by clicking the button below. 
                This link will expire in <strong>5 minutes</strong>.
            </p>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{activationUrl}' style='background-color: #4CAF50; color: white; text-decoration: none; padding: 12px 24px; border-radius: 5px; font-size: 16px;'>Activate My Account</a>
            </div>
            <p style='font-size: 14px; color: #666;'>
                If you did not register an account, please ignore this email or contact our support team for assistance.
            </p>
            <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>
            <p style='font-size: 12px; color: #999; text-align: center;'>
                This email was sent from a no-reply address. For help, contact our support team at support@example.com.<br>
                &copy; 2024 Your Company. All rights reserved.
            </p>
        </div>";

        //  Attach logo
        var logoPath = Path.Combine(en.WebRootPath, "photo/images", "logo_JMBus.png");
         mail.Attachments.Add(new Attachment(logoPath) { ContentId = "logo" });

        // Send the email
        hp.SendEmail(mail);
    }


    private static string GenerateRandomToken()
    {
        return Guid.NewGuid().ToString();
    }

    public string GetNextPrefixId(string tableName)
    {
        string currentMaxId = "";

        if (tableName == "Members")
        {
            currentMaxId = db.Members.Max(m => m.Id) ?? "M0000"; // Default if table is empty
        }
        else if (tableName == "Staffs")
        {
            currentMaxId = db.Users.Max(u => u.Id) ?? "U0000"; // Adjust default prefix if needed
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

    public IActionResult Activate(string token)
    {
        // Retrieve token record and include associated user
        var tokenRecord = db.Tokens.Include(t => t.User).FirstOrDefault(t => t.Id == token);

        if (tokenRecord == null)
        {
            TempData["Info"] = "Invalid activation link.";
            return RedirectToAction("Login");
        }

        // Check if the token has expired
        if (DateTime.Now > tokenRecord.Expired)
        {
            // Remove expired token
            db.Tokens.Remove(tokenRecord);
            db.SaveChanges();

            TempData["Info"] = "Activation link has expired. Please request a new one.";
            return RedirectToAction("Login");
        }

        // Get the user associated with the token
        var user = tokenRecord.User;
        if (user != null && user.Status != "Block" && user.EmailVerified == 0)
        {
            // Activate the user account
            user.EmailVerified = 1;

            // Remove the token as it is no longer needed
            db.Tokens.Remove(tokenRecord);

            // Save all changes in one go
            db.SaveChanges();

            TempData["Info"] = "Account activated successfully! You can now log in.";
        }
        else
        {
            TempData["Info"] = "Account activation failed. Please check the status of your account or contact support.";
        }

        return RedirectToAction("Login");
    }


    public IActionResult ResendActivation(string? email)
    {
        // Check if the email is provided
        if (string.IsNullOrEmpty(email))
        {
            TempData["Info"] = "Email address is required.";
            return RedirectToAction("Login");
        }

        // Find user in the database
        var user = db.Users.FirstOrDefault(u => u.Email == email);
        if (user == null || user.EmailVerified != 0 || user.Status != "Active")
        {
            TempData["Info"] = "Invalid request to resend activation. Please check the account status.";
            return RedirectToAction("Login");
        }

        // Check for existing token and delete it if valid
        var existingToken = db.Tokens.FirstOrDefault(t => t.UserId == user.Id);
        if (existingToken != null)
        {
            db.Tokens.Remove(existingToken);
            db.SaveChanges();
        }

        // Generate and save a new token
        string randomTokenId = GenerateRandomToken(); // Assumes this method exists and generates a unique token
        var token = new Token
        {
            Id = randomTokenId,
            UserId = user.Id,
            Expired = DateTime.Now.AddMinutes(5) // Token valid for 5 minutes
        };
        db.Tokens.Add(token);
        db.SaveChanges();

        // Send the activation email
        SendActivationLink(user, randomTokenId, true); // Assumes this method exists and handles email sending

        // Inform the user of success
        TempData["Info"] = "Activation email has been resent successfully!";
        return RedirectToAction("Login");
    }




    // GET: Account/UpdatePassword
    // TODO
    //[Authorize]
    //public IActionResult UpdatePassword()
    //{
    //    return View();
    //}

    //// POST: Account/UpdatePassword
    //// TODO
    //[Authorize]
    //[HttpPost]
    //public IActionResult UpdatePassword(UpdatePasswordVM vm)
    //{
    //    // Get user (admin or member) record based on email (PK)
    //    // TODO
    //    var u = db.Users.Find(User.Identity!.Name);
    //    if (u == null) return RedirectToAction("Index", "Home");

    //    // If current password not matched
    //    // TODO
    //    if (!hp.VerifyPassword(u.Hash, vm.Current))
    //    {
    //        ModelState.AddModelError("Current", "Current Password not matched.");
    //    }

    //    if (ModelState.IsValid)
    //    {
    //        // Update user password (hash)
    //        u.Hash = hp.HashPassword(vm.New);
    //        db.SaveChanges();

    //        TempData["Info"] = "Password updated.";
    //        return RedirectToAction();
    //    }

    //    return View();
    //}

   // GET: Account/UpdateProfile
   [Authorize(Roles = "Member")]
    public IActionResult UpdateProfile()
    {
        // Get member record based on email (PK)
        // TODO
        var m = db.Users.FirstOrDefault(u => u.Email == User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        var vm = new UpdateProfileVM
        {
            Id = m.Id,
            FirstName =m.FirstName,
            LastName = m.LastName,
            Age = m.Age,
            IcNo = m.IcNo,
            Gender = m.Gender,
            Position = m.Position,
            Email = m.Email,
            PhoneNo = m.Phone,
            Country = m.Country,
            Status = m.Status,
            PhotoURL = m.PhotoURL,
        };

        return View(vm);
    }

    //// POST: Account/UpdateProfile
    //// TODO
    [Authorize(Roles = "Member")]
    [HttpPost]
    public IActionResult UpdateProfile(UpdateProfileVM vm)
    {
        var m = db.Users.FirstOrDefault(u => u.Email == User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        if (vm.Photo != null)
        {
            var err = hp.ValidatePhoto(vm.Photo);
            if (err != "") ModelState.AddModelError("Photo", err);
        }

        if (ModelState.IsValid)
        {
        
            m.FirstName = vm.FirstName;
            m.LastName = vm.LastName;
            m.Gender = vm.Gender;
            m.Position = vm.Position;
            m.Phone = vm.PhoneNo;
            m.Country = vm.Country;
 

            if (vm.Photo != null)
            {
                hp.DeletePhoto(m.PhotoURL, "photo/users");
                m.PhotoURL = hp.SavePhoto(vm.Photo, "photo/users");
            }

            db.SaveChanges();

            TempData["Info"] = "Profile updated.";
            return RedirectToAction();
        }
        else
        {
            TempData["Info"] = "Profile Not updated.";
            // TODO
            vm.Id = m.Id;
            vm.Email = m.Email;
            vm.Age = m.Age;
            vm.IcNo = m.IcNo;
            vm.Status = m.Status;
            vm.PhotoURL = m.PhotoURL;
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"ModelState error: {error.ErrorMessage}");
            }
        }
  


        return View(vm);
    }

    //// GET: Account/ResetPassword
    //public IActionResult ResetPassword()
    //{
    //    return View();
    //}

    //// POST: Account/ResetPassword
    //[HttpPost]
    //public IActionResult ResetPassword(ResetPasswordVM vm)
    //{
    //    var u = db.Users.Find(vm.Email);

    //    if (u == null)
    //    {
    //        ModelState.AddModelError("Email", "Email not found.");
    //    }

    //    if (ModelState.IsValid)
    //    {
    //        // Generate random password
    //        // TODO
    //        string password = hp.RandomPassword();

    //        // Update user (admin or member) record
    //        // TODO
    //        u!.Hash = hp.HashPassword(password);
    //        db.SaveChanges();
    //        // TODO: Send reset password email

    //        TempData["Info"] = $"Password reset to <b>{password}</b>.";
    //        return RedirectToAction();
    //    }

    //    return View();
    //}
}

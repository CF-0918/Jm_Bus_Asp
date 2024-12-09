using Demo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;

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
    [Authorize]
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

        if ((ModelState.IsValid("EmailVerifiedHiddenField") && vm.EmailVerifiedHiddenField!="EmailVerified"))
        {
            ModelState.AddModelError("Email", "Email Haven't Verified");
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
                Status = "Active",
                EmailVerified = 0,
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


    // Static dictionary to store email-OTP pairs
    private static Dictionary<string, string> emailOtpDict = new Dictionary<string, string>();

    [HttpPost]
    public JsonResult SendEmailVerifiedNumber(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Json(new { success = false, message = "Email is required." });
        }

        try
        {
            var otp = GenerateOtp();
            emailOtpDict[email] = otp;  // Store OTP in the dictionary

            var mail = new MailMessage();
            mail.To.Add(new MailAddress(email, "user"));
            mail.Subject = "OTP Verified Code - Become JMBus Member";
            mail.IsBodyHtml = true;
            mail.Body = $@"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>OTP Verification</title>
                <style>
                    .otp_container {{ border: 1px solid #ccc; text-align: center; border-radius: 5px; padding: 20px; max-width: 400px; margin: 0 auto; font-family: Arial, sans-serif; }}
                    .otp_title {{ font-weight: bold; background-color: blue; color: #fff; padding: 10px; border-radius: 5px 5px 0 0; }}
                    .otp_number {{ font-size: 24px; font-weight: bold; margin: 20px auto; padding: 10px; border: 3px solid black; width: 150px; border-radius: 5px; }}
                    .otp_message {{ font-size: 14px; margin: 15px 0; color: #333; }}
                </style>
            </head>
            <body>
                <div class=""otp_container"">
                    <div class=""otp_title"">
                        <h2>OTP Verification Code</h2>
                    </div>
                    <div class=""otp_message"">
                        <p>Dear Customer,</p>
                        <p>Please find your OTP code below. You can copy and paste it during the account registration process.</p>
                    </div>
                    <div class=""otp_number"">{otp}</div>
                </div>
            </body>
            </html>";

            hp.SendEmail(mail); // Send email using your configured SMTP helper

            return Json(new { success = true, message = "OTP sent successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error sending email: {ex.Message}" });
        }
    }

    [HttpPost]
    public JsonResult ValidateCode(string email, string code)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
        {
            return Json(new { success = false, message = "Email and code are required." });
        }

        if (emailOtpDict.ContainsKey(email))
        {
            var storedOtp = emailOtpDict[email];  // Retrieve OTP from dictionary
            if (storedOtp == code)
            {
                emailOtpDict.Remove(email);  // Remove OTP from dictionary after validation
                return Json(new { success = true, message = "Code verified successfully." });
            }
            else
            {
                return Json(new { success = false, message = "Invalid code." });
            }
        }
        else
        {
            return Json(new { success = false, message = "OTP not found or expired." });
        }
    }

    private static string GenerateOtp()
    {
        const int length = 6;
        var random = new Random();
        var otp = new char[length];

        for (int i = 0; i < length; i++)
        {
            otp[i] = (char)('0' + random.Next(0, 10)); // Ensure digits only
        }

        return new string(otp);
    }


    private void SendActivationLink(User u, string tokenId, bool resend = false)
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




    //GET: Account/UpdatePassword
    [Authorize]
    public IActionResult UpdatePassword()
    {
        return View();
    }


    //// POST: Account/UpdatePassword
    //// TODO
    [Authorize]
    [HttpPost]
    public IActionResult UpdatePassword(UpdatePasswordVM vm)
    {
        // Get user (admin or member) record based on email (PK)

        var u = db.Users.FirstOrDefault(u => u.Email == User.Identity!.Name);

        if (u == null) return RedirectToAction("Index", "Home");

        // If current password not matched
        // TODO
        if (!hp.VerifyPassword(u.Hash, vm.Current))
        {
            ModelState.AddModelError("Current", "Current Password not matched.");
        }

        if (ModelState.IsValid)
        {
            // Update user password (hash)
            u.Hash = hp.HashPassword(vm.New);
            db.SaveChanges();

            TempData["Info"] = "Password updated.";
            return RedirectToAction();
        }

        return View();
    }

    // GET: Account/UpdateProfile
    [Authorize(Roles = "Member")]
    public IActionResult UpdateProfile()
    {
        // Get member record based on email (PK)
        // TODO
        var m = db.Members.FirstOrDefault(u => u.Email == User.Identity!.Name);
        if (m == null) return RedirectToAction("Index", "Home");

        var vm = new UpdateProfileVM
        {
            Id = m.Id,
            FirstName = m.FirstName,
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
        var m = db.Members.FirstOrDefault(u => u.Email == User.Identity!.Name);
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

        }



        return View(vm);
    }

    //// GET: Account/ResetPassword
    public IActionResult ResetPassword()
    {
        return View();
    }

    //// POST : Account/ResetPassword
    [HttpPost]
    public  IActionResult ResetPassword(ResetPasswordVM vm)
    {
        Console.WriteLine("GoogleCaptchaToken: " + vm.GoogleCaptchaToken); // Debug token value
        System.Diagnostics.Debug.WriteLine("GoogleCaptchaToken: " + vm.GoogleCaptchaToken);


        if (string.IsNullOrWhiteSpace(vm.GoogleCaptchaToken))
        {
            ModelState.AddModelError("GoogleCaptchaToken", "Google CAPTCHA token is required.");
            return View(vm);
        }

        bool success =  hp.VerifyReCaptchaV2(vm.GoogleCaptchaToken);

        if (!success)
        {
            ModelState.AddModelError("GoogleCaptchaToken", "Google CAPTCHA verification failed.");
            return View(vm);
        }

        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);
        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
            return View(vm);
        }

        if (ModelState.IsValid)
        {
            string password = hp.RandomPassword();
            u!.Hash = hp.HashPassword(password);
            db.SaveChanges();

            var fullName = $"{u.FirstName} {u.LastName}";
            var mail = new MailMessage();
            mail.To.Add(new MailAddress(u.Email, fullName));
            mail.Subject = "Reset Password - Secure Your Access";
           
            mail.IsBodyHtml = true;

            // Email Body
            mail.Body = $@"
                  <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                    <div style='text-align: center; margin-bottom: 20px;'>
                        <img src='cid:logo' alt='Company Logo' style='width: 100px; height: auto;'>
                    </div>
                    <h2 style='color: #4CAF50; text-align: center;'>Your Password Has Been Reset</h2>
                    <p style='font-size: 16px; line-height: 1.6; color: #333;'>
                        Hi <strong>{fullName}</strong>,
                    </p>
                    <p style='font-size: 16px; line-height: 1.6; color: #333;'>
                        We wanted to let you know that your password has been successfully reset. Your new password is: <strong>{password}</strong>
                    </p>
                    <p style='font-size: 16px; line-height: 1.6; color: #333;'>
                        Please keep this password safe and use it to log in to your account. If you did not request this change or believe it was done in error, please contact our support team immediately.
                    </p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <p style='font-size: 14px; color: #666;'>If you need any assistance, feel free to reach out to our support team.</p>
                    </div>
                    <p style='font-size: 14px; color: #666;'>
                        If you did not request a password reset, please ignore this email or contact our support team for assistance.
                    </p>
                    <hr style='border: none; border-top: 1px solid #ddd; margin: 20px 0;'>
                    <p style='font-size: 12px; color: #999; text-align: center;'>
                        This email was sent from a no-reply address. For help, contact our support team at support@example.com.<br>
                        &copy; {DateTime.Now} JMBUS. All rights reserved.
                    </p>
                </div>
                ";
            //  Attach logo
            var logoPath = Path.Combine(en.WebRootPath, "photo/images", "logo_JMBus.png");
            mail.Attachments.Add(new Attachment(logoPath) { ContentId = "logo" });

            // Send the email
            hp.SendEmail(mail);
            TempData["Info"] ="Password Has Been Reset,Check Email";
            return RedirectToAction("Login");
        }

        return View(vm);
    }


}

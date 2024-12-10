using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Demo.Models;

// View Models ----------------------------------------------------------------

#nullable disable warnings

public class LoginVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [Display(Name = "Login Password")]
    public string Password { get; set; }

    public bool RememberMe { get; set; }
}
public class RegisterVM
{

    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }

    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }

    [Range(13, 100, ErrorMessage = "Age must be between 13 and 100.")]
    public int Age { get; set; }

    [RegularExpression(@"\d{6}-\d{2}-\d{4}", ErrorMessage = "Invalid Identification Card No format.")]
    [Display(Name = "Identification Card No")]
    public string IcNo { get; set; }

    public char Gender { get; set; }

    [StringLength(50)]
    [Display(Name = "Job")]
    public string Position { get; set; }

    [EmailAddress]
    [StringLength(100)]
    [Remote("CheckEmail", "Account", ErrorMessage = "Duplicated email.")]
    public string Email { get; set; }
    [MaxLength(6)]
    [Display(Name = "Verfication Code")]
    public string VerificationNumberEmail{  get; set; }

    [Required(ErrorMessage = "Email hasn't been verified.")]
    public string EmailVerifiedHiddenField { get; set; }

    [StringLength(15)]
    [Display(Name = "Phone Number")]
    public string PhoneNo { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string Password { get; set; }


    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; }

    [StringLength(50)]
    public string Country { get; set; } = "Malaysian";

    public IFormFile Photo { get; set; }
}

public class UpdatePasswordVM
{
    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string Current { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string New { get; set; }

    [StringLength(100, MinimumLength = 5)]
    [Compare("New")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string Confirm { get; set; }
}

public class UpdateProfileVM
{
    public string? Id { get; set; }

    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }

    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }

    public int? Age { get; set; }

    public string? IcNo { get; set; }

    public char Gender { get; set; }

    [StringLength(50)]
    [Display(Name = "Job")]
    public string Position { get; set; }

    public string? Email { get; set; }


    [StringLength(15)]
    [Display(Name = "Phone Number")]
    public string PhoneNo { get; set; }

    public string Country { get; set; } 

    public string? Status {  get; set; }

    public string? PhotoURL {  get; set; }
    public IFormFile? Photo { get; set; }
}

public class ResetPasswordVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }
    public string GoogleCaptchaToken { get; set; }
}

//Jayden Part
public class RentServiceFormVM
{
    public string RentId;
}

//Eason Part 
public class CreateStaffVM
{
    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }

    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }

    [Range(13, 100, ErrorMessage = "Age must be between 13 and 100.")]
    public int Age { get; set; }

    [RegularExpression(@"\d{6}-\d{2}-\d{4}", ErrorMessage = "Invalid Identification Card No format.")]
    [Display(Name = "Identification Card No")]
    public string IcNo { get; set; }

    public char Gender { get; set; }

    [EmailAddress]
    [StringLength(100)]
    [Remote("CheckEmail", "Account", ErrorMessage = "Duplicated email.")]
    public string Email { get; set; }

    [StringLength(15)]
    [Display(Name = "Phone Number")]
    public string PhoneNo { get; set; }


    [StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string Password { get; set; }


    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; }

    public IFormFile Photo { get; set; }
}

public class StaffAdminProfileVM
{
    public string? Id { get; set; }

    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }

    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }

    public int? Age { get; set; }

    public string? IcNo { get; set; }

    public char Gender { get; set; }

    public string? Email { get; set; }


    [StringLength(15)]
    [Display(Name = "Phone Number")]
    public string PhoneNo { get; set; }


    public string? Status { get; set; }

    public string? PhotoURL { get; set; }
    public IFormFile? Photo { get; set; }
}
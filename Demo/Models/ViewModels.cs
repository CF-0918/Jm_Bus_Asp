using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net.Sockets;

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

    public bool? Subscribe { get; set; }

    public string? PhotoURL {  get; set; }
    public IFormFile? Photo { get; set; }
}

public class VoucherVM
{
    [StringLength(50)]
    public string Name { get; set; }

    public int PointNeeded { get; set; }

    [Display(Name = "Value (RM)")]
    public int CashDiscount { get; set; }
    [Remote("CheckDateIsTodayOrGreaterThan", "Membership", ErrorMessage = "Start Date Should Not Be Past")]
    public DateOnly StartDate { get; set; }

    [Remote(action: "CheckRangeDate", controller: "Membership", AdditionalFields = nameof(StartDate) ,ErrorMessage = "End Date Should Be Greater Than Start Date.")]
    public DateOnly EndDate { get; set; }

    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
    public int Qty { get; set; }

    [StringLength(100)]
    public string Description { get; set; }
}

public class EditVoucherVM
{
    [StringLength(50)]
    public string Name { get; set; }

    public int PointNeeded { get; set; }

    [Display(Name = "Value (RM)")]
    public int CashDiscount { get; set; }
    [Remote("CheckDateIsTodayOrGreaterThan", "Membership", ErrorMessage = "Start Date Should Not Be Past")]
    public DateOnly StartDate { get; set; }

    [Remote(action: "CheckRangeDate", controller: "Membership", AdditionalFields = nameof(StartDate), ErrorMessage = "End Date Should Be Greater Than Start Date.")]
    public DateOnly EndDate { get; set; }

    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
    public int Qty { get; set; }

    [StringLength(100)]
    public string Description { get; set; }
}

public class VoucherStatusUpdateVM
{
    public string Status { get; set; }
}

public class VoucherRedeem
{
    public string Id { get; set; }
}
public class RankVM
{
    [StringLength(50)]
    [Display(Name = "Rank Name")]
    [Remote("CheckRankName", "Membership", ErrorMessage = "Duplicated Rank Name.")]
    public string Name { get; set; }

    [StringLength(100)]
    public string Description { get; set; }

    [Display(Name = "Min Spend")]
    [Range(0, int.MaxValue, ErrorMessage = "Min Spend must be a positive value.")]
    public int MinSpend { get; set; }

    [Range(0, 100, ErrorMessage = "Discounts Percentage must be between 0 and 100.")]
    [Display(Name = "Discounts Percentage (%)")]
    public int Discounts { get; set; }
}

public class EditRankVM
{
    public string? Id { get; set; }
    [StringLength(50)]

    public string Name { get; set; }

    [StringLength(100)]
    public string Description { get; set; }

    [Display(Name = "Min Spend")]
    [Range(0, int.MaxValue, ErrorMessage = "Min Spend must be a positive value.")]
    public int MinSpend { get; set; }

    [Range(0, 100, ErrorMessage = "Discounts Percentage must be between 0 and 100.")]
    [Display(Name = "Discounts Percentage (%)")]
    public int Discounts { get; set; }
}

public class VoucherViewModel
{
    public string VoucherId { get; set; }
    public string VoucherName { get; set; }
    public string Description { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal CashDiscount { get; set; }
    public int PointNeeded { get; set; }
    public string Status { get; set; }
    public int Amount { get; set; } 
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

public class StaffDetailsVM
{
    public string? Id { get; set; }
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

    public string? Status { get; set; }

    public string? PhotoURL { get; set; }
    public IFormFile Photo { get; set; }
}

public class EditStaffVM
{
    public string? Id { get; set; }

    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }

    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }

    public int Age { get; set; }

    [StringLength(12)]
    [Display(Name = "IC Number")]
    public string? IcNo { get; set; }

    public char Gender { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(15)]
    [Display(Name = "Phone Number")]
    public string PhoneNo { get; set; }

    public string? Status { get; set; }

    [Url]
    public string? PhotoURL { get; set; }

    public IFormFile? Photo { get; set; }
}

// ViewModel for the status update
public class UpdateStatusVM
{
    public string Status { get; set; }
}
public class MemberDetailsVM
{
    public string? Id { get; set; }
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

    public string? Status { get; set; }

    public string? PhotoURL { get; set; }
    public IFormFile Photo { get; set; }
}

public class EditMemberVM
{
    public string? Id { get; set; }

    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }

    [StringLength(50, MinimumLength = 2)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }

    public int Age { get; set; }

    [StringLength(12)]
    [Display(Name = "IC Number")]
    public string? IcNo { get; set; }

    public char Gender { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(15)]
    [Display(Name = "Phone Number")]
    public string PhoneNo { get; set; }

    public string Status { get; set; }

    [Url]
    public string? PhotoURL { get; set; }

    public IFormFile? Photo { get; set; }
}

public class MemberStatusUpdateModel
{
    public string Status { get; set; }
}

public class AddCategoryBus
{
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(50, ErrorMessage = "Category name cannot exceed 50 characters.")]
    public string Name { get; set; }
}

public class EditCategoryBusVM
{
    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(50, ErrorMessage = "Category name cannot exceed 50 characters.")]
    public string Name { get; set; }
}


public class AddBusVM
{
    public string Name { get; set; }

    [Required(ErrorMessage = "Bus plate is required.")]
    [RegularExpression(@"^[A-Z]{3}[0-9]{4}$", ErrorMessage = "Bus plate must start with three uppercase letters followed by four digits (e.g., VHA5309).")]

    public string BusPlate { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [RegularExpression(@"^(Active|Inactive)$", ErrorMessage = "Status must be either 'Active' or 'Inactive'.")]
    public string Status { get; set; }

    [Required(ErrorMessage = "Capacity is required.")]
    [Range(10, 50, ErrorMessage = "Capacity must be between 10 and 50.")]
    public int Capacity { get; set; }

    public IFormFile Photo { get; set; }

    [Required(ErrorMessage = "Category Bus ID is required.")]
    [RegularExpression(@"^[A-Z0-9\-]{1,20}$", ErrorMessage = "Category Bus ID must be alphanumeric and up to 20 characters.")]
    [Display(Name="Category")]
    public string CategoryBusId { get; set; }
}

public class EditBusVM
{
    public string? Id { get; set; }
    public string Name { get; set; }

    [Required(ErrorMessage = "Bus plate is required.")]
    [RegularExpression(@"^[A-Z]{3}[0-9]{4}$", ErrorMessage = "Bus plate must start with three uppercase letters followed by four digits (e.g., VHA5309).")]

    public string BusPlate { get; set; }

    public string? Status { get; set; }

    [Required(ErrorMessage = "Capacity is required.")]
    [Range(10, 50, ErrorMessage = "Capacity must be between 10 and 50.")]
    public int Capacity { get; set; }

    [Url]
    public string? PhotoURL { get; set; }
    public IFormFile? Photo { get; set; }

    [Required(ErrorMessage = "Category Bus ID is required.")]
    [RegularExpression(@"^[A-Z0-9\-]{1,20}$", ErrorMessage = "Category Bus ID must be alphanumeric and up to 20 characters.")]
    [Display(Name = "Category")]
    public string CategoryBusId { get; set; }
}

public class BusStatusUpdateVM
{
    public string Status { get; set; }
}

public class RouteVM
{
    public string Depart { get; set; }

    [Remote("CheckDepartAndDestination", "Maintenance", ErrorMessage = "Destination should not be the same as Depart")]
    public string Destination { get; set; }

    public int EstimatedTimeHour { get; set; }
    public int EstimatedTimeMin { get; set; }
}


public class ScheduleVM
{
    [Display(Name ="Bus")]
   public string BusId { get; set; }
    [Display(Name = "Route")]
    public string RouteId { get; set; }
    [Display(Name = "Depart Date")]
    public DateOnly DepartDate {  get; set; }
    [Display(Name = "Depart Time")]
    public TimeOnly DepartTime { get; set; }

    public string Status { get; set; }
    public int Price {  get; set; }
    [Display(Name = "Discount Price")]
    public int DiscountPrice { get; set; }
    public string Remark { get; set; }
    [Display(Name = "Send Email To Subscriber")]
    public string? SubscribeEmail {  get; set; }
}

public class ScheduleDetailsVM
{
    public string BusId { get; set; }
    public string BusName { get; set; }
    public string BusCapacity { get; set; }
    public string BusPlate { get; set; }
    public string CategoryBusName { get; set; }
    public string PhotoURL { get; set; }

    public string ScheduleId { get; set; }
    public DateOnly DepartDate { get; set; }
    public TimeOnly DepartTime { get; set; }
    public int Price { get; set; }
    public int DiscountPrice { get; set; }

    public string Depart { get; set; }
    public string Destination { get; set; }
    public int Hour { get; set; }
    public int Min { get; set; }
}

public class VoucherUserDetailsVM
{
    public string VoucherId {  get; set; }
    public string VoucherName {  get; set; }
    public int CashDiscount { get; set; }
    public int Qty { get; set; }

}

public class SubscribeNewsLetterVM
{
    public string SubscribeNewsLetter { get; set; }
}
public class PaymentVM
{
    public string CardType {  get; set; }
    public string CardHolderName {  get; set; }
    public string CardNumber { get; set; }
    public string ExpirationDate {  get; set; }
    public string Cvv {  get; set; }
    [Display(Name ="Terms Checking")]
    public string TermsCheck {  get; set; }

    public string? VoucherId { get; set; }

}
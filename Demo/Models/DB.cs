using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Demo.Models;

#nullable disable warnings

public class DB : DbContext
{
    public DB(DbContextOptions options) : base(options) { }

    // DB Sets
    public DbSet<User> Users { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Staff> Staffs { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Rank> Ranks { get; set; }
    public DbSet<Voucher> Vouchers { get; set; }
    public DbSet<MemberVoucher> MemberVouchers { get; set; }

    public DbSet<Rent> Rents { get; set; }
    public DbSet<Token> Tokens { get; set; }
    public DbSet<CategoryBus> CategoryBuses { get; set; }
    public DbSet<Bus> Buses { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<RouteLocation> RouteLocations { get; set; }
    public DbSet<Subscriptions> Subscriptionses { get; set; }

    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingSeat> BookingSeats { get; set; }

    public DbSet<Review> Reviews { get; set; }

}

// Entity Classes -------------------------------------------------------------

public class User
{
    [Key, MaxLength(10)]
    public string Id { get; set; }
    [MaxLength(50)]
    public string FirstName { get; set; }
    [MaxLength(50)]
    public string LastName { get; set; }
    [MaxLength (2)]
    public int Age { get; set; }
    [MaxLength (15)]
    public string IcNo { get; set; }

    public char Gender { get; set; }

    [MaxLength(50)]
    public string Email { get; set; }
    [MaxLength(50)]
    public string Phone { get; set; }
    [MaxLength(100)]
    public string Hash { get; set; }

    [MaxLength(100)]
    public string PhotoURL { get; set; }
    [MaxLength(10)]
    public string Status { get; set; }
    [MaxLength(1)]
    public int EmailVerified {  get; set; }
    public string Role => GetType().Name;

    public Token Token { get; set; }
}
public class Admin : User
{

}
public class Staff : User
{
}

// TODO
public class Member : User
{
    [MaxLength(50)]
    public string Position { get; set; }
    [MaxLength(50)]
    public string Country { get; set; }

    //New add subscription concept
    public bool IsSubscribedToNewsletter { get; set; } // Tracks newsletter subscription
    //New add membership concept
    public int Points { get; set; }

    public decimal MinSpend {  get; set; }

    //navigation property
    public Rank Rank { get; set; }
    public string RankId {  get; set; }

    // Navigation property to track the vouchers the member has redeemed
    public List<MemberVoucher> MemberVoucher { get; set; }

    public List<Rent> Rents { get; set; }
    public Subscriptions Subscriptions { get; set; }
    public Review Review { get; set; }
}

public class Voucher
{
    [Key]
    public string Id { get; set; }
    public string Name { get; set; }

    public int PointNeeded {  get; set; }

    public string Description { get; set; }

    public int CashDiscount {  get; set; }

    public int Qty {  get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public string Status { get; set; }
    // Navigation property to track which members have redeemed this voucher
    public List<Booking>? Bookings { get; set; }
    public List<MemberVoucher>? MemberVoucher { get; set; }

}

// This class represents the join table between Member and Voucher
public class MemberVoucher
{
    [Key]
    public int Id { get; set; }  // Unique identifier for this record

    public string MemberId { get; set; }
    public virtual Member Member { get; set; }  // Navigation property to Member

    public string VoucherId { get; set; }
    public virtual Voucher Voucher { get; set; }  // Navigation property to Voucher

    public int Amount { get; set; }  // How many times the voucher has been redeemed by this member
}
public class Rank
{
    [Key]
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public  int MinSpend {  get; set; }    
    public int Discounts {  get; set; }
    public List<Member> Members { get; set; }
}

public class Token
{
    [Key]
    public string Id { get; set; }
    public DateTime Expired { get; set; }

    // Foreign Key
    public string UserId { get; set; }
    public User User { get; set; }
}

public class Bus
{
    [Key]
    public string Id { get; set; }

    public string Name {  get; set; }
    public string BusPlate { get; set; }

    public string Status { get; set; }

    public int Capacity { get; set; }

    [MaxLength(100)]
    public string PhotoURL { get; set; }

    // Foreign Key
    public string CategoryBusId { get; set; }

    // Navigation Properties
    public CategoryBus CategoryBus { get; set; }
    public List<Seat> Seats { get; set; } // One-to-Many relationship
    public List<Schedule> Schedules { get; set; } // One-to-Many relationship
}

public class CategoryBus
{
    [Key]
    public string Id { get; set; }

    public string Name { get; set; }

    // Navigation Property
    public List<Bus> Buses { get; set; } // One-to-Many relationship
}

public class Seat
{
    [Key]
    public int Id { get; set; } // Auto-increment primary key

    // Foreign Key
    public string BusId { get; set; }

    public string SeatNo { get; set; }

    // Navigation Property
    public Bus Bus { get; set; } // Each seat belongs to one bus
}


public class Schedule
{
    [Key]
    public string Id { get; set; }
    public DateOnly DepartDate { get; set; }
    public TimeOnly DepartTime { get; set; }
    public int Price {  get; set; }
    public int DiscountPrice { get; set; }

    public string Status {  get; set; }
    public string Remark {  get; set; }

    // Foreign Key
    public string BusId { get; set; }
    public string RouteLocationId { get; set; }

    // Navigation Property
    public Bus Bus { get; set; } 
    public RouteLocation RouteLocation { get; set; }

    public List<Booking> Bookings { get; set; }   
}

public class RouteLocation
{
    [Key]
    public string Id { get; set; }
    public string Depart {  get; set; }
    public string Destination { get; set; }
    public int Hour {  get; set; }
    public int Min {  get; set; }

    public List<Schedule> Schedules {  get; set; }

}

public class Rent
{
    [Key]
    public string Id { get; set; }
    public TimeOnly DepTime { get; set; }
    public TimeOnly ArrTime { get; set; }
    public DateOnly Start_Date { get; set; }
    public DateOnly End_Date { get; set; }
    public string Location { get; set; }
    public string Destination { get; set; }

    public string Purpose { get; set; }
    public int Numppl { get; set; }
    public string PerIC { get; set; }
    public string Phone { get; set; }

    [EmailAddress]
    public string Email { get; set; }

    public string Req { get; set; }
    public string status { get; set; }

    // Foreign Key
    public string MemberId { get; set; }

    // Navigation Property
    public Member Member { get; set; }
}
public class Subscriptions
{
    [Key]
    public int Id { get; set; } // Added get and set accessors for Id

    public bool Paid { get; set; }
    public DateOnly SubscribeDate { get; set; }
    public int Price { get; set; }

    // Navigation Property
    public string MemberId { get; set; }
    public Member Member { get; set; } // Changed from List<Member> to Member for a one-to-one relationship
}

public class Booking
{
    [Key]
    public string Id { get; set; }
    public int Price {  get; set; }
    public int Qty {  get; set; }
    public string Status {  get; set; }
    public DateTime BookingDateTime { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }

    // Foreign Key
    public string? VoucherId { get; set; }
    public string ScheduleId { get; set; }
    public string MemberId { get; set; }

    // Navigation Properties
    public Schedule Schedule { get; set; }
    public Member Member { get; set; }
    public Voucher? Voucher { get; set; } // Nullable navigation property

    // One-to-Many Relationship
    public List<BookingSeat> BookingSeats { get; set; } // Corrected to List
}


public class BookingSeat
{
    [Key]
    public int Id { get; set; }

    public string SeatNo { get; set; }
    public string Status { get; set; } // Booked / Cancelled

    // Foreign Key
    public string BookingId { get; set; }

    // Navigation Property
    public Booking Booking { get; set; } // Corrected to a single Booking
}

public class Review
{
    public string Id { get; set; }
    public int Rating {  get; set; }
    public string Comment {  get; set; }
    public DateOnly CommentDate {  get; set; }
    public int numberOfComments {  get; set; }

    public string MemberId { get; set; }
    public Member Member{ get; set; }
}


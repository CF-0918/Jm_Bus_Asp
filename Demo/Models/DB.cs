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

    public DbSet<Token> Tokens { get; set; }
    public DbSet<CategoryBus> CategoryBuses { get; set; }
    public DbSet<Bus> Buses { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<Schedule> Schedule { get; set; }
    
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

    //New add membership concept
    public int Points { get; set; }

    public decimal MinSpend {  get; set; }

    //navigation property
    public Rank Rank { get; set; }
    public string RankId {  get; set; }

    // Navigation property to track the vouchers the member has redeemed
    public List<MemberVoucher> MemberVoucher { get; set; }
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
    public List<MemberVoucher> MemberVoucher { get; set; }
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
    public List<Member> Member { get; set; }
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
    public int Id { get; set; }//auto increment

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

    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public string Route { get; set; }

    // Foreign Key
    public string BusId { get; set; }

    // Navigation Property
    public Bus Bus { get; set; } // One-to-Many relationship
}

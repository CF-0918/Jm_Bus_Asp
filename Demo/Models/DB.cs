using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;

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

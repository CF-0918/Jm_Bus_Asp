using Microsoft.EntityFrameworkCore;
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

// TODO


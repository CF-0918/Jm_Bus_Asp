﻿// <auto-generated />
using System;
using Demo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Demo.Migrations
{
    [DbContext(typeof(DB))]
    [Migration("20241212120608_addNewQTyColumn")]
    partial class addNewQTyColumn
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Demo.Models.Bus", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("BusPlate")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Capacity")
                        .HasColumnType("int");

                    b.Property<string>("CategoryBusId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhotoURL")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CategoryBusId");

                    b.ToTable("Buses");
                });

            modelBuilder.Entity("Demo.Models.CategoryBus", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("CategoryBuses");
                });

            modelBuilder.Entity("Demo.Models.MemberVoucher", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("Amount")
                        .HasColumnType("int");

                    b.Property<string>("MemberId")
                        .IsRequired()
                        .HasColumnType("nvarchar(10)");

                    b.Property<string>("VoucherId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("MemberId");

                    b.HasIndex("VoucherId");

                    b.ToTable("MemberVouchers");
                });

            modelBuilder.Entity("Demo.Models.Rank", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Discounts")
                        .HasColumnType("int");

                    b.Property<int>("MinSpend")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Ranks");
                });

            modelBuilder.Entity("Demo.Models.Schedule", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("ArrivalTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("BusId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("DepartureTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Route")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("BusId");

                    b.ToTable("Schedule");
                });

            modelBuilder.Entity("Demo.Models.Seat", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("BusId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("SeatNo")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("BusId");

                    b.ToTable("Seats");
                });

            modelBuilder.Entity("Demo.Models.Token", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("Expired")
                        .HasColumnType("datetime2");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(10)");

                    b.HasKey("Id");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.ToTable("Tokens");
                });

            modelBuilder.Entity("Demo.Models.User", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.Property<int>("Age")
                        .HasMaxLength(2)
                        .HasColumnType("int");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("nvarchar(8)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<int>("EmailVerified")
                        .HasMaxLength(1)
                        .HasColumnType("int");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Gender")
                        .IsRequired()
                        .HasColumnType("nvarchar(1)");

                    b.Property<string>("Hash")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("IcNo")
                        .IsRequired()
                        .HasMaxLength(15)
                        .HasColumnType("nvarchar(15)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("PhotoURL")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.HasKey("Id");

                    b.ToTable("Users");

                    b.HasDiscriminator().HasValue("User");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("Demo.Models.Voucher", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("CashDiscount")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateOnly>("EndDate")
                        .HasColumnType("date");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("PointNeeded")
                        .HasColumnType("int");

                    b.Property<int>("Qty")
                        .HasColumnType("int");

                    b.Property<DateOnly>("StartDate")
                        .HasColumnType("date");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Vouchers");
                });

            modelBuilder.Entity("Demo.Models.Admin", b =>
                {
                    b.HasBaseType("Demo.Models.User");

                    b.HasDiscriminator().HasValue("Admin");
                });

            modelBuilder.Entity("Demo.Models.Member", b =>
                {
                    b.HasBaseType("Demo.Models.User");

                    b.Property<string>("Country")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<decimal>("MinSpend")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("Points")
                        .HasColumnType("int");

                    b.Property<string>("Position")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("RankId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasIndex("RankId");

                    b.HasDiscriminator().HasValue("Member");
                });

            modelBuilder.Entity("Demo.Models.Staff", b =>
                {
                    b.HasBaseType("Demo.Models.User");

                    b.HasDiscriminator().HasValue("Staff");
                });

            modelBuilder.Entity("Demo.Models.Bus", b =>
                {
                    b.HasOne("Demo.Models.CategoryBus", "CategoryBus")
                        .WithMany("Buses")
                        .HasForeignKey("CategoryBusId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CategoryBus");
                });

            modelBuilder.Entity("Demo.Models.MemberVoucher", b =>
                {
                    b.HasOne("Demo.Models.Member", "Member")
                        .WithMany("MemberVoucher")
                        .HasForeignKey("MemberId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Demo.Models.Voucher", "Voucher")
                        .WithMany("MemberVoucher")
                        .HasForeignKey("VoucherId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Member");

                    b.Navigation("Voucher");
                });

            modelBuilder.Entity("Demo.Models.Schedule", b =>
                {
                    b.HasOne("Demo.Models.Bus", "Bus")
                        .WithMany("Schedules")
                        .HasForeignKey("BusId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Bus");
                });

            modelBuilder.Entity("Demo.Models.Seat", b =>
                {
                    b.HasOne("Demo.Models.Bus", "Bus")
                        .WithMany("Seats")
                        .HasForeignKey("BusId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Bus");
                });

            modelBuilder.Entity("Demo.Models.Token", b =>
                {
                    b.HasOne("Demo.Models.User", "User")
                        .WithOne("Token")
                        .HasForeignKey("Demo.Models.Token", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Demo.Models.Member", b =>
                {
                    b.HasOne("Demo.Models.Rank", "Rank")
                        .WithMany("Member")
                        .HasForeignKey("RankId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Rank");
                });

            modelBuilder.Entity("Demo.Models.Bus", b =>
                {
                    b.Navigation("Schedules");

                    b.Navigation("Seats");
                });

            modelBuilder.Entity("Demo.Models.CategoryBus", b =>
                {
                    b.Navigation("Buses");
                });

            modelBuilder.Entity("Demo.Models.Rank", b =>
                {
                    b.Navigation("Member");
                });

            modelBuilder.Entity("Demo.Models.User", b =>
                {
                    b.Navigation("Token")
                        .IsRequired();
                });

            modelBuilder.Entity("Demo.Models.Voucher", b =>
                {
                    b.Navigation("MemberVoucher");
                });

            modelBuilder.Entity("Demo.Models.Member", b =>
                {
                    b.Navigation("MemberVoucher");
                });
#pragma warning restore 612, 618
        }
    }
}

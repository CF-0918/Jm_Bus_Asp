using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class AddNewTableAndAlterScheduleColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedule_Buses_BusId",
                table: "Schedule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Schedule",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "ArrivalTime",
                table: "Schedule");

            migrationBuilder.DropColumn(
                name: "DepartureTime",
                table: "Schedule");

            migrationBuilder.RenameTable(
                name: "Schedule",
                newName: "Schedules");

            migrationBuilder.RenameColumn(
                name: "Route",
                table: "Schedules",
                newName: "TicketType");

            migrationBuilder.RenameIndex(
                name: "IX_Schedule_BusId",
                table: "Schedules",
                newName: "IX_Schedules_BusId");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DepartDate",
                table: "Schedules",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DepartTime",
                table: "Schedules",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "DiscountPrice",
                table: "Schedules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Price",
                table: "Schedules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "Schedules",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "ReturnDate",
                table: "Schedules",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ReturnTime",
                table: "Schedules",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "RouteId",
                table: "Schedules",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Schedules",
                table: "Schedules",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Depart = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hour = table.Column<int>(type: "int", nullable: false),
                    Min = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_RouteId",
                table: "Schedules",
                column: "RouteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Buses_BusId",
                table: "Schedules",
                column: "BusId",
                principalTable: "Buses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Routes_RouteId",
                table: "Schedules",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Buses_BusId",
                table: "Schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Routes_RouteId",
                table: "Schedules");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Schedules",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_RouteId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "DepartDate",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "DepartTime",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "DiscountPrice",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ReturnDate",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "ReturnTime",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "RouteId",
                table: "Schedules");

            migrationBuilder.RenameTable(
                name: "Schedules",
                newName: "Schedule");

            migrationBuilder.RenameColumn(
                name: "TicketType",
                table: "Schedule",
                newName: "Route");

            migrationBuilder.RenameIndex(
                name: "IX_Schedules_BusId",
                table: "Schedule",
                newName: "IX_Schedule_BusId");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArrivalTime",
                table: "Schedule",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DepartureTime",
                table: "Schedule",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Schedule",
                table: "Schedule",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedule_Buses_BusId",
                table: "Schedule",
                column: "BusId",
                principalTable: "Buses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

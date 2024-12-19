using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class editsomething2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingSeats_Bookings_BookingId",
                table: "BookingSeats");

            migrationBuilder.DropIndex(
                name: "IX_BookingSeats_BookingId",
                table: "BookingSeats");

            migrationBuilder.AlterColumn<string>(
                name: "BookingId",
                table: "BookingSeats",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "BookingSeatId",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BookingSeatId",
                table: "Bookings",
                column: "BookingSeatId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_BookingSeats_BookingSeatId",
                table: "Bookings",
                column: "BookingSeatId",
                principalTable: "BookingSeats",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_BookingSeats_BookingSeatId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BookingSeatId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookingSeatId",
                table: "Bookings");

            migrationBuilder.AlterColumn<string>(
                name: "BookingId",
                table: "BookingSeats",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeats_BookingId",
                table: "BookingSeats",
                column: "BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingSeats_Bookings_BookingId",
                table: "BookingSeats",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

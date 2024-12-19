using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class editsomething3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AlterColumn<string>(
                name: "MemberId",
                table: "Bookings",
                type: "nvarchar(10)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeats_BookingId",
                table: "BookingSeats",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_MemberId",
                table: "Bookings",
                column: "MemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_MemberId",
                table: "Bookings",
                column: "MemberId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BookingSeats_Bookings_BookingId",
                table: "BookingSeats",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_MemberId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_BookingSeats_Bookings_BookingId",
                table: "BookingSeats");

            migrationBuilder.DropIndex(
                name: "IX_BookingSeats_BookingId",
                table: "BookingSeats");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_MemberId",
                table: "Bookings");

            migrationBuilder.AlterColumn<string>(
                name: "BookingId",
                table: "BookingSeats",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "MemberId",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)");

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
    }
}

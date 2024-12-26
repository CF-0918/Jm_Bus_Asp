using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class JaydenPart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rent_Users_MemberId",
                table: "Rent");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rent",
                table: "Rent");

            migrationBuilder.RenameTable(
                name: "Rent",
                newName: "Rents");

            migrationBuilder.RenameIndex(
                name: "IX_Rent_MemberId",
                table: "Rents",
                newName: "IX_Rents_MemberId");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "Start_Date",
                table: "Rents",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "End_Date",
                table: "Rents",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rents",
                table: "Rents",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rents_Users_MemberId",
                table: "Rents",
                column: "MemberId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rents_Users_MemberId",
                table: "Rents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Rents",
                table: "Rents");

            migrationBuilder.RenameTable(
                name: "Rents",
                newName: "Rent");

            migrationBuilder.RenameIndex(
                name: "IX_Rents_MemberId",
                table: "Rent",
                newName: "IX_Rent_MemberId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Start_Date",
                table: "Rent",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTime>(
                name: "End_Date",
                table: "Rent",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Rent",
                table: "Rent",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Rent_Users_MemberId",
                table: "Rent",
                column: "MemberId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

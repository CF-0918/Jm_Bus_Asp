using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class updateagian : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Subscriptionses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Paid = table.Column<bool>(type: "bit", nullable: false),
                    SubscribeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false),
                    MemberId = table.Column<string>(type: "nvarchar(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptionses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptionses_Users_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptionses_MemberId",
                table: "Subscriptionses",
                column: "MemberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subscriptionses");
        }
    }
}

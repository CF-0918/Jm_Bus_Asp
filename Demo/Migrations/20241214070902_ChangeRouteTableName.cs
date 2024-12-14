using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Migrations
{
    /// <inheritdoc />
    public partial class ChangeRouteTableName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Routes_RouteId",
                table: "Schedules");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.RenameColumn(
                name: "RouteId",
                table: "Schedules",
                newName: "RouteLocationId");

            migrationBuilder.RenameIndex(
                name: "IX_Schedules_RouteId",
                table: "Schedules",
                newName: "IX_Schedules_RouteLocationId");

            migrationBuilder.CreateTable(
                name: "RouteLocations",
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
                    table.PrimaryKey("PK_RouteLocations", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_RouteLocations_RouteLocationId",
                table: "Schedules",
                column: "RouteLocationId",
                principalTable: "RouteLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_RouteLocations_RouteLocationId",
                table: "Schedules");

            migrationBuilder.DropTable(
                name: "RouteLocations");

            migrationBuilder.RenameColumn(
                name: "RouteLocationId",
                table: "Schedules",
                newName: "RouteId");

            migrationBuilder.RenameIndex(
                name: "IX_Schedules_RouteLocationId",
                table: "Schedules",
                newName: "IX_Schedules_RouteId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Routes_RouteId",
                table: "Schedules",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

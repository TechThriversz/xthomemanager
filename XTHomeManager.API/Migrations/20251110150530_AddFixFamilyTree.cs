using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XTHomeManager.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFixFamilyTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FamilyMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Birthday = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Relation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpouseIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeceased = table.Column<bool>(type: "bit", nullable: false),
                    DeathDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BornPlace = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiedPlace = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OtherMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Relation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Birthday = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeceased = table.Column<bool>(type: "bit", nullable: false),
                    DeathDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BornPlace = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiedPlace = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtherMembers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FamilyMembers");

            migrationBuilder.DropTable(
                name: "OtherMembers");
        }
    }
}

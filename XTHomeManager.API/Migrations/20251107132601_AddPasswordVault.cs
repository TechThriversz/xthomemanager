using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XTHomeManager.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordVault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "RentEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Passwords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EncryptedPassword = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssociatedPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecoveryEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityQuestions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Passwords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Passwords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Passwords_UserId",
                table: "Passwords",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Passwords");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "RentEntries");
        }
    }
}

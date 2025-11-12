using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XTHomeManager.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Settings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Pakistan");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Settings",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "PKR");

            migrationBuilder.AddColumn<string>(
                name: "DateFormat",
                table: "Settings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "dd/MM/yyyy");

            migrationBuilder.AddColumn<int>(
                name: "DecimalPlaces",
                table: "Settings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WeightUnit",
                table: "Settings",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "kg");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "DateFormat",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "DecimalPlaces",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "WeightUnit",
                table: "Settings");
        }
    }
}

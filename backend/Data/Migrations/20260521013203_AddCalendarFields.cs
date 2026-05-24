using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskHub.backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAllDay",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReminderMinutesBefore",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RepeatEndDate",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepeatInterval",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RepeatType",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "None");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IsAllDay",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ReminderMinutesBefore",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RepeatEndDate",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RepeatInterval",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RepeatType",
                table: "Tasks");
        }
    }
}

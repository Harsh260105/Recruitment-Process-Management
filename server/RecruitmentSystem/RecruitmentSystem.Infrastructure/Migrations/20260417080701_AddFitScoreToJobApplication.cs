using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFitScoreToJobApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FitScore",
                table: "JobApplications",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("1a73a5b5-bf8f-4398-9a31-0d136fd62ac1"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 8, 7, 1, 116, DateTimeKind.Utc).AddTicks(671));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("2c73a5b5-bf8f-4398-9a31-0d136fd62ac2"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 8, 7, 1, 116, DateTimeKind.Utc).AddTicks(1421));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("3b73a5b5-bf8f-4398-9a31-0d136fd62ac3"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 8, 7, 1, 116, DateTimeKind.Utc).AddTicks(1424));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("4d73a5b5-bf8f-4398-9a31-0d136fd62ac4"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 8, 7, 1, 116, DateTimeKind.Utc).AddTicks(1426));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5e73a5b5-bf8f-4398-9a31-0d136fd62ac5"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 8, 7, 1, 116, DateTimeKind.Utc).AddTicks(1427));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("6f73a5b5-bf8f-4398-9a31-0d136fd62ac6"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 8, 7, 1, 116, DateTimeKind.Utc).AddTicks(1428));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("7a73a5b5-bf8f-4398-9a31-0d136fd62ac7"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 8, 7, 1, 116, DateTimeKind.Utc).AddTicks(1429));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("8b73a5b5-bf8f-4398-9a31-0d136fd62ac8"),
                column: "CreatedAt",
                value: new DateTime(2026, 4, 17, 8, 7, 1, 116, DateTimeKind.Utc).AddTicks(1430));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FitScore",
                table: "JobApplications");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("1a73a5b5-bf8f-4398-9a31-0d136fd62ac1"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 30, 17, 31, 25, 778, DateTimeKind.Utc).AddTicks(4345));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("2c73a5b5-bf8f-4398-9a31-0d136fd62ac2"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 30, 17, 31, 25, 778, DateTimeKind.Utc).AddTicks(5404));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("3b73a5b5-bf8f-4398-9a31-0d136fd62ac3"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 30, 17, 31, 25, 778, DateTimeKind.Utc).AddTicks(5406));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("4d73a5b5-bf8f-4398-9a31-0d136fd62ac4"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 30, 17, 31, 25, 778, DateTimeKind.Utc).AddTicks(5408));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5e73a5b5-bf8f-4398-9a31-0d136fd62ac5"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 30, 17, 31, 25, 778, DateTimeKind.Utc).AddTicks(5409));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("6f73a5b5-bf8f-4398-9a31-0d136fd62ac6"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 30, 17, 31, 25, 778, DateTimeKind.Utc).AddTicks(5410));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("7a73a5b5-bf8f-4398-9a31-0d136fd62ac7"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 30, 17, 31, 25, 778, DateTimeKind.Utc).AddTicks(5411));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("8b73a5b5-bf8f-4398-9a31-0d136fd62ac8"),
                column: "CreatedAt",
                value: new DateTime(2026, 3, 30, 17, 31, 25, 778, DateTimeKind.Utc).AddTicks(5412));
        }
    }
}

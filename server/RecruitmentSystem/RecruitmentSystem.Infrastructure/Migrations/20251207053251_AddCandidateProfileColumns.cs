using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateProfileColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanBypassApplicationLimits",
                table: "CandidateProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OverrideExpiresAt",
                table: "CandidateProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("1a73a5b5-bf8f-4398-9a31-0d136fd62ac1"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 7, 5, 32, 49, 495, DateTimeKind.Utc).AddTicks(8398));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("2c73a5b5-bf8f-4398-9a31-0d136fd62ac2"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 7, 5, 32, 49, 495, DateTimeKind.Utc).AddTicks(9786));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("3b73a5b5-bf8f-4398-9a31-0d136fd62ac3"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 7, 5, 32, 49, 495, DateTimeKind.Utc).AddTicks(9792));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("4d73a5b5-bf8f-4398-9a31-0d136fd62ac4"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 7, 5, 32, 49, 495, DateTimeKind.Utc).AddTicks(9794));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5e73a5b5-bf8f-4398-9a31-0d136fd62ac5"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 7, 5, 32, 49, 495, DateTimeKind.Utc).AddTicks(9796));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("6f73a5b5-bf8f-4398-9a31-0d136fd62ac6"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 7, 5, 32, 49, 495, DateTimeKind.Utc).AddTicks(9798));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("7a73a5b5-bf8f-4398-9a31-0d136fd62ac7"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 7, 5, 32, 49, 495, DateTimeKind.Utc).AddTicks(9800));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("8b73a5b5-bf8f-4398-9a31-0d136fd62ac8"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 7, 5, 32, 49, 495, DateTimeKind.Utc).AddTicks(9802));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanBypassApplicationLimits",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "OverrideExpiresAt",
                table: "CandidateProfiles");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("1a73a5b5-bf8f-4398-9a31-0d136fd62ac1"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 15, 5, 13, 26, 193, DateTimeKind.Utc).AddTicks(1887));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("2c73a5b5-bf8f-4398-9a31-0d136fd62ac2"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 15, 5, 13, 26, 193, DateTimeKind.Utc).AddTicks(3015));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("3b73a5b5-bf8f-4398-9a31-0d136fd62ac3"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 15, 5, 13, 26, 193, DateTimeKind.Utc).AddTicks(3018));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("4d73a5b5-bf8f-4398-9a31-0d136fd62ac4"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 15, 5, 13, 26, 193, DateTimeKind.Utc).AddTicks(3020));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5e73a5b5-bf8f-4398-9a31-0d136fd62ac5"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 15, 5, 13, 26, 193, DateTimeKind.Utc).AddTicks(3021));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("6f73a5b5-bf8f-4398-9a31-0d136fd62ac6"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 15, 5, 13, 26, 193, DateTimeKind.Utc).AddTicks(3022));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("7a73a5b5-bf8f-4398-9a31-0d136fd62ac7"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 15, 5, 13, 26, 193, DateTimeKind.Utc).AddTicks(3024));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("8b73a5b5-bf8f-4398-9a31-0d136fd62ac8"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 15, 5, 13, 26, 193, DateTimeKind.Utc).AddTicks(3025));
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStaffProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StaffProfiles_Users_ReportingManagerId",
                table: "StaffProfiles");

            migrationBuilder.DropIndex(
                name: "IX_StaffProfiles_ReportingManagerId",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "Designation",
                table: "StaffProfiles");

            migrationBuilder.DropColumn(
                name: "ReportingManagerId",
                table: "StaffProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Skills",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Skills",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Roles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "CandidateWorkExperiences",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "JobDescription",
                table: "CandidateWorkExperiences",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "EmploymentType",
                table: "CandidateWorkExperiences",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "CandidateProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ResumeFilePath",
                table: "CandidateProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "ResumeFileName",
                table: "CandidateProfiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "PortfolioUrl",
                table: "CandidateProfiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "LinkedInProfile",
                table: "CandidateProfiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "GitHubProfile",
                table: "CandidateProfiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Degree",
                table: "CandidateProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "CurrentLocation",
                table: "CandidateProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "College",
                table: "CandidateProfiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<decimal>(
                name: "GPA",
                table: "CandidateEducations",
                type: "decimal(4,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(4,2)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("1a73a5b5-bf8f-4398-9a31-0d136fd62ac1"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 25, 10, 31, 32, 888, DateTimeKind.Utc).AddTicks(7256));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("2c73a5b5-bf8f-4398-9a31-0d136fd62ac2"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 25, 10, 31, 32, 888, DateTimeKind.Utc).AddTicks(8904));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("3b73a5b5-bf8f-4398-9a31-0d136fd62ac3"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 25, 10, 31, 32, 888, DateTimeKind.Utc).AddTicks(8945));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("4d73a5b5-bf8f-4398-9a31-0d136fd62ac4"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 25, 10, 31, 32, 888, DateTimeKind.Utc).AddTicks(8947));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5e73a5b5-bf8f-4398-9a31-0d136fd62ac5"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 25, 10, 31, 32, 888, DateTimeKind.Utc).AddTicks(8948));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("6f73a5b5-bf8f-4398-9a31-0d136fd62ac6"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 25, 10, 31, 32, 888, DateTimeKind.Utc).AddTicks(8949));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("7a73a5b5-bf8f-4398-9a31-0d136fd62ac7"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 25, 10, 31, 32, 888, DateTimeKind.Utc).AddTicks(8951));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("8b73a5b5-bf8f-4398-9a31-0d136fd62ac8"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 25, 10, 31, 32, 888, DateTimeKind.Utc).AddTicks(8952));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Designation",
                table: "StaffProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ReportingManagerId",
                table: "StaffProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Skills",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Skills",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Roles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "CandidateWorkExperiences",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JobDescription",
                table: "CandidateWorkExperiences",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmploymentType",
                table: "CandidateWorkExperiences",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "CandidateProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResumeFilePath",
                table: "CandidateProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResumeFileName",
                table: "CandidateProfiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PortfolioUrl",
                table: "CandidateProfiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LinkedInProfile",
                table: "CandidateProfiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GitHubProfile",
                table: "CandidateProfiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Degree",
                table: "CandidateProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CurrentLocation",
                table: "CandidateProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "College",
                table: "CandidateProfiles",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GPA",
                table: "CandidateEducations",
                type: "decimal(4,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(4,2)");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("1a73a5b5-bf8f-4398-9a31-0d136fd62ac1"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 10, 48, 42, 872, DateTimeKind.Utc).AddTicks(4942));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("2c73a5b5-bf8f-4398-9a31-0d136fd62ac2"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 10, 48, 42, 872, DateTimeKind.Utc).AddTicks(5924));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("3b73a5b5-bf8f-4398-9a31-0d136fd62ac3"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 10, 48, 42, 872, DateTimeKind.Utc).AddTicks(5929));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("4d73a5b5-bf8f-4398-9a31-0d136fd62ac4"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 10, 48, 42, 872, DateTimeKind.Utc).AddTicks(5930));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5e73a5b5-bf8f-4398-9a31-0d136fd62ac5"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 10, 48, 42, 872, DateTimeKind.Utc).AddTicks(5932));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("6f73a5b5-bf8f-4398-9a31-0d136fd62ac6"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 10, 48, 42, 872, DateTimeKind.Utc).AddTicks(5933));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("7a73a5b5-bf8f-4398-9a31-0d136fd62ac7"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 10, 48, 42, 872, DateTimeKind.Utc).AddTicks(5934));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("8b73a5b5-bf8f-4398-9a31-0d136fd62ac8"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 10, 48, 42, 872, DateTimeKind.Utc).AddTicks(5936));

            migrationBuilder.CreateIndex(
                name: "IX_StaffProfiles_ReportingManagerId",
                table: "StaffProfiles",
                column: "ReportingManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_StaffProfiles_Users_ReportingManagerId",
                table: "StaffProfiles",
                column: "ReportingManagerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGPAFieldToCandidateEducation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "GPAScale",
                table: "CandidateEducations",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "FieldOfStudy",
                table: "CandidateEducations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "EducationType",
                table: "CandidateEducations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<decimal>(
                name: "GPA",
                table: "CandidateEducations",
                type: "decimal(4,2)",
                nullable: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GPA",
                table: "CandidateEducations");

            migrationBuilder.AlterColumn<string>(
                name: "GPAScale",
                table: "CandidateEducations",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FieldOfStudy",
                table: "CandidateEducations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EducationType",
                table: "CandidateEducations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("1a73a5b5-bf8f-4398-9a31-0d136fd62ac1"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 50, 3, 948, DateTimeKind.Utc).AddTicks(1439));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("2c73a5b5-bf8f-4398-9a31-0d136fd62ac2"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 50, 3, 948, DateTimeKind.Utc).AddTicks(2414));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("3b73a5b5-bf8f-4398-9a31-0d136fd62ac3"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 50, 3, 948, DateTimeKind.Utc).AddTicks(2419));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("4d73a5b5-bf8f-4398-9a31-0d136fd62ac4"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 50, 3, 948, DateTimeKind.Utc).AddTicks(2420));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5e73a5b5-bf8f-4398-9a31-0d136fd62ac5"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 50, 3, 948, DateTimeKind.Utc).AddTicks(2421));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("6f73a5b5-bf8f-4398-9a31-0d136fd62ac6"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 50, 3, 948, DateTimeKind.Utc).AddTicks(2423));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("7a73a5b5-bf8f-4398-9a31-0d136fd62ac7"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 50, 3, 948, DateTimeKind.Utc).AddTicks(2424));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("8b73a5b5-bf8f-4398-9a31-0d136fd62ac8"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 50, 3, 948, DateTimeKind.Utc).AddTicks(2426));
        }
    }
}

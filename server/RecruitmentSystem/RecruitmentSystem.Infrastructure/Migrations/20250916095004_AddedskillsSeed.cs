using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RecruitmentSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedskillsSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.InsertData(
                table: "Skills",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Programming Languages", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Modern object-oriented programming language", "C#" },
                    { 2, "Programming Languages", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Popular enterprise programming language", "Java" },
                    { 3, "Programming Languages", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Versatile programming language for data science and web development", "Python" },
                    { 4, "Programming Languages", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Client-side scripting language for web development", "JavaScript" },
                    { 5, "Programming Languages", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Typed superset of JavaScript", "TypeScript" },
                    { 6, "Web Technologies", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "JavaScript library for building user interfaces", "React" },
                    { 7, "Web Technologies", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Platform for building mobile and desktop web applications", "Angular" },
                    { 8, "Web Technologies", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "JavaScript runtime built on Chrome's V8 JavaScript engine", "Node.js" },
                    { 9, "Web Technologies", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Cross-platform framework for building modern web applications", "ASP.NET Core" },
                    { 10, "Databases", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Microsoft's relational database management system", "SQL Server" },
                    { 11, "Databases", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Open-source relational database management system", "MySQL" },
                    { 12, "Databases", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Advanced open-source relational database", "PostgreSQL" },
                    { 13, "Databases", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "NoSQL document database", "MongoDB" },
                    { 14, "Cloud & DevOps", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Amazon Web Services cloud platform", "AWS" },
                    { 15, "Cloud & DevOps", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Microsoft's cloud computing platform", "Azure" },
                    { 16, "Cloud & DevOps", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Platform for developing, shipping, and running applications", "Docker" },
                    { 17, "Cloud & DevOps", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Distributed version control system", "Git" },
                    { 18, "Soft Skills", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Ability to convey information effectively", "Communication" },
                    { 19, "Soft Skills", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Ability to work effectively with others", "Teamwork" },
                    { 20, "Soft Skills", new DateTime(2025, 9, 16, 9, 0, 0, 0, DateTimeKind.Utc), "Ability to identify and resolve problems", "Problem Solving" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Skills",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("1a73a5b5-bf8f-4398-9a31-0d136fd62ac1"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 1, 46, 823, DateTimeKind.Utc).AddTicks(5967));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("2c73a5b5-bf8f-4398-9a31-0d136fd62ac2"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 1, 46, 823, DateTimeKind.Utc).AddTicks(6954));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("3b73a5b5-bf8f-4398-9a31-0d136fd62ac3"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 1, 46, 823, DateTimeKind.Utc).AddTicks(6958));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("4d73a5b5-bf8f-4398-9a31-0d136fd62ac4"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 1, 46, 823, DateTimeKind.Utc).AddTicks(6959));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5e73a5b5-bf8f-4398-9a31-0d136fd62ac5"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 1, 46, 823, DateTimeKind.Utc).AddTicks(6961));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("6f73a5b5-bf8f-4398-9a31-0d136fd62ac6"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 1, 46, 823, DateTimeKind.Utc).AddTicks(6962));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("7a73a5b5-bf8f-4398-9a31-0d136fd62ac7"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 1, 46, 823, DateTimeKind.Utc).AddTicks(6963));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("8b73a5b5-bf8f-4398-9a31-0d136fd62ac8"),
                column: "CreatedAt",
                value: new DateTime(2025, 9, 16, 9, 1, 46, 823, DateTimeKind.Utc).AddTicks(6965));
        }
    }
}

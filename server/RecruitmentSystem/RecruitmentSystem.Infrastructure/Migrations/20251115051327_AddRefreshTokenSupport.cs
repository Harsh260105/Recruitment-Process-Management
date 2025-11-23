using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecruitmentSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReasonRevoked = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("1a73a5b5-bf8f-4398-9a31-0d136fd62ac1"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 2, 5, 24, 6, 821, DateTimeKind.Utc).AddTicks(1148));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("2c73a5b5-bf8f-4398-9a31-0d136fd62ac2"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 2, 5, 24, 6, 821, DateTimeKind.Utc).AddTicks(2192));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("3b73a5b5-bf8f-4398-9a31-0d136fd62ac3"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 2, 5, 24, 6, 821, DateTimeKind.Utc).AddTicks(2196));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("4d73a5b5-bf8f-4398-9a31-0d136fd62ac4"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 2, 5, 24, 6, 821, DateTimeKind.Utc).AddTicks(2198));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("5e73a5b5-bf8f-4398-9a31-0d136fd62ac5"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 2, 5, 24, 6, 821, DateTimeKind.Utc).AddTicks(2199));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("6f73a5b5-bf8f-4398-9a31-0d136fd62ac6"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 2, 5, 24, 6, 821, DateTimeKind.Utc).AddTicks(2200));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("7a73a5b5-bf8f-4398-9a31-0d136fd62ac7"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 2, 5, 24, 6, 821, DateTimeKind.Utc).AddTicks(2202));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("8b73a5b5-bf8f-4398-9a31-0d136fd62ac8"),
                column: "CreatedAt",
                value: new DateTime(2025, 11, 2, 5, 24, 6, 821, DateTimeKind.Utc).AddTicks(2203));
        }
    }
}

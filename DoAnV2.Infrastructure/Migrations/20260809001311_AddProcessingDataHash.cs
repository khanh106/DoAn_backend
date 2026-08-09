using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnV2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingDataHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "data_hash",
                table: "processings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 0, 13, 9, 202, DateTimeKind.Utc).AddTicks(547));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 0, 13, 9, 202, DateTimeKind.Utc).AddTicks(3457));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 0, 13, 9, 202, DateTimeKind.Utc).AddTicks(3462));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 4,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 0, 13, 9, 202, DateTimeKind.Utc).AddTicks(3465));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2026, 8, 9, 0, 13, 9, 847, DateTimeKind.Utc).AddTicks(143), "$2a$11$noccDtIJjN7Ag8cA94DOfuzQXeKS5oVvzrYzTDcvYTUMFaAO0AW.y" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data_hash",
                table: "processings");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2026, 8, 8, 15, 14, 46, 818, DateTimeKind.Utc).AddTicks(5878));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2026, 8, 8, 15, 14, 46, 818, DateTimeKind.Utc).AddTicks(7217));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2026, 8, 8, 15, 14, 46, 818, DateTimeKind.Utc).AddTicks(7235));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 4,
                column: "created_at",
                value: new DateTime(2026, 8, 8, 15, 14, 46, 818, DateTimeKind.Utc).AddTicks(7237));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2026, 8, 8, 15, 14, 47, 479, DateTimeKind.Utc).AddTicks(6235), "$2a$11$hdkYmO/TMpXwEbDPG.Dv5.qSiVXdi93cptZsaNsnd4y/r3VmekaX6" });
        }
    }
}

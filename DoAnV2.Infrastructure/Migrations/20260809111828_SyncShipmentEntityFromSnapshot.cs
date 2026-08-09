using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnV2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncShipmentEntityFromSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 11, 18, 26, 489, DateTimeKind.Utc).AddTicks(236));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 11, 18, 26, 489, DateTimeKind.Utc).AddTicks(2104));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 11, 18, 26, 489, DateTimeKind.Utc).AddTicks(2110));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 4,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 11, 18, 26, 489, DateTimeKind.Utc).AddTicks(2113));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2026, 8, 9, 11, 18, 27, 176, DateTimeKind.Utc).AddTicks(4525), "$2a$11$VSKjT1g.fZCbhdFLRj555.WDfeFM.QuOwb7lIQzN5sjGLL/aVJK8u" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 0, 45, 39, 858, DateTimeKind.Utc).AddTicks(8860));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 0, 45, 39, 859, DateTimeKind.Utc).AddTicks(699));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 0, 45, 39, 859, DateTimeKind.Utc).AddTicks(703));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 4,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 0, 45, 39, 859, DateTimeKind.Utc).AddTicks(719));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2026, 8, 9, 0, 45, 40, 213, DateTimeKind.Utc).AddTicks(9616), "$2a$11$r/BKA.ngcdNHCD7FUXdrt.INRRYVH4KGQAvVuHoH24zhN7hzuA2H." });
        }
    }
}

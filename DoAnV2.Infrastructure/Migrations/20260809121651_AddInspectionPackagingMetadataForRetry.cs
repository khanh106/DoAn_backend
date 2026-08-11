using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnV2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectionPackagingMetadataForRetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "data_hash",
                table: "packagings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata_uri",
                table: "packagings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "data_hash",
                table: "inspections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata_uri",
                table: "inspections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 12, 16, 49, 107, DateTimeKind.Utc).AddTicks(8504));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 12, 16, 49, 108, DateTimeKind.Utc).AddTicks(5687));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 12, 16, 49, 108, DateTimeKind.Utc).AddTicks(5695));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 4,
                column: "created_at",
                value: new DateTime(2026, 8, 9, 12, 16, 49, 108, DateTimeKind.Utc).AddTicks(5697));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2026, 8, 9, 12, 16, 49, 706, DateTimeKind.Utc).AddTicks(4595), "$2a$11$iFqOcRamZ5PmsVfUqANyfO/s8lIZPiTDkxOjA0Nqh3znvwgtuooh2" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data_hash",
                table: "packagings");

            migrationBuilder.DropColumn(
                name: "metadata_uri",
                table: "packagings");

            migrationBuilder.DropColumn(
                name: "data_hash",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "metadata_uri",
                table: "inspections");

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
    }
}

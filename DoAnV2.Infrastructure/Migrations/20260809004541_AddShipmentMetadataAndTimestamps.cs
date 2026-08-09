using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnV2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentMetadataAndTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "data_hash",
                table: "shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "metadata_uri",
                table: "shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ready_data_hash",
                table: "shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ready_for_sale_date",
                table: "shipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ready_metadata_uri",
                table: "shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ready_transaction_hash",
                table: "shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "receive_data_hash",
                table: "shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "receive_metadata_uri",
                table: "shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "receive_transaction_hash",
                table: "shipments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ship_transaction_hash",
                table: "shipments",
                type: "nvarchar(max)",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data_hash",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "metadata_uri",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ready_data_hash",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ready_for_sale_date",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ready_metadata_uri",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ready_transaction_hash",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "receive_data_hash",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "receive_metadata_uri",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "receive_transaction_hash",
                table: "shipments");

            migrationBuilder.DropColumn(
                name: "ship_transaction_hash",
                table: "shipments");

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
    }
}

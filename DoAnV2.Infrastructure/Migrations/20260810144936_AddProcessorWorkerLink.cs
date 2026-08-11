using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnV2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessorWorkerLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "processor_workers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    processor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    worker_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    invited_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    responded_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processor_workers", x => x.id);
                    table.ForeignKey(
                        name: "fk_processor_workers_users_processor_id",
                        column: x => x.processor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_processor_workers_users_worker_id",
                        column: x => x.worker_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2026, 8, 10, 14, 49, 34, 753, DateTimeKind.Utc).AddTicks(2452));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2026, 8, 10, 14, 49, 34, 753, DateTimeKind.Utc).AddTicks(4255));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2026, 8, 10, 14, 49, 34, 753, DateTimeKind.Utc).AddTicks(4258));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 4,
                column: "created_at",
                value: new DateTime(2026, 8, 10, 14, 49, 34, 753, DateTimeKind.Utc).AddTicks(4260));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2026, 8, 10, 14, 49, 35, 82, DateTimeKind.Utc).AddTicks(7922), "$2a$11$mfESCLBm1H.9lLXCUtv03OCfqaaXkHAQiVsGPm1wpfOTFLelcCBie" });

            migrationBuilder.CreateIndex(
                name: "ix_processor_workers_processor_id_worker_id",
                table: "processor_workers",
                columns: new[] { "processor_id", "worker_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_processor_workers_worker_id",
                table: "processor_workers",
                column: "worker_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processor_workers");

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
    }
}

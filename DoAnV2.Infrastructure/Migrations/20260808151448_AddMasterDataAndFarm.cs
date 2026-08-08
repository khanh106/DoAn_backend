using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnV2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterDataAndFarm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_farm_areas_users_processor_id",
                table: "farm_areas");

            migrationBuilder.DropForeignKey(
                name: "fk_fruit_types_users_processor_id",
                table: "fruit_types");

            migrationBuilder.DropForeignKey(
                name: "fk_material_items_users_processor_id",
                table: "material_items");

            migrationBuilder.DropForeignKey(
                name: "fk_products_fruit_types_fruit_type_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_material_items_processor_id",
                table: "material_items");

            migrationBuilder.DropIndex(
                name: "ix_fruit_types_processor_id",
                table: "fruit_types");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "material_items",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "fruit_types",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

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

            migrationBuilder.CreateIndex(
                name: "ix_material_items_processor_id_code",
                table: "material_items",
                columns: new[] { "processor_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fruit_types_processor_id_code",
                table: "fruit_types",
                columns: new[] { "processor_id", "code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_farm_areas_users_processor_id",
                table: "farm_areas",
                column: "processor_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_fruit_types_users_processor_id",
                table: "fruit_types",
                column: "processor_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_material_items_users_processor_id",
                table: "material_items",
                column: "processor_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_fruit_types_fruit_type_id",
                table: "products",
                column: "fruit_type_id",
                principalTable: "fruit_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_farm_areas_users_processor_id",
                table: "farm_areas");

            migrationBuilder.DropForeignKey(
                name: "fk_fruit_types_users_processor_id",
                table: "fruit_types");

            migrationBuilder.DropForeignKey(
                name: "fk_material_items_users_processor_id",
                table: "material_items");

            migrationBuilder.DropForeignKey(
                name: "fk_products_fruit_types_fruit_type_id",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_material_items_processor_id_code",
                table: "material_items");

            migrationBuilder.DropIndex(
                name: "ix_fruit_types_processor_id_code",
                table: "fruit_types");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "material_items",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "fruit_types",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 1,
                column: "created_at",
                value: new DateTime(2026, 8, 7, 17, 54, 39, 210, DateTimeKind.Utc).AddTicks(2073));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2,
                column: "created_at",
                value: new DateTime(2026, 8, 7, 17, 54, 39, 210, DateTimeKind.Utc).AddTicks(3389));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 3,
                column: "created_at",
                value: new DateTime(2026, 8, 7, 17, 54, 39, 210, DateTimeKind.Utc).AddTicks(3395));

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "id",
                keyValue: 4,
                column: "created_at",
                value: new DateTime(2026, 8, 7, 17, 54, 39, 210, DateTimeKind.Utc).AddTicks(3397));

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "created_at", "password_hash" },
                values: new object[] { new DateTime(2026, 8, 7, 17, 54, 39, 505, DateTimeKind.Utc).AddTicks(8242), "$2a$11$7TV6jX8I6zdEUQF.nJvXxu4Yjin0aRevWzBn2pDZH6zkZRZrQk7TG" });

            migrationBuilder.CreateIndex(
                name: "ix_material_items_processor_id",
                table: "material_items",
                column: "processor_id");

            migrationBuilder.CreateIndex(
                name: "ix_fruit_types_processor_id",
                table: "fruit_types",
                column: "processor_id");

            migrationBuilder.AddForeignKey(
                name: "fk_farm_areas_users_processor_id",
                table: "farm_areas",
                column: "processor_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_fruit_types_users_processor_id",
                table: "fruit_types",
                column: "processor_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_material_items_users_processor_id",
                table: "material_items",
                column: "processor_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_products_fruit_types_fruit_type_id",
                table: "products",
                column: "fruit_type_id",
                principalTable: "fruit_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

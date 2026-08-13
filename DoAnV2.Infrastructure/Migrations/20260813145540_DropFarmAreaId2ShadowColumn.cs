using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnV2.Infrastructure.Migrations
{
    public partial class DropFarmAreaId2ShadowColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa FK constraint trước
            migrationBuilder.DropForeignKey(
                name: "fk_batches_farm_areas_farm_area_id2",
                table: "batches");

            // Xóa index trên cột shadow
            migrationBuilder.DropIndex(
                name: "ix_batches_farm_area_id2",
                table: "batches");

            // Xóa cột shadow
            migrationBuilder.DropColumn(
                name: "farm_area_id2",
                table: "batches");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "farm_area_id2",
                table: "batches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_batches_farm_area_id2",
                table: "batches",
                column: "farm_area_id2");

            migrationBuilder.AddForeignKey(
                name: "fk_batches_farm_areas_farm_area_id2",
                table: "batches",
                column: "farm_area_id2",
                principalTable: "farm_areas",
                principalColumn: "id");
        }
    }
}
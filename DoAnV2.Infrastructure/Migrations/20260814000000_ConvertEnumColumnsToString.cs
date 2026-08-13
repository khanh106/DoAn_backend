using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnV2.Infrastructure.Migrations
{
    /// <summary>
    /// Convert cột enum Batch.BlockchainSyncStatus và BatchWorker.Status
    /// từ int → nvarchar(max) để khớp với cấu hình HasConversion&lt;string&gt;
    /// trong ApplicationDbContext.ConvertEnumsToString.
    ///
    /// Lỗi: "Conversion failed when converting the nvarchar value 'PENDING' to data type int"
    /// nguyên nhân: code mới lưu string "PENDING" nhưng cột DB vẫn là int
    /// (từ migration ban đầu trước khi HasConversion&lt;string&gt; được thêm vào DbContext).
    /// </summary>
    public partial class ConvertEnumColumnsToString : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Batches.BlockchainSyncStatus: int → nvarchar(max)
            // Bước 1: ALTER COLUMN (NULL cho phép convert trước).
            migrationBuilder.AlterColumn<string>(
                name: "blockchain_sync_status",
                table: "batches",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            // Bước 2: Vì ALTER COLUMN từ int → nvarchar đòi hỏi phải NULL trước,
            // ta dùng SQL thủ công để cast giá trị int sang tên enum tương ứng.
            migrationBuilder.Sql(@"
                UPDATE batches
                SET blockchain_sync_status = CASE blockchain_sync_status
                    WHEN 0 THEN 'PENDING'
                    WHEN 1 THEN 'CONFIRMED'
                    WHEN 2 THEN 'FAILED'
                    ELSE 'PENDING'
                END
                WHERE ISNUMERIC(blockchain_sync_status) = 1;
            ");

            // 2. BatchWorkers.Status: int → nvarchar(max)
            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "batch_workers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.Sql(@"
                UPDATE batch_workers
                SET status = CASE status
                    WHEN 0 THEN 'PENDING'
                    WHEN 1 THEN 'ACCEPTED'
                    WHEN 2 THEN 'REJECTED'
                    ELSE 'PENDING'
                END
                WHERE ISNUMERIC(status) = 1;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: nvarchar → int. Chỉ chạy được nếu tất cả giá trị là số.
            migrationBuilder.Sql(@"
                UPDATE batches
                SET blockchain_sync_status = CASE blockchain_sync_status
                    WHEN 'PENDING' THEN '0'
                    WHEN 'CONFIRMED' THEN '1'
                    WHEN 'FAILED' THEN '2'
                    ELSE '0'
                END;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "blockchain_sync_status",
                table: "batches",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.Sql(@"
                UPDATE batch_workers
                SET status = CASE status
                    WHEN 'PENDING' THEN '0'
                    WHEN 'ACCEPTED' THEN '1'
                    WHEN 'REJECTED' THEN '2'
                    ELSE '0'
                END;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "batch_workers",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
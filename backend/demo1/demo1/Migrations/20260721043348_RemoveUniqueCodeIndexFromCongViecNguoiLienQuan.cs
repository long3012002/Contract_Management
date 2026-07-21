using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUniqueCodeIndexFromCongViecNguoiLienQuan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET @exist := (SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = 'CongViecNguoiLienQuans' AND index_name = 'IX_CongViecNguoiLienQuan_Task_User');
                SET @sqlstmt := IF(@exist > 0, 'ALTER TABLE `CongViecNguoiLienQuans` DROP INDEX `IX_CongViecNguoiLienQuan_Task_User`', 'SELECT 1');
                PREPARE stmt FROM @sqlstmt;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                SET @exist := (SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = 'CongViecNguoiLienQuans' AND index_name = 'IX_CongViecNguoiLienQuans_Code' AND non_unique = 0);
                SET @sqlstmt := IF(@exist > 0, 'ALTER TABLE `CongViecNguoiLienQuans` DROP INDEX `IX_CongViecNguoiLienQuans_Code`', 'SELECT 1');
                PREPARE stmt FROM @sqlstmt;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

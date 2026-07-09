using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demo1.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogsPartitioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE `AuditLogs` PARTITION BY RANGE (TO_DAYS(Timestamp)) (
                    PARTITION p_past VALUES LESS THAN (TO_DAYS('2026-01-01')),
                    PARTITION p2026_q1 VALUES LESS THAN (TO_DAYS('2026-04-01')),
                    PARTITION p2026_q2 VALUES LESS THAN (TO_DAYS('2026-07-01')),
                    PARTITION p2026_q3 VALUES LESS THAN (TO_DAYS('2026-10-01')),
                    PARTITION p2026_q4 VALUES LESS THAN (TO_DAYS('2027-01-01')),
                    PARTITION p_future VALUES LESS THAN MAXVALUE
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE `AuditLogs` REMOVE PARTITIONING;
            ");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KopiKala.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyReportTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    report_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_revenue = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total_bookings = table.Column<int>(type: "integer", nullable: false),
                    total_no_shows = table.Column<int>(type: "integer", nullable: false),
                    total_cancelled = table.Column<int>(type: "integer", nullable: false),
                    top_selling_item = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("daily_reports_pkey", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "daily_reports_report_date_key",
                table: "daily_reports",
                column: "report_date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_reports");
        }
    }
}

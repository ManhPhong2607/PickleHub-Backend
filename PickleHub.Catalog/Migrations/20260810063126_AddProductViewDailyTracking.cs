using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleHub.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddProductViewDailyTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_view_daily",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    view_date = table.Column<DateOnly>(type: "date", nullable: false),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_view_daily", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_view_daily_ProductId_view_date",
                table: "product_view_daily",
                columns: new[] { "ProductId", "view_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_view_daily");
        }
    }
}

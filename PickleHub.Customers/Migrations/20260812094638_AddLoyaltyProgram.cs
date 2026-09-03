using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PickleHub.Customers.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyProgram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "total_spent",
                table: "customer",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "customer_spend_ledger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_spend_ledger", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_tier",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    min_spend = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    benefits_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loyalty_tier", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "loyalty_tier",
                columns: new[] { "Id", "benefits_json", "created_at", "discount_percent", "min_spend", "Name", "sort_order", "updated_at" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-0000-0000-000000000001"), "[\"\\u01AFu \\u0111\\u00E3i \\u0111\\u1EB7c quy\\u1EC1n trong c\\u00E1c d\\u1ECBp sinh nh\\u1EADt, ra m\\u1EAFt s\\u1EA3n ph\\u1EA9m m\\u1EDBi v\\u00E0 nh\\u1EEFng s\\u1EF1 ki\\u1EC7n kh\\u00E1c.\",\"H\\u1ED7 tr\\u1EE3 ch\\u0103m s\\u00F3c t\\u1EADn t\\u00ECnh trong su\\u1ED1t qu\\u00E1 tr\\u00ECnh mua h\\u00E0ng.\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 5m, 3000000m, "Rookie", 1, null },
                    { new Guid("a1000000-0000-0000-0000-000000000002"), "[\"\\u01AFu \\u0111\\u00E3i \\u0111\\u1EB7c quy\\u1EC1n trong c\\u00E1c d\\u1ECBp sinh nh\\u1EADt, ra m\\u1EAFt s\\u1EA3n ph\\u1EA9m m\\u1EDBi v\\u00E0 nh\\u1EEFng s\\u1EF1 ki\\u1EC7n kh\\u00E1c.\",\"H\\u1ED7 tr\\u1EE3 ch\\u0103m s\\u00F3c t\\u1EADn t\\u00ECnh trong su\\u1ED1t qu\\u00E1 tr\\u00ECnh mua h\\u00E0ng.\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 7m, 8000000m, "Rally", 2, null },
                    { new Guid("a1000000-0000-0000-0000-000000000003"), "[\"\\u01AFu \\u0111\\u00E3i \\u0111\\u1EB7c quy\\u1EC1n trong c\\u00E1c d\\u1ECBp sinh nh\\u1EADt, ra m\\u1EAFt s\\u1EA3n ph\\u1EA9m m\\u1EDBi v\\u00E0 nh\\u1EEFng s\\u1EF1 ki\\u1EC7n kh\\u00E1c.\",\"H\\u1ED7 tr\\u1EE3 ch\\u0103m s\\u00F3c t\\u1EADn t\\u00ECnh trong su\\u1ED1t qu\\u00E1 tr\\u00ECnh mua h\\u00E0ng.\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10m, 20000000m, "Ace", 3, null },
                    { new Guid("a1000000-0000-0000-0000-000000000004"), "[\"\\u01AFu \\u0111\\u00E3i \\u0111\\u1EB7c quy\\u1EC1n trong c\\u00E1c d\\u1ECBp sinh nh\\u1EADt, ra m\\u1EAFt s\\u1EA3n ph\\u1EA9m m\\u1EDBi v\\u00E0 nh\\u1EEFng s\\u1EF1 ki\\u1EC7n kh\\u00E1c.\",\"H\\u1ED7 tr\\u1EE3 ch\\u0103m s\\u00F3c t\\u1EADn t\\u00ECnh trong su\\u1ED1t qu\\u00E1 tr\\u00ECnh mua h\\u00E0ng.\"]", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 15m, 50000000m, "Champion", 4, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_customer_spend_ledger_OrderId",
                table: "customer_spend_ledger",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_spend_ledger");

            migrationBuilder.DropTable(
                name: "loyalty_tier");

            migrationBuilder.DropColumn(
                name: "total_spent",
                table: "customer");
        }
    }
}

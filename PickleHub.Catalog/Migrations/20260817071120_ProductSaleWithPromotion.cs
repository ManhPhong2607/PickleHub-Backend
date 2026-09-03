using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleHub.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class ProductSaleWithPromotion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "promotion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "promotion_product",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promotion_product_promotion_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "promotion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_is_active_starts_at_ends_at",
                table: "promotion",
                columns: new[] { "is_active", "starts_at", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "IX_promotion_product_ProductId",
                table: "promotion_product",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_product_PromotionId_ProductId",
                table: "promotion_product",
                columns: new[] { "PromotionId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promotion_product");

            migrationBuilder.DropTable(
                name: "promotion");
        }
    }
}

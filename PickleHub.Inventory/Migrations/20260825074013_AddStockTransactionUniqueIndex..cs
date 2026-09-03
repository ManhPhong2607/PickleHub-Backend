using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleHub.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTransactionUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_transaction_inventory_item_id",
                table: "stock_transaction");

            migrationBuilder.AddColumn<int>(
                name: "reserved_quantity",
                table: "inventory_item",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_stock_transaction_idempotency",
                table: "stock_transaction",
                columns: new[] { "inventory_item_id", "Type", "reference_id" },
                unique: true,
                filter: "\"reference_id\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_stock_transaction_idempotency",
                table: "stock_transaction");

            migrationBuilder.DropColumn(
                name: "reserved_quantity",
                table: "inventory_item");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transaction_inventory_item_id",
                table: "stock_transaction",
                column: "inventory_item_id");
        }
    }
}

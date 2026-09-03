using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleHub.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryVersionColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "inventory_item",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "inventory_item");
        }
    }
}

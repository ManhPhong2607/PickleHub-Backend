using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleHub.Inventory.Migrations
{
    /// <inheritdoc />
    public partial class CleanUpRowVersionColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "inventory_item");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

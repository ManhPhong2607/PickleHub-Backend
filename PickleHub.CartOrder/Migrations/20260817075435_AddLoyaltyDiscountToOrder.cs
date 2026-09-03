using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleHub.CartOrder.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyDiscountToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LoyaltyDiscountAmount",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LoyaltyDiscountPercent",
                table: "Orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoyaltyDiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LoyaltyDiscountPercent",
                table: "Orders");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleHub.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "promotion",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "priority",
                table: "promotion");
        }
    }
}

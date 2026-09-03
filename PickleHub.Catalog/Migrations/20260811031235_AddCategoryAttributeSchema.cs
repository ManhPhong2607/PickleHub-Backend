using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleHub.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryAttributeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "attribute_schema_json",
                table: "category",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attribute_schema_json",
                table: "category");
        }
    }
}

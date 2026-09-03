using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleHub.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "brand",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE brand
                SET slug = LOWER(REGEXP_REPLACE(
                    REGEXP_REPLACE(""Name"", '[^a-zA-Z0-9\s-]', '', 'g'),
                    '\s+', '-', 'g'))
                WHERE slug = ''
            ");

            migrationBuilder.CreateIndex(
                name: "IX_brand_slug",
                table: "brand",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_brand_slug",
                table: "brand");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "brand");
        }
    }
}

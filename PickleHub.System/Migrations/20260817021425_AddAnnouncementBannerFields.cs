using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleHub.System.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncementBannerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cta_link",
                table: "site_announcement",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_public_id",
                table: "site_announcement",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "site_announcement",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cta_link",
                table: "site_announcement");

            migrationBuilder.DropColumn(
                name: "image_public_id",
                table: "site_announcement");

            migrationBuilder.DropColumn(
                name: "image_url",
                table: "site_announcement");
        }
    }
}

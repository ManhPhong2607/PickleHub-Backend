using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleHub.Blog.Migrations
{
    /// <inheritdoc />
    public partial class InititalCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_category",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_category", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "post",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    cover_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cover_image_public_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    view_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    seo_title = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    seo_description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    related_product_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post", x => x.Id);
                    table.ForeignKey(
                        name: "FK_post_content_category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "content_category",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_category_slug",
                table: "content_category",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_post_CategoryId",
                table: "post",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_post_slug",
                table: "post",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_post_Status_published_at",
                table: "post",
                columns: new[] { "Status", "published_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "post");

            migrationBuilder.DropTable(
                name: "content_category");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleHub.Authen.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_email_verified",
                table: "user",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "email_verification_token",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UserId1 = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_verification_token", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_verification_token_user_UserId",
                        column: x => x.UserId,
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_email_verification_token_user_UserId1",
                        column: x => x.UserId1,
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_token_Token",
                table: "email_verification_token",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_token_UserId",
                table: "email_verification_token",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_token_UserId1",
                table: "email_verification_token",
                column: "UserId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_verification_token");

            migrationBuilder.DropColumn(
                name: "is_email_verified",
                table: "user");
        }
    }
}

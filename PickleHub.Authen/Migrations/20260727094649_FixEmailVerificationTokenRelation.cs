using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PickleHub.Authen.Migrations
{
    /// <inheritdoc />
    public partial class FixEmailVerificationTokenRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_email_verification_token_user_UserId1",
                table: "email_verification_token");

            migrationBuilder.DropIndex(
                name: "IX_email_verification_token_UserId1",
                table: "email_verification_token");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "email_verification_token");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "email_verification_token",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_token_UserId1",
                table: "email_verification_token",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_email_verification_token_user_UserId1",
                table: "email_verification_token",
                column: "UserId1",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRolesAndBookActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "User");

            migrationBuilder.CreateTable(
                name: "book_activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Details = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_activities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_book_activities_ActorId",
                table: "book_activities",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_book_activities_BookId",
                table: "book_activities",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_book_activities_OccurredAt",
                table: "book_activities",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "book_activities");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "users");
        }
    }
}

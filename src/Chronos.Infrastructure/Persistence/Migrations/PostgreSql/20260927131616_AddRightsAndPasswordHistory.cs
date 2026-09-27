using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Chronos.Infrastructure.Persistence.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class AddRightsAndPasswordHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasswordHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleRights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    RightId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleRights", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Rights",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), "leave.view-own", "View the caller's own leave requests." },
                    { new Guid("11111111-1111-1111-1111-111111111102"), "leave.request", "Submit a new leave request." },
                    { new Guid("11111111-1111-1111-1111-111111111103"), "leave.approve", "Approve or reject a leave request." },
                    { new Guid("11111111-1111-1111-1111-111111111104"), "projects.manage", "Create and manage projects." },
                    { new Guid("11111111-1111-1111-1111-111111111105"), "users.manage", "Promote/demote users within the organization." }
                });

            migrationBuilder.InsertData(
                table: "RoleRights",
                columns: new[] { "Id", "RightId", "Role" },
                values: new object[,]
                {
                    { new Guid("21111111-1111-1111-1111-111111111101"), new Guid("11111111-1111-1111-1111-111111111101"), 0 },
                    { new Guid("21111111-1111-1111-1111-111111111102"), new Guid("11111111-1111-1111-1111-111111111102"), 0 },
                    { new Guid("21111111-1111-1111-1111-111111111103"), new Guid("11111111-1111-1111-1111-111111111101"), 1 },
                    { new Guid("21111111-1111-1111-1111-111111111104"), new Guid("11111111-1111-1111-1111-111111111102"), 1 },
                    { new Guid("21111111-1111-1111-1111-111111111105"), new Guid("11111111-1111-1111-1111-111111111103"), 1 },
                    { new Guid("21111111-1111-1111-1111-111111111106"), new Guid("11111111-1111-1111-1111-111111111101"), 2 },
                    { new Guid("21111111-1111-1111-1111-111111111107"), new Guid("11111111-1111-1111-1111-111111111102"), 2 },
                    { new Guid("21111111-1111-1111-1111-111111111108"), new Guid("11111111-1111-1111-1111-111111111103"), 2 },
                    { new Guid("21111111-1111-1111-1111-111111111109"), new Guid("11111111-1111-1111-1111-111111111104"), 2 },
                    { new Guid("21111111-1111-1111-1111-111111111110"), new Guid("11111111-1111-1111-1111-111111111105"), 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistories_UserId_CreatedAtUtc",
                table: "PasswordHistories",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Rights_Code",
                table: "Rights",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleRights_Role_RightId",
                table: "RoleRights",
                columns: new[] { "Role", "RightId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordHistories");

            migrationBuilder.DropTable(
                name: "Rights");

            migrationBuilder.DropTable(
                name: "RoleRights");
        }
    }
}

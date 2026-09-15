using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAtUtc", "Email", "IsDeleted", "Name", "PasswordHash", "RoleId", "UpdatedAtUtc" },
                values: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 9, 15, 0, 0, 0, 0, DateTimeKind.Utc), "admin@ecommerce2.local", false, "Development Admin", "$2a$11$Br98PLEdMqaE6XU/upGZvuyHlHgimwVIrj5dDbOdvszbgdmKeGIfu", new Guid("11111111-1111-1111-1111-111111111111"), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));
        }
    }
}

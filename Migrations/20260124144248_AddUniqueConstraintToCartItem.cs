using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlogApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToCartItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems");

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "all",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 24, 14, 42, 48, 652, DateTimeKind.Utc).AddTicks(2119));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "blog",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 24, 14, 42, 48, 652, DateTimeKind.Utc).AddTicks(2121));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "feedback",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 24, 14, 42, 48, 652, DateTimeKind.Utc).AddTicks(2122));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "intro",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 24, 14, 42, 48, 652, DateTimeKind.Utc).AddTicks(2123));

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ProductId",
                table: "CartItems",
                columns: new[] { "CartId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_ProductId",
                table: "CartItems");

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "all",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 24, 13, 10, 51, 985, DateTimeKind.Utc).AddTicks(977));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "blog",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 24, 13, 10, 51, 985, DateTimeKind.Utc).AddTicks(979));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "feedback",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 24, 13, 10, 51, 985, DateTimeKind.Utc).AddTicks(979));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "intro",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 24, 13, 10, 51, 985, DateTimeKind.Utc).AddTicks(980));

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");
        }
    }
}

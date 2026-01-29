using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlogApi.Migrations
{
    /// <inheritdoc />
    public partial class AllowNullImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "all",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 29, 11, 22, 55, 665, DateTimeKind.Utc).AddTicks(3016));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "blog",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 29, 11, 22, 55, 665, DateTimeKind.Utc).AddTicks(3018));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "feedback",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 29, 11, 22, 55, 665, DateTimeKind.Utc).AddTicks(3018));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "intro",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 29, 11, 22, 55, 665, DateTimeKind.Utc).AddTicks(3019));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "all",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 27, 3, 41, 9, 767, DateTimeKind.Utc).AddTicks(6765));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "blog",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 27, 3, 41, 9, 767, DateTimeKind.Utc).AddTicks(6768));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "feedback",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 27, 3, 41, 9, 767, DateTimeKind.Utc).AddTicks(6769));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "intro",
                column: "CreatedAt",
                value: new DateTime(2026, 1, 27, 3, 41, 9, 767, DateTimeKind.Utc).AddTicks(6769));
        }
    }
}

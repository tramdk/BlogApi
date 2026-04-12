using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlogApi.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPublicToFileMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "FileMetadata",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "all",
                column: "CreatedAt",
                value: new DateTime(2026, 4, 12, 13, 54, 11, 399, DateTimeKind.Utc).AddTicks(361));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "blog",
                column: "CreatedAt",
                value: new DateTime(2026, 4, 12, 13, 54, 11, 399, DateTimeKind.Utc).AddTicks(363));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "feedback",
                column: "CreatedAt",
                value: new DateTime(2026, 4, 12, 13, 54, 11, 399, DateTimeKind.Utc).AddTicks(364));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "intro",
                column: "CreatedAt",
                value: new DateTime(2026, 4, 12, 13, 54, 11, 399, DateTimeKind.Utc).AddTicks(365));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "FileMetadata");

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
    }
}

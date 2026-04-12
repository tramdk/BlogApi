using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlogApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudinaryFieldsToFileMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicId",
                table: "FileMetadata",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "FileMetadata",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "all",
                column: "CreatedAt",
                value: new DateTime(2026, 4, 12, 14, 13, 20, 571, DateTimeKind.Utc).AddTicks(6596));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "blog",
                column: "CreatedAt",
                value: new DateTime(2026, 4, 12, 14, 13, 20, 571, DateTimeKind.Utc).AddTicks(6598));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "feedback",
                column: "CreatedAt",
                value: new DateTime(2026, 4, 12, 14, 13, 20, 571, DateTimeKind.Utc).AddTicks(6599));

            migrationBuilder.UpdateData(
                table: "PostCategories",
                keyColumn: "Id",
                keyValue: "intro",
                column: "CreatedAt",
                value: new DateTime(2026, 4, 12, 14, 13, 20, 571, DateTimeKind.Utc).AddTicks(6600));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "FileMetadata");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "FileMetadata");

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
    }
}

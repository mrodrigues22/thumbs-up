using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApprooveItApi.Migrations
{
    /// <inheritdoc />
    public partial class ContentFeatureStatusMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "ExtractedAt",
                table: "ContentFeatures",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "AnalysisStatus",
                table: "ContentFeatures",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "ContentFeatures",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAnalyzedAt",
                table: "ContentFeatures",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalysisStatus",
                table: "ContentFeatures");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "ContentFeatures");

            migrationBuilder.DropColumn(
                name: "LastAnalyzedAt",
                table: "ContentFeatures");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExtractedAt",
                table: "ContentFeatures",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}

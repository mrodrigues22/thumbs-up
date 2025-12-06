using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApprooveItApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderToMediaFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "MediaFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
            
            // Backfill Order values for existing records based on UploadedAt timestamp
            migrationBuilder.Sql(@"
                WITH OrderedMedia AS (
                    SELECT 
                        ""Id"",
                        (ROW_NUMBER() OVER (PARTITION BY ""SubmissionId"" ORDER BY ""UploadedAt"") - 1) AS ""NewOrder""
                    FROM ""MediaFiles""
                )
                UPDATE ""MediaFiles""
                SET ""Order"" = om.""NewOrder""
                FROM OrderedMedia om
                WHERE ""MediaFiles"".""Id"" = om.""Id"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "MediaFiles");
        }
    }
}

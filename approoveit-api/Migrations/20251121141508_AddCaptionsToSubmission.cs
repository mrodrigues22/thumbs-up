using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApprooveItApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCaptionsToSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Captions",
                table: "Submissions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Captions",
                table: "Submissions");
        }
    }
}

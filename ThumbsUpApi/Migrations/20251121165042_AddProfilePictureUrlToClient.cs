using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThumbsUpApi.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePictureUrlToClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "Clients",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                table: "Clients");
        }
    }
}

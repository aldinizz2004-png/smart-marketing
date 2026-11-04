using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMarketingPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessSocialGroupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SocialGroupId",
                table: "Businesses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SocialGroupId",
                table: "Businesses");
        }
    }
}

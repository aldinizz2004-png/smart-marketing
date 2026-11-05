using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMarketingPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddImageDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DescribedAt",
                table: "MediaAssets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DescribedByExecutionId",
                table: "MediaAssets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "MediaAssets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityAdvice",
                table: "MediaAssets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityIssuesJson",
                table: "MediaAssets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowsProduct",
                table: "MediaAssets",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisualDescription",
                table: "MediaAssets",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "MediaAssets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_DescribedByExecutionId",
                table: "MediaAssets",
                column: "DescribedByExecutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_MediaAssets_AiExecutions_DescribedByExecutionId",
                table: "MediaAssets",
                column: "DescribedByExecutionId",
                principalTable: "AiExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MediaAssets_AiExecutions_DescribedByExecutionId",
                table: "MediaAssets");

            migrationBuilder.DropIndex(
                name: "IX_MediaAssets_DescribedByExecutionId",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "DescribedAt",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "DescribedByExecutionId",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "QualityAdvice",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "QualityIssuesJson",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "ShowsProduct",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "VisualDescription",
                table: "MediaAssets");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "MediaAssets");
        }
    }
}

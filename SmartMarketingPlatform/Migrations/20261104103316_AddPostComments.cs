using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartMarketingPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddPostComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalCommentId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AuthorExternalId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AuthorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AuthorUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    NeedsReply = table.Column<bool>(type: "bit", nullable: true),
                    SuggestedReply = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AnalyzedByExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReplyBody = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RepliedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RepliedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostComments_AiExecutions_AnalyzedByExecutionId",
                        column: x => x.AnalyzedByExecutionId,
                        principalTable: "AiExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostComments_AppUsers_RepliedByUserId",
                        column: x => x.RepliedByUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostComments_Publications_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "Publications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostComments_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Statuses",
                columns: new[] { "Id", "Category", "Code", "IsActive", "SortOrder", "TranslationKey" },
                values: new object[,]
                {
                    { 1601, "PostComment", "New", true, 1, "status.post_comment.new" },
                    { 1602, "PostComment", "Analyzed", true, 2, "status.post_comment.analyzed" },
                    { 1603, "PostComment", "Replied", true, 3, "status.post_comment.replied" },
                    { 1604, "PostComment", "Ignored", true, 4, "status.post_comment.ignored" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_AnalyzedByExecutionId",
                table: "PostComments",
                column: "AnalyzedByExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_PublicationId_ExternalCommentId",
                table: "PostComments",
                columns: new[] { "PublicationId", "ExternalCommentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_RepliedByUserId",
                table: "PostComments",
                column: "RepliedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_StatusId_PostedAt",
                table: "PostComments",
                columns: new[] { "StatusId", "PostedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostComments");

            migrationBuilder.DeleteData(
                table: "Statuses",
                keyColumn: "Id",
                keyValue: 1601);

            migrationBuilder.DeleteData(
                table: "Statuses",
                keyColumn: "Id",
                keyValue: 1602);

            migrationBuilder.DeleteData(
                table: "Statuses",
                keyColumn: "Id",
                keyValue: 1603);

            migrationBuilder.DeleteData(
                table: "Statuses",
                keyColumn: "Id",
                keyValue: 1604);
        }
    }
}

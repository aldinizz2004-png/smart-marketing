using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartMarketingPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OwnerReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Headline = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    WinsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ConcernsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ActionsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    GeneratedByExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsScheduled = table.Column<bool>(type: "bit", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SentTo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwnerReports_AiExecutions_GeneratedByExecutionId",
                        column: x => x.GeneratedByExecutionId,
                        principalTable: "AiExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OwnerReports_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerReports_BusinessId_CreatedAt",
                table: "OwnerReports",
                columns: new[] { "BusinessId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OwnerReports_GeneratedByExecutionId",
                table: "OwnerReports",
                column: "GeneratedByExecutionId");

            migrationBuilder.CreateIndex(
                name: "UX_OwnerReports_Business_PeriodEnd_Scheduled",
                table: "OwnerReports",
                columns: new[] { "BusinessId", "PeriodEnd" },
                unique: true,
                filter: "[IsScheduled] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwnerReports");
        }
    }
}

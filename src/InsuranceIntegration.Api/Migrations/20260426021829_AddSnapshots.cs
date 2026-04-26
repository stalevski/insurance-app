using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceIntegration.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PolicySnapshots",
                columns: table => new
                {
                    PolicyReference = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    QuoteReference = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UnderwritingYear = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentPhase = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicySnapshots", x => x.PolicyReference);
                });

            migrationBuilder.CreateTable(
                name: "QuoteSnapshots",
                columns: table => new
                {
                    QuoteReference = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PolicyReference = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UnderwritingYear = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentPhase = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsBound = table.Column<bool>(type: "INTEGER", nullable: false),
                    SnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteSnapshots", x => x.QuoteReference);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PolicySnapshots_LastUpdatedUtc",
                table: "PolicySnapshots",
                column: "LastUpdatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PolicySnapshots_ProductCode_UnderwritingYear",
                table: "PolicySnapshots",
                columns: new[] { "ProductCode", "UnderwritingYear" });

            migrationBuilder.CreateIndex(
                name: "IX_PolicySnapshots_QuoteReference",
                table: "PolicySnapshots",
                column: "QuoteReference");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteSnapshots_LastUpdatedUtc",
                table: "QuoteSnapshots",
                column: "LastUpdatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteSnapshots_PolicyReference",
                table: "QuoteSnapshots",
                column: "PolicyReference");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteSnapshots_ProductCode_UnderwritingYear",
                table: "QuoteSnapshots",
                columns: new[] { "ProductCode", "UnderwritingYear" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PolicySnapshots");

            migrationBuilder.DropTable(
                name: "QuoteSnapshots");
        }
    }
}

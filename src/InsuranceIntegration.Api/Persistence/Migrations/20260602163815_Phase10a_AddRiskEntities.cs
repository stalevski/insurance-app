using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceIntegration.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase10a_AddRiskEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuoteId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PolicyReference = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    QuoteReference = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    InsuredName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TransactionType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UnderwritingYear = table.Column<int>(type: "INTEGER", nullable: false),
                    InceptionDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    AnnualPremium = table.Column<decimal>(type: "TEXT", nullable: false),
                    PolicyVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    PriorPolicyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    QuoteReference = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TransactionType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UnderwritingYear = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    AdjustedPremium = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExternalReference = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceSystem = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TransactionType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ClearanceDecision = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AutoCleared = table.Column<bool>(type: "INTEGER", nullable: false),
                    UnderwritingYear = table.Column<int>(type: "INTEGER", nullable: false),
                    BrokerCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    InsuredName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    AdjustedPremium = table.Column<decimal>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Policies_PolicyReference",
                table: "Policies",
                column: "PolicyReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Policies_ProductCode_UnderwritingYear",
                table: "Policies",
                columns: new[] { "ProductCode", "UnderwritingYear" });

            migrationBuilder.CreateIndex(
                name: "IX_Policies_SubmissionId",
                table: "Policies",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_UpdatedAtUtc",
                table: "Policies",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_QuoteReference",
                table: "Quotes",
                column: "QuoteReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_SubmissionId",
                table: "Quotes",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_UpdatedAtUtc",
                table: "Quotes",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ExternalReference",
                table: "Submissions",
                column: "ExternalReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ProductCode_UnderwritingYear",
                table: "Submissions",
                columns: new[] { "ProductCode", "UnderwritingYear" });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_UpdatedAtUtc",
                table: "Submissions",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Policies");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "Submissions");
        }
    }
}

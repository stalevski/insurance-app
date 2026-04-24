using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsuranceIntegration.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InboxMessages",
                columns: table => new
                {
                    Source = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EnvelopeId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    HandlerName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ResultJson = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => new { x.Source, x.EnvelopeId });
                });

            migrationBuilder.CreateTable(
                name: "KnownSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExternalReference = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    InsuredName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ProductCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    UnderwritingYear = table.Column<int>(type: "INTEGER", nullable: false),
                    BrokerCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    RegisteredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AggregateType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AggregateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CausationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DispatchedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DispatchAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.EventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_ProcessedAtUtc",
                table: "InboxMessages",
                column: "ProcessedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_KnownSubmissions_ProductCode_UnderwritingYear",
                table: "KnownSubmissions",
                columns: new[] { "ProductCode", "UnderwritingYear" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_AggregateType_AggregateId",
                table: "OutboxMessages",
                columns: new[] { "AggregateType", "AggregateId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_DispatchedAtUtc_OccurredAtUtc",
                table: "OutboxMessages",
                columns: new[] { "DispatchedAtUtc", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages");

            migrationBuilder.DropTable(
                name: "KnownSubmissions");

            migrationBuilder.DropTable(
                name: "OutboxMessages");
        }
    }
}

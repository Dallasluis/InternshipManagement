using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationAndPrivateDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsResend",
                table: "EmailActionTokens",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AppealPending",
                table: "CompanyProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AppealReviewedAt",
                table: "CompanyProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AppealSubmittedAt",
                table: "CompanyProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasUsedVerificationAppeal",
                table: "CompanyProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "CompanyProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "CompanyProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReverificationRequiredAt",
                table: "CompanyProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionInactiveAt",
                table: "CompanyProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationSubmittedAt",
                table: "CompanyProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FailedLoginWindowStartedAt",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RememberMe",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CompanyVerificationHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyProfileId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CriteriaAssessmentJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsAppeal = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyVerificationHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAccessAudits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReportId = table.Column<int>(type: "int", nullable: true),
                    CompanyProfileId = table.Column<int>(type: "int", nullable: true),
                    AccessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAccessAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentAccessTokens",
                columns: table => new
                {
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    ReportId = table.Column<int>(type: "int", nullable: true),
                    CompanyProfileId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAccessTokens", x => x.TokenHash);
                });

            migrationBuilder.CreateTable(
                name: "PrivateDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    RetentionState = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateDocuments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyVerificationHistories_ChangedByUserId_CreatedAt",
                table: "CompanyVerificationHistories",
                columns: new[] { "ChangedByUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyVerificationHistories_CompanyProfileId_CreatedAt",
                table: "CompanyVerificationHistories",
                columns: new[] { "CompanyProfileId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccessAudits_DocumentId_AccessedAt",
                table: "DocumentAccessAudits",
                columns: new[] { "DocumentId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccessAudits_UserId_AccessedAt",
                table: "DocumentAccessAudits",
                columns: new[] { "UserId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccessTokens_DocumentId_RequestedByUserId",
                table: "DocumentAccessTokens",
                columns: new[] { "DocumentId", "RequestedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccessTokens_ExpiresAt",
                table: "DocumentAccessTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateDocuments_OwnerUserId_RetentionState",
                table: "PrivateDocuments",
                columns: new[] { "OwnerUserId", "RetentionState" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyVerificationHistories");

            migrationBuilder.DropTable(
                name: "DocumentAccessAudits");

            migrationBuilder.DropTable(
                name: "DocumentAccessTokens");

            migrationBuilder.DropTable(
                name: "PrivateDocuments");

            migrationBuilder.DropColumn(
                name: "IsResend",
                table: "EmailActionTokens");

            migrationBuilder.DropColumn(
                name: "AppealPending",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "AppealReviewedAt",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "AppealSubmittedAt",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "HasUsedVerificationAppeal",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "ReverificationRequiredAt",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "SubscriptionInactiveAt",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "VerificationSubmittedAt",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FailedLoginWindowStartedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RememberMe",
                table: "AspNetUsers");
        }
    }
}

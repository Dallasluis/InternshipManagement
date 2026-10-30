using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompletePrivateDocumentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AcademicTranscriptDocumentId",
                table: "StudentProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CertificatesDocumentId",
                table: "StudentProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CoverLetterDocumentId",
                table: "StudentProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdentificationDocumentId",
                table: "StudentProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OtherSupportingDocumentsId",
                table: "StudentProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QualificationDocumentId",
                table: "StudentProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResumeDocumentId",
                table: "StudentProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceDocumentIdsJson",
                table: "CompanyVerificationHistories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusBeforeSuspension",
                table: "CompanyProfiles",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcademicTranscriptDocumentId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "CertificatesDocumentId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "CoverLetterDocumentId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "IdentificationDocumentId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "OtherSupportingDocumentsId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "QualificationDocumentId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "ResumeDocumentId",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "EvidenceDocumentIdsJson",
                table: "CompanyVerificationHistories");

            migrationBuilder.DropColumn(
                name: "StatusBeforeSuspension",
                table: "CompanyProfiles");
        }
    }
}

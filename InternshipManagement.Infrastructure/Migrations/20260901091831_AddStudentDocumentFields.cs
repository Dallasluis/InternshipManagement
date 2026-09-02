using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentDocumentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcademicTranscriptUrl",
                table: "StudentProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificatesUrl",
                table: "StudentProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverLetterUrl",
                table: "StudentProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentificationDocumentUrl",
                table: "StudentProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherSupportingDocumentsUrl",
                table: "StudentProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualificationDocumentUrl",
                table: "StudentProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcademicTranscriptUrl",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "CertificatesUrl",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "CoverLetterUrl",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "IdentificationDocumentUrl",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "OtherSupportingDocumentsUrl",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "QualificationDocumentUrl",
                table: "StudentProfiles");
        }
    }
}

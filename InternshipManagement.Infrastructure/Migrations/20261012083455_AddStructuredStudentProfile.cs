using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredStudentProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Educations_StudentProfiles_StudentProfileId",
                table: "Educations");

            migrationBuilder.DropForeignKey(
                name: "FK_Skills_StudentProfiles_StudentProfileId",
                table: "Skills");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperiences_StudentProfiles_StudentProfileId",
                table: "WorkExperiences");

            migrationBuilder.AddColumn<string>(
                name: "CompletionStatus",
                table: "Educations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportingDocumentUrl",
                table: "Educations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentCertifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentProfileId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssuingOrganization = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CredentialUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificateFileUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentCertifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentCertifications_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentProjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentProfileId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Technologies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepositoryUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentProjects_StudentProfiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "StudentProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentCertifications_StudentProfileId",
                table: "StudentCertifications",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentProjects_StudentProfileId",
                table: "StudentProjects",
                column: "StudentProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Educations_StudentProfiles_StudentProfileId",
                table: "Educations",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Skills_StudentProfiles_StudentProfileId",
                table: "Skills",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperiences_StudentProfiles_StudentProfileId",
                table: "WorkExperiences",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Educations_StudentProfiles_StudentProfileId",
                table: "Educations");

            migrationBuilder.DropForeignKey(
                name: "FK_Skills_StudentProfiles_StudentProfileId",
                table: "Skills");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkExperiences_StudentProfiles_StudentProfileId",
                table: "WorkExperiences");

            migrationBuilder.DropTable(
                name: "StudentCertifications");

            migrationBuilder.DropTable(
                name: "StudentProjects");

            migrationBuilder.DropColumn(
                name: "CompletionStatus",
                table: "Educations");

            migrationBuilder.DropColumn(
                name: "SupportingDocumentUrl",
                table: "Educations");

            migrationBuilder.AddForeignKey(
                name: "FK_Educations_StudentProfiles_StudentProfileId",
                table: "Educations",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Skills_StudentProfiles_StudentProfileId",
                table: "Skills",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkExperiences_StudentProfiles_StudentProfileId",
                table: "WorkExperiences",
                column: "StudentProfileId",
                principalTable: "StudentProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

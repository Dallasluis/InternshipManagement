using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredInternshipRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredQualifications",
                table: "Internships",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredSkills",
                table: "Internships",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredQualifications",
                table: "Internships",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequiredSkills",
                table: "Internships",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredQualifications",
                table: "Internships");

            migrationBuilder.DropColumn(
                name: "PreferredSkills",
                table: "Internships");

            migrationBuilder.DropColumn(
                name: "RequiredQualifications",
                table: "Internships");

            migrationBuilder.DropColumn(
                name: "RequiredSkills",
                table: "Internships");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInternshipWorkArrangement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkArrangement",
                table: "Internships",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "UPDATE [Internships] SET [WorkArrangement] = CASE WHEN [IsRemote] = 1 THEN 1 ELSE 0 END; " +
                "UPDATE [Internships] SET [InternshipType] = 0 WHERE [InternshipType] <> 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkArrangement",
                table: "Internships");
        }
    }
}

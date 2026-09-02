using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationInterviewAndOfferFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InterviewDateTime",
                table: "InternshipApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewLocationOrLink",
                table: "InternshipApplications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewNotes",
                table: "InternshipApplications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewType",
                table: "InternshipApplications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OfferAcceptedAt",
                table: "InternshipApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OfferDeclinedAt",
                table: "InternshipApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfferDetails",
                table: "InternshipApplications",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OfferExpiryDate",
                table: "InternshipApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OfferStartDate",
                table: "InternshipApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OfferStipendAmount",
                table: "InternshipApplications",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterviewDateTime",
                table: "InternshipApplications");

            migrationBuilder.DropColumn(
                name: "InterviewLocationOrLink",
                table: "InternshipApplications");

            migrationBuilder.DropColumn(
                name: "InterviewNotes",
                table: "InternshipApplications");

            migrationBuilder.DropColumn(
                name: "InterviewType",
                table: "InternshipApplications");

            migrationBuilder.DropColumn(
                name: "OfferAcceptedAt",
                table: "InternshipApplications");

            migrationBuilder.DropColumn(
                name: "OfferDeclinedAt",
                table: "InternshipApplications");

            migrationBuilder.DropColumn(
                name: "OfferDetails",
                table: "InternshipApplications");

            migrationBuilder.DropColumn(
                name: "OfferExpiryDate",
                table: "InternshipApplications");

            migrationBuilder.DropColumn(
                name: "OfferStartDate",
                table: "InternshipApplications");

            migrationBuilder.DropColumn(
                name: "OfferStipendAmount",
                table: "InternshipApplications");
        }
    }
}

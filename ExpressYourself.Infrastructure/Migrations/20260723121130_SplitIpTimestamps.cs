using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpressYourself.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitIpTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastUpdated",
                table: "IpAddresses",
                newName: "LastUpdatedAtUtc");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastCheckedAtUtc",
                table: "IpAddresses",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCheckedAtUtc",
                table: "IpAddresses");

            migrationBuilder.RenameColumn(
                name: "LastUpdatedAtUtc",
                table: "IpAddresses",
                newName: "LastUpdated");
        }
    }
}

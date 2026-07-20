using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExpressYourself.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    TwoLetterCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    ThreeLetterCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    CountryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.TwoLetterCode);
                });

            migrationBuilder.CreateTable(
                name: "IpAddresses",
                columns: table => new
                {
                    Address = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    CountryTwoLetterCode = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpAddresses", x => x.Address);
                    table.ForeignKey(
                        name: "FK_IpAddresses_Countries_CountryTwoLetterCode",
                        column: x => x.CountryTwoLetterCode,
                        principalTable: "Countries",
                        principalColumn: "TwoLetterCode",
                        onDelete: ReferentialAction.SetNull);
                });

           
            migrationBuilder.CreateIndex(
                name: "IX_Countries_ThreeLetterCode",
                table: "Countries",
                column: "ThreeLetterCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpAddresses_CountryTwoLetterCode",
                table: "IpAddresses",
                column: "CountryTwoLetterCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IpAddresses");

            migrationBuilder.DropTable(
                name: "Countries");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace University_Agent_System.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "userName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Students",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "approvalCondition",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejectionReason",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "studentGrades_Report1",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "agentContract",
                table: "Agents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    countryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    countryArabic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    countryEnglish = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    active = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.countryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_countryId",
                table: "Students",
                column: "countryId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_countryId",
                table: "Agents",
                column: "countryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Countries_countryId",
                table: "Agents",
                column: "countryId",
                principalTable: "Countries",
                principalColumn: "countryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Countries_countryId",
                table: "Students",
                column: "countryId",
                principalTable: "Countries",
                principalColumn: "countryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Countries_countryId",
                table: "Agents");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Countries_countryId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_Students_countryId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Agents_countryId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "userName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "approvalCondition",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "rejectionReason",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "studentGrades_Report1",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "agentContract",
                table: "Agents");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Students",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}

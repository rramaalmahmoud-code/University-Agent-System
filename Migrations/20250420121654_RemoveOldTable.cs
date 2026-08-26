using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace University_Agent_System.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_Students_Faculties_facultyId",
            //    table: "Students");

            //migrationBuilder.DropForeignKey(
            //    name: "FK_Students_Programs_programId",
            //    table: "Students");

            //migrationBuilder.DropForeignKey(
            //    name: "FK_Students_Semesters_semesterId",
            //    table: "Students");

            migrationBuilder.DropTable(
                name: "Programs");

            migrationBuilder.DropTable(
                name: "Semesters");

            migrationBuilder.DropTable(
                name: "Faculties");

            migrationBuilder.DropTable(
                name: "AcademicYears");

            migrationBuilder.DropIndex(
                name: "IX_Students_facultyId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_programId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_semesterId",
                table: "Students");

            migrationBuilder.RenameColumn(
                name: "programId",
                table: "Students",
                newName: "studentGender");

            migrationBuilder.RenameColumn(
                name: "facultyId",
                table: "Students",
                newName: "major_no");

            migrationBuilder.AddColumn<int>(
                name: "Faculty_no",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "userId",
                table: "Agents",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "contractStartDate",
                table: "Agents",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "contractEndDate",
                table: "Agents",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "active",
                table: "Agents",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "nationalId",
                table: "Agents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Faculty_no",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "nationalId",
                table: "Agents");

            migrationBuilder.RenameColumn(
                name: "studentGender",
                table: "Students",
                newName: "programId");

            migrationBuilder.RenameColumn(
                name: "major_no",
                table: "Students",
                newName: "facultyId");

            migrationBuilder.AlterColumn<int>(
                name: "userId",
                table: "Agents",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "contractStartDate",
                table: "Agents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "contractEndDate",
                table: "Agents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "active",
                table: "Agents",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AcademicYears",
                columns: table => new
                {
                    academicYearId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    active = table.Column<int>(type: "int", nullable: true),
                    year = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYears", x => x.academicYearId);
                });

            migrationBuilder.CreateTable(
                name: "Faculties",
                columns: table => new
                {
                    facultyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    active = table.Column<int>(type: "int", nullable: true),
                    facultyNameArabic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    facultyNameEnglish = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculties", x => x.facultyId);
                });

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    semesterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    yearId = table.Column<int>(type: "int", nullable: true),
                    active = table.Column<int>(type: "int", nullable: true),
                    semesterArabic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    semesterEnglish = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semesters", x => x.semesterId);
                    table.ForeignKey(
                        name: "FK_Semesters_AcademicYears_yearId",
                        column: x => x.yearId,
                        principalTable: "AcademicYears",
                        principalColumn: "academicYearId");
                });

            migrationBuilder.CreateTable(
                name: "Programs",
                columns: table => new
                {
                    programId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    facultyId = table.Column<int>(type: "int", nullable: true),
                    active = table.Column<int>(type: "int", nullable: true),
                    programNameArabic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    programNameEnglish = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programs", x => x.programId);
                    table.ForeignKey(
                        name: "FK_Programs_Faculties_facultyId",
                        column: x => x.facultyId,
                        principalTable: "Faculties",
                        principalColumn: "facultyId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_facultyId",
                table: "Students",
                column: "facultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_programId",
                table: "Students",
                column: "programId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_semesterId",
                table: "Students",
                column: "semesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Programs_facultyId",
                table: "Programs",
                column: "facultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Semesters_yearId",
                table: "Semesters",
                column: "yearId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Faculties_facultyId",
                table: "Students",
                column: "facultyId",
                principalTable: "Faculties",
                principalColumn: "facultyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Programs_programId",
                table: "Students",
                column: "programId",
                principalTable: "Programs",
                principalColumn: "programId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Semesters_semesterId",
                table: "Students",
                column: "semesterId",
                principalTable: "Semesters",
                principalColumn: "semesterId");
        }
    }
}

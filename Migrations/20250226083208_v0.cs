using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace University_Agent_System.Migrations
{
    /// <inheritdoc />
    public partial class v0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicYears",
                columns: table => new
                {
                    academicYearId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    year = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYears", x => x.academicYearId);
                });

            migrationBuilder.CreateTable(
                name: "Degrees",
                columns: table => new
                {
                    degreeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    degreeArabic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    degreeEnglish = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Degrees", x => x.degreeId);
                });

            migrationBuilder.CreateTable(
                name: "Faculties",
                columns: table => new
                {
                    facultyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    facultyNameArabic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    facultyNameEnglish = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculties", x => x.facultyId);
                });

            migrationBuilder.CreateTable(
                name: "Nationalities",
                columns: table => new
                {
                    nationalityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nationalityArabic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    nationalityEnglish = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nationalities", x => x.nationalityId);
                });

            migrationBuilder.CreateTable(
                name: "Programs",
                columns: table => new
                {
                    programId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    programNameArabic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    programNameEnglish = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programs", x => x.programId);
                });

            migrationBuilder.CreateTable(
                name: "Statuses",
                columns: table => new
                {
                    statusId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    statusArabic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    statusEnglish = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statuses", x => x.statusId);
                });

            migrationBuilder.CreateTable(
                name: "UserTypes",
                columns: table => new
                {
                    userTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userTypeArabic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    userTypeEnglish = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTypes", x => x.userTypeId);
                });

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    semesterId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    semesterArabic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    semesterEnglish = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    yearId = table.Column<int>(type: "int", nullable: false)
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
                name: "Users",
                columns: table => new
                {
                    userId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    userNameArabic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    userNameEnglish = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    userTypeId = table.Column<int>(type: "int", nullable: false),
                    userEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    userPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    userPassword = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.userId);
                    table.ForeignKey(
                        name: "FK_Users_UserTypes_userTypeId",
                        column: x => x.userTypeId,
                        principalTable: "UserTypes",
                        principalColumn: "userTypeId");
                });

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    agentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    agentCode = table.Column<int>(type: "int", nullable: false),
                    agentNameArabic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    agentNameEnglish = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    nationalityId = table.Column<int>(type: "int", nullable: false),
                    countryId = table.Column<int>(type: "int", nullable: false),
                    city = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    agentEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    agentIban = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    passowrd = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    agentPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    commission = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    contractStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    contractEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    userId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.agentId);
                    table.ForeignKey(
                        name: "FK_Agents_Nationalities_nationalityId",
                        column: x => x.nationalityId,
                        principalTable: "Nationalities",
                        principalColumn: "nationalityId");
                    table.ForeignKey(
                        name: "FK_Agents_Users_userId",
                        column: x => x.userId,
                        principalTable: "Users",
                        principalColumn: "userId");
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    studentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nationalId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    studentNameArabic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    studentNameEnglish = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    nationalityId = table.Column<int>(type: "int", nullable: false),
                    countryId = table.Column<int>(type: "int", nullable: false),
                    city = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    studentEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    studentPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    studentSchool = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    studentGPA = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    degreeId = table.Column<int>(type: "int", nullable: false),
                    programId = table.Column<int>(type: "int", nullable: false),
                    facultyId = table.Column<int>(type: "int", nullable: false),
                    semesterId = table.Column<int>(type: "int", nullable: false),
                    agentId = table.Column<int>(type: "int", nullable: false),
                    statusId = table.Column<int>(type: "int", nullable: false),
                    studentCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    studentPicture = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    studentProof_of_Identity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    studentHigh_School_Certificate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    studentGrades_Report = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    studentBachelor_Certification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    isTransfer = table.Column<int>(type: "int", nullable: false),
                    isDiploma = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.studentId);
                    table.ForeignKey(
                        name: "FK_Students_Agents_agentId",
                        column: x => x.agentId,
                        principalTable: "Agents",
                        principalColumn: "agentId");
                    table.ForeignKey(
                        name: "FK_Students_Degrees_degreeId",
                        column: x => x.degreeId,
                        principalTable: "Degrees",
                        principalColumn: "degreeId");
                    table.ForeignKey(
                        name: "FK_Students_Faculties_facultyId",
                        column: x => x.facultyId,
                        principalTable: "Faculties",
                        principalColumn: "facultyId");
                    table.ForeignKey(
                        name: "FK_Students_Nationalities_nationalityId",
                        column: x => x.nationalityId,
                        principalTable: "Nationalities",
                        principalColumn: "nationalityId");
                    table.ForeignKey(
                        name: "FK_Students_Programs_programId",
                        column: x => x.programId,
                        principalTable: "Programs",
                        principalColumn: "programId");
                    table.ForeignKey(
                        name: "FK_Students_Semesters_semesterId",
                        column: x => x.semesterId,
                        principalTable: "Semesters",
                        principalColumn: "semesterId");
                    table.ForeignKey(
                        name: "FK_Students_Statuses_statusId",
                        column: x => x.statusId,
                        principalTable: "Statuses",
                        principalColumn: "statusId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_nationalityId",
                table: "Agents",
                column: "nationalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_userId",
                table: "Agents",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_Semesters_yearId",
                table: "Semesters",
                column: "yearId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_agentId",
                table: "Students",
                column: "agentId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_degreeId",
                table: "Students",
                column: "degreeId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_facultyId",
                table: "Students",
                column: "facultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_nationalityId",
                table: "Students",
                column: "nationalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_programId",
                table: "Students",
                column: "programId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_semesterId",
                table: "Students",
                column: "semesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_statusId",
                table: "Students",
                column: "statusId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_userTypeId",
                table: "Users",
                column: "userTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "Degrees");

            migrationBuilder.DropTable(
                name: "Faculties");

            migrationBuilder.DropTable(
                name: "Programs");

            migrationBuilder.DropTable(
                name: "Semesters");

            migrationBuilder.DropTable(
                name: "Statuses");

            migrationBuilder.DropTable(
                name: "Nationalities");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "AcademicYears");

            migrationBuilder.DropTable(
                name: "UserTypes");
        }
    }
}

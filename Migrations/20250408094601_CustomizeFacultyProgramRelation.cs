using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace University_Agent_System.Migrations
{
    /// <inheritdoc />
    public partial class CustomizeFacultyProgramRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Programs_Faculties_facultyId",
                table: "Programs");

            migrationBuilder.AddForeignKey(
                name: "FK_Programs_Faculties_facultyId",
                table: "Programs",
                column: "facultyId",
                principalTable: "Faculties",
                principalColumn: "facultyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Programs_Faculties_facultyId",
                table: "Programs");

            migrationBuilder.AddForeignKey(
                name: "FK_Programs_Faculties_facultyId",
                table: "Programs",
                column: "facultyId",
                principalTable: "Faculties",
                principalColumn: "facultyId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

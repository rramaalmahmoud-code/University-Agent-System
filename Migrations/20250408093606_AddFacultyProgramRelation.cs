using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace University_Agent_System.Migrations
{
    /// <inheritdoc />
    public partial class AddFacultyProgramRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "facultyId",
                table: "Programs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Programs_facultyId",
                table: "Programs",
                column: "facultyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Programs_Faculties_facultyId",
                table: "Programs",
                column: "facultyId",
                principalTable: "Faculties",
                principalColumn: "facultyId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Programs_Faculties_facultyId",
                table: "Programs");

            migrationBuilder.DropIndex(
                name: "IX_Programs_facultyId",
                table: "Programs");

            migrationBuilder.DropColumn(
                name: "facultyId",
                table: "Programs");
        }
    }
}

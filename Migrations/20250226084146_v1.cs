using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace University_Agent_System.Migrations
{
    /// <inheritdoc />
    public partial class v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "active",
                table: "UserTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "active",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "active",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "active",
                table: "Statuses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "active",
                table: "Semesters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "active",
                table: "Programs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "active",
                table: "Nationalities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "active",
                table: "Faculties",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "active",
                table: "Degrees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "active",
                table: "Agents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "active",
                table: "AcademicYears",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "active",
                table: "UserTypes");

            migrationBuilder.DropColumn(
                name: "active",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "active",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "active",
                table: "Statuses");

            migrationBuilder.DropColumn(
                name: "active",
                table: "Semesters");

            migrationBuilder.DropColumn(
                name: "active",
                table: "Programs");

            migrationBuilder.DropColumn(
                name: "active",
                table: "Nationalities");

            migrationBuilder.DropColumn(
                name: "active",
                table: "Faculties");

            migrationBuilder.DropColumn(
                name: "active",
                table: "Degrees");

            migrationBuilder.DropColumn(
                name: "active",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "active",
                table: "AcademicYears");
        }
    }
}

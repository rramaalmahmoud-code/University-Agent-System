using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace University_Agent_System.Migrations
{
    /// <inheritdoc />
    public partial class AddNewColumnToAgents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "agentStatus",
                table: "Agents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "agentStatus",
                table: "Agents");
        }
    }
}

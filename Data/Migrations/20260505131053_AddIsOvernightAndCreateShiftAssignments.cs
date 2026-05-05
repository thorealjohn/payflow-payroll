using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace itpayroll.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsOvernightAndCreateShiftAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOvernight",
                table: "Shifts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOvernight",
                table: "Shifts");
        }
    }
}

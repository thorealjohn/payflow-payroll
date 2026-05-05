using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace itpayroll.Data.Migrations
{
    /// <inheritdoc />
    public partial class Midupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Earnings",
                newName: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Earnings",
                newName: "CreatedDate");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGR.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFatorContabilFromTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FatorContabil",
                table: "Tenant");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FatorContabil",
                table: "Tenant",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false);
        }
    }
}

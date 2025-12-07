using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGR.Api.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class RemoveToleranciaPesoFromReceita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ToleranciaPeso",
                table: "Receita");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ToleranciaPeso",
                table: "Receita",
                type: "numeric",
                nullable: true);
        }
    }
}




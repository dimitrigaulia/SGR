using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGR.Api.Migrations.Tenant
{
    public partial class RemoveIndiceContabilFromFichaTecnica : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backup step intentionally not implemented here; ensure DB backup before applying migration in production
            migrationBuilder.DropColumn(
                name: "IndiceContabil",
                table: "FichaTecnica");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "IndiceContabil",
                table: "FichaTecnica",
                type: "numeric",
                nullable: true);
        }
    }
}

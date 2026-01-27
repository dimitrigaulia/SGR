using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGR.Api.Migrations.Tenant
{
    public partial class RemoveRendimentoPorcoesFromFichaTecnica : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the textual RendimentoPorcoes column (data will be lost).
            migrationBuilder.DropColumn(
                name: "RendimentoPorcoes",
                table: "FichaTecnica");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate the column to allow rollback
            migrationBuilder.AddColumn<string>(
                name: "RendimentoPorcoes",
                table: "FichaTecnica",
                type: "varchar(200)",
                nullable: true);
        }
    }
}
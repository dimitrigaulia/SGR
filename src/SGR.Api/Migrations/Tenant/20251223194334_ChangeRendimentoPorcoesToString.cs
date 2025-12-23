using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGR.Api.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class ChangeRendimentoPorcoesToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RendimentoPorcoes",
                table: "FichaTecnica",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "RendimentoPorcoes",
                table: "FichaTecnica",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SGR.Api.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class InitialTenantCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriaInsumo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    IsAtivo = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioCriacao = table.Column<string>(type: "text", nullable: true),
                    UsuarioAtualizacao = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriaInsumo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoriaReceita",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    IsAtivo = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioCriacao = table.Column<string>(type: "text", nullable: true),
                    UsuarioAtualizacao = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriaReceita", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Perfil",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    IsAtivo = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioCriacao = table.Column<string>(type: "text", nullable: true),
                    UsuarioAtualizacao = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfil", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnidadeMedida",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Sigla = table.Column<string>(type: "text", nullable: false),
                    IsAtivo = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioCriacao = table.Column<string>(type: "text", nullable: true),
                    UsuarioAtualizacao = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadeMedida", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FichaTecnica",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoriaId = table.Column<long>(type: "bigint", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    DescricaoComercial = table.Column<string>(type: "text", nullable: true),
                    CustoTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    CustoPorUnidade = table.Column<decimal>(type: "numeric", nullable: false),
                    RendimentoFinal = table.Column<decimal>(type: "numeric", nullable: true),
                    IndiceContabil = table.Column<decimal>(type: "numeric", nullable: true),
                    PrecoSugeridoVenda = table.Column<decimal>(type: "numeric", nullable: true),
                    ICOperador = table.Column<char>(type: "character(1)", nullable: true),
                    ICValor = table.Column<int>(type: "integer", nullable: true),
                    IPCValor = table.Column<int>(type: "integer", nullable: true),
                    MargemAlvoPercentual = table.Column<decimal>(type: "numeric", nullable: true),
                    IsAtivo = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioCriacao = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioAtualizacao = table.Column<string>(type: "text", nullable: true),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichaTecnica", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FichaTecnica_CategoriaReceita_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "CategoriaReceita",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Receita",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    CategoriaId = table.Column<long>(type: "bigint", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    InstrucoesEmpratamento = table.Column<string>(type: "text", nullable: true),
                    Rendimento = table.Column<decimal>(type: "numeric", nullable: false),
                    PesoPorPorcao = table.Column<decimal>(type: "numeric", nullable: true),
                    ToleranciaPeso = table.Column<decimal>(type: "numeric", nullable: true),
                    FatorRendimento = table.Column<decimal>(type: "numeric", nullable: false),
                    TempoPreparo = table.Column<int>(type: "integer", nullable: true),
                    Versao = table.Column<string>(type: "text", nullable: true),
                    CustoTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    CustoPorPorcao = table.Column<decimal>(type: "numeric", nullable: false),
                    PathImagem = table.Column<string>(type: "text", nullable: true),
                    IsAtivo = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioCriacao = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioAtualizacao = table.Column<string>(type: "text", nullable: true),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receita", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Receita_CategoriaReceita_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "CategoriaReceita",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PerfilId = table.Column<long>(type: "bigint", nullable: false),
                    IsAtivo = table.Column<bool>(type: "boolean", nullable: false),
                    NomeCompleto = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    SenhaHash = table.Column<string>(type: "text", nullable: false),
                    PathImagem = table.Column<string>(type: "text", nullable: true),
                    UsuarioCriacao = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioAtualizacao = table.Column<string>(type: "text", nullable: true),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuario_Perfil_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "Perfil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Insumo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    CategoriaId = table.Column<long>(type: "bigint", nullable: false),
                    UnidadeCompraId = table.Column<long>(type: "bigint", nullable: false),
                    UnidadeUsoId = table.Column<long>(type: "bigint", nullable: false),
                    QuantidadePorEmbalagem = table.Column<decimal>(type: "numeric", nullable: false),
                    CustoUnitario = table.Column<decimal>(type: "numeric", nullable: false),
                    FatorCorrecao = table.Column<decimal>(type: "numeric", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    PathImagem = table.Column<string>(type: "text", nullable: true),
                    IsAtivo = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioCriacao = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioAtualizacao = table.Column<string>(type: "text", nullable: true),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Insumo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Insumo_CategoriaInsumo_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "CategoriaInsumo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Insumo_UnidadeMedida_UnidadeCompraId",
                        column: x => x.UnidadeCompraId,
                        principalTable: "UnidadeMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Insumo_UnidadeMedida_UnidadeUsoId",
                        column: x => x.UnidadeUsoId,
                        principalTable: "UnidadeMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FichaTecnicaCanal",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FichaTecnicaId = table.Column<long>(type: "bigint", nullable: false),
                    Canal = table.Column<string>(type: "text", nullable: false),
                    NomeExibicao = table.Column<string>(type: "text", nullable: true),
                    PrecoVenda = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxaPercentual = table.Column<decimal>(type: "numeric", nullable: true),
                    ComissaoPercentual = table.Column<decimal>(type: "numeric", nullable: true),
                    MargemCalculadaPercentual = table.Column<decimal>(type: "numeric", nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    IsAtivo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichaTecnicaCanal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FichaTecnicaCanal_FichaTecnica_FichaTecnicaId",
                        column: x => x.FichaTecnicaId,
                        principalTable: "FichaTecnica",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FichaTecnicaItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FichaTecnicaId = table.Column<long>(type: "bigint", nullable: false),
                    TipoItem = table.Column<string>(type: "text", nullable: false),
                    ReceitaId = table.Column<long>(type: "bigint", nullable: true),
                    InsumoId = table.Column<long>(type: "bigint", nullable: true),
                    Quantidade = table.Column<decimal>(type: "numeric", nullable: false),
                    UnidadeMedidaId = table.Column<long>(type: "bigint", nullable: false),
                    ExibirComoQB = table.Column<bool>(type: "boolean", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    UsuarioCriacao = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsuarioAtualizacao = table.Column<string>(type: "text", nullable: true),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichaTecnicaItem", x => x.Id);
                    table.CheckConstraint("CK_FichaTecnicaItem_TipoItem", "(\"TipoItem\" = 'Receita' AND \"ReceitaId\" IS NOT NULL AND \"InsumoId\" IS NULL) OR (\"TipoItem\" = 'Insumo' AND \"InsumoId\" IS NOT NULL AND \"ReceitaId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_FichaTecnicaItem_FichaTecnica_FichaTecnicaId",
                        column: x => x.FichaTecnicaId,
                        principalTable: "FichaTecnica",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FichaTecnicaItem_Insumo_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FichaTecnicaItem_Receita_ReceitaId",
                        column: x => x.ReceitaId,
                        principalTable: "Receita",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FichaTecnicaItem_UnidadeMedida_UnidadeMedidaId",
                        column: x => x.UnidadeMedidaId,
                        principalTable: "UnidadeMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReceitaItem",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReceitaId = table.Column<long>(type: "bigint", nullable: false),
                    InsumoId = table.Column<long>(type: "bigint", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric", nullable: false),
                    UnidadeMedidaId = table.Column<long>(type: "bigint", nullable: false),
                    ExibirComoQB = table.Column<bool>(type: "boolean", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Observacoes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceitaItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceitaItem_Insumo_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceitaItem_Receita_ReceitaId",
                        column: x => x.ReceitaId,
                        principalTable: "Receita",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceitaItem_UnidadeMedida_UnidadeMedidaId",
                        column: x => x.UnidadeMedidaId,
                        principalTable: "UnidadeMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriaInsumo_Nome",
                table: "CategoriaInsumo",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FichaTecnica_CategoriaId",
                table: "FichaTecnica",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_FichaTecnicaCanal_FichaTecnicaId",
                table: "FichaTecnicaCanal",
                column: "FichaTecnicaId");

            migrationBuilder.CreateIndex(
                name: "IX_FichaTecnicaItem_FichaTecnicaId",
                table: "FichaTecnicaItem",
                column: "FichaTecnicaId");

            migrationBuilder.CreateIndex(
                name: "IX_FichaTecnicaItem_InsumoId",
                table: "FichaTecnicaItem",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_FichaTecnicaItem_ReceitaId",
                table: "FichaTecnicaItem",
                column: "ReceitaId");

            migrationBuilder.CreateIndex(
                name: "IX_FichaTecnicaItem_UnidadeMedidaId",
                table: "FichaTecnicaItem",
                column: "UnidadeMedidaId");

            migrationBuilder.CreateIndex(
                name: "IX_Insumo_CategoriaId",
                table: "Insumo",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Insumo_UnidadeCompraId",
                table: "Insumo",
                column: "UnidadeCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_Insumo_UnidadeUsoId",
                table: "Insumo",
                column: "UnidadeUsoId");

            migrationBuilder.CreateIndex(
                name: "IX_Receita_CategoriaId",
                table: "Receita",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceitaItem_InsumoId",
                table: "ReceitaItem",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceitaItem_ReceitaId",
                table: "ReceitaItem",
                column: "ReceitaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceitaItem_UnidadeMedidaId",
                table: "ReceitaItem",
                column: "UnidadeMedidaId");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadeMedida_Nome",
                table: "UnidadeMedida",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnidadeMedida_Sigla",
                table: "UnidadeMedida",
                column: "Sigla",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_Email",
                table: "Usuario",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_PerfilId",
                table: "Usuario",
                column: "PerfilId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FichaTecnicaCanal");

            migrationBuilder.DropTable(
                name: "FichaTecnicaItem");

            migrationBuilder.DropTable(
                name: "ReceitaItem");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "FichaTecnica");

            migrationBuilder.DropTable(
                name: "Insumo");

            migrationBuilder.DropTable(
                name: "Receita");

            migrationBuilder.DropTable(
                name: "Perfil");

            migrationBuilder.DropTable(
                name: "CategoriaInsumo");

            migrationBuilder.DropTable(
                name: "UnidadeMedida");

            migrationBuilder.DropTable(
                name: "CategoriaReceita");
        }
    }
}

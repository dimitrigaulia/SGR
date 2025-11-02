# Padrões e Regras do Sistema SGR (Sistema de Gerenciamento de Restaurantes)

## Visão Geral do Sistema

O SGR é um sistema **multitenant** onde cada restaurante possui um banco de dados separado. Quando um novo restaurante é cadastrado, o sistema cria um novo banco de dados replicando a estrutura e dados iniciais de um banco de dados **clone/template**.

## Arquitetura Multitenant

- Cada restaurante possui seu próprio banco de dados isolado
- Existe um banco de dados **clone/template** que serve como base para novos restaurantes
- Ao criar um novo restaurante, o sistema:
  1. Cria um novo banco de dados
  2. Replica a estrutura (tabelas, índices, constraints) do banco clone
  3. Replica os dados iniciais do banco clone
  4. Associa o banco ao restaurante

## Contexto Backoffice/Admin/Config

O primeiro contexto a ser desenvolvido é o **backoffice/admin/config**, que gerencia:
- Restaurantes cadastrados
- Usuários que acessam o backoffice
- Perfis de acesso
- Configurações gerais do sistema

Este contexto possui seu próprio banco de dados compartilhado (não é multitenant), pois gerencia todos os restaurantes.

## Nomenclatura de Banco de Dados

**IMPORTANTE:** As colunas do banco de dados devem seguir o **mesmo padrão das propriedades C#** (.NET moderno). Isso permite que o Entity Framework Core use suas convenções padrão, evitando a necessidade de mapeamento manual via Fluent API.

### Regras para Colunas do Banco:
- **Nomes das colunas = Nomes das propriedades C#** (idênticos)
- PascalCase ou snake_case (conforme configuração do EF Core)
- Sem prefixos de tipo ou entidade
- Nomes descritivos e claros

**Benefício:** Com essa abordagem, não é necessário usar `HasColumnName()` no Fluent API, pois o EF Core consegue mapear automaticamente.

## Regras de Tipos de Dados no C#

### IMPORTANTE - Tipos Numéricos:
- **SEMPRE usar `long` para campos numéricos** (chaves primárias, chaves estrangeiras, códigos)
- **NUNCA usar `int`** - mesmo que o valor seja pequeno
- Exceções apenas em casos muito específicos e justificados

### Mapeamento de Tipos:
| Tipo C# | Uso | Observações |
|---------|-----|-------------|
| `long` | Chaves primárias, chaves estrangeiras, códigos | **SEMPRE usar long ao invés de int** |
| `string` | Textos, nomes, descrições | Com tamanho máximo quando aplicável |
| `string?` | Textos opcionais | Nullable quando o campo pode ser nulo |
| `bool` | Valores booleanos | Usar `bool?` apenas se realmente opcional |
| `DateTime` ou `DateTimeOffset` | Datas e horários | Preferir `DateTimeOffset` para UTC |
| `DateTime?` ou `DateTimeOffset?` | Datas opcionais | Nullable quando aplicável |
| `decimal` | Valores monetários, precisão decimal | Para valores financeiros |
| `byte[]` | Dados binários | Para imagens, arquivos, etc. |

## Padrões de Entidades e Models

### Nomenclatura de Classes (.NET Moderno):
- Entidades do banco: `Usuario`, `Perfil`, `Restaurante` (PascalCase, singular)
- DTOs: `UsuarioDto`, `PerfilDto`, `UsuarioResponse`
- ViewModels/Requests: `LoginRequest`, `LoginResponse`, `CreateUsuarioRequest`, `UpdateUsuarioRequest`
- Interfaces: `IUsuarioService`, `IAuthService` (prefixo I)
- Serviços: `UsuarioService`, `AuthService`
- Controllers: `AuthController`, `UsuarioController`

### Nomenclatura de Propriedades C# (.NET Moderno 2025):

**IMPORTANTE:** Propriedades C# devem seguir o padrão .NET moderno:
- **PascalCase** para todas as propriedades públicas
- **Nomes descritivos e claros** - sem prefixos de tipo
- **Nomes em português** (seguindo o padrão do projeto)
- **Sem abreviações** desnecessárias

**Regras:**
- Chaves primárias: `Id` ou `{Entidade}Id` (ex: `UsuarioId`)
- Chaves estrangeiras: `{Entidade}Id` (ex: `PerfilId`)
- Propriedades de navegação: Nome da entidade no singular ou plural conforme o caso
- Propriedades booleanas: Usar prefixos como `Is`, `Has`, `Can` (ex: `IsActive`, `HasPermission`)

**Exemplos de Nomenclatura:**
- Código/ID: `Id` ou `Codigo`
- Chave estrangeira: `PerfilId`
- Nome completo: `NomeCompleto`
- Login: `Login`
- Senha: `Senha` (ou `PasswordHash`)
- Status: `IsAtivo` ou `Status`
- Data de ação: `DataAcao` ou `DataUltimaAtualizacao`
- Caminho da imagem: `PathImagem` ou `ImagePath`

### Propriedades de Auditoria Padrão:
Todas as entidades principais devem incluir campos de auditoria quando aplicável:
- `CriadoPor` ou `UsuarioCriacao` - Usuário que criou o registro (string ou long)
- `DataCriacao` - Data de criação (DateTime ou DateTimeOffset)
- `AtualizadoPor` ou `UsuarioAtualizacao` - Usuário da última ação (string ou long?)
- `DataAtualizacao` - Data da última ação/modificação (DateTime? ou DateTimeOffset?)

## Padrões de Código Backend (.NET)

### Entity Framework Core:
- Usar Entity Framework Core para acesso a dados
- Como as colunas do BD seguem o mesmo padrão das propriedades C#, o EF Core usa suas convenções padrão
- Configurações adicionais via Fluent API apenas quando necessário (relacionamentos, índices, constraints, etc.)
- Configurar corretamente os tipos `long` nas propriedades
- Configurar tamanhos máximos de strings quando necessário

### Controllers:
- Usar controllers RESTful
- Prefixo de rota: `api/[controller]`
- Retornar DTOs, nunca entidades diretas

### Serviços:
- Criar camada de serviços para lógica de negócio
- Injeção de dependência obrigatória

### Segurança:
- Senhas devem ser hash (BCrypt ou similar)
- Implementar autenticação JWT
- Validar permissões por perfil

## Padrões de Código Frontend (Angular)

### Nomenclatura:
- Serviços: `usuario.service.ts`, `perfil.service.ts`
- Componentes: `login.component.ts`, `usuario-list.component.ts`
- Models/Interfaces: `usuario.model.ts`, `perfil.model.ts`

### Estrutura:
- Usar Angular Material para componentes UI
- Tema dark obrigatório (já configurado)
- Reactive Forms para formulários
- RxJS para programação reativa

### Responsividade:
- **Sistema 100% responsivo** - O sistema deve funcionar perfeitamente em:
  - Celulares (smartphones) - breakpoint mobile
  - Tablets - breakpoint tablet
  - Desktops - breakpoint desktop
- Usar Angular Material BreakpointObserver para detectar tamanhos de tela
- Implementar layout adaptativo (sidebar vira drawer no mobile, menus adaptativos, etc.)
- Testar em diferentes resoluções e dispositivos
- Mobile-first approach quando aplicável
- Garantir que todos os componentes sejam acessíveis e usáveis em telas pequenas

## Banco de Dados

### Connection Strings:
- Backoffice: Banco compartilhado para gestão
- Restaurantes: Banco isolado por restaurante

### Migrations:
- Usar EF Core Migrations para criar e atualizar o esquema do banco de dados
- Nomear migrations com padrão descritivo: `YYYYMMDD_HHMMSS_Descricao`
- As migrations serão geradas automaticamente seguindo o padrão das entidades C#
- As colunas criadas seguirão automaticamente o mesmo padrão das propriedades (Id, NomeCompleto, etc.)

## Estrutura de Pastas Recomendada

### Backend (SGR.Api):
```
src/SGR.Api/
├── Controllers/
│   └── Backoffice/
├── Models/
│   ├── Entities/
│   └── DTOs/
├── Services/
│   ├── Interfaces/
│   └── Implementations/
├── Data/
│   └── ApplicationDbContext.cs
├── Mappings/
└── Helpers/
```

## Observações Importantes

1. **SEMPRE usar `long` para campos numéricos** - Esta é uma regra obrigatória (chaves primárias, estrangeiras, códigos)
2. **Seguir padrão .NET moderno 2025** - PascalCase, nomes descritivos, sem prefixos de tipo nas propriedades
3. **Colunas do BD = Propriedades C#** - As colunas do banco devem ter os mesmos nomes das propriedades (mesmo padrão), evitando mapeamento manual
4. **Nunca expor entidades diretamente** - Sempre usar DTOs nos controllers
5. **Manter auditoria** - Todos os registros importantes devem ter campos de auditoria
6. **Tema dark obrigatório** - Todo o frontend deve seguir o tema escuro
7. **100% Responsivo** - O sistema deve funcionar perfeitamente em celulares, tablets e desktops. Todos os componentes devem ser adaptáveis e testados em diferentes resoluções.

## Exemplo de Entidade Completa (.NET Moderno)

### Usuario (Entidade):
```csharp
public class Usuario
{
    public long Id { get; set; }                          // Coluna BD: Id
    public long PerfilId { get; set; }                    // Coluna BD: PerfilId (FK)
    public bool IsAtivo { get; set; }                     // Coluna BD: IsAtivo
    public string NomeCompleto { get; set; }              // Coluna BD: NomeCompleto
    public string? PathImagem { get; set; }               // Coluna BD: PathImagem
    public string Login { get; set; }                     // Coluna BD: Login
    public string SenhaHash { get; set; }                 // Coluna BD: SenhaHash
    public string? UsuarioAtualizacao { get; set; }       // Coluna BD: UsuarioAtualizacao
    public DateTime? DataAtualizacao { get; set; }        // Coluna BD: DataAtualizacao
    
    // Navegação
    public Perfil Perfil { get; set; }
}
```

### Perfil (Entidade):
```csharp
public class Perfil
{
    public long Id { get; set; }                          // Coluna BD: Id
    public string Nome { get; set; }                      // Coluna BD: Nome
    public bool IsAtivo { get; set; }                     // Coluna BD: IsAtivo
    public string? UsuarioCriacao { get; set; }           // Coluna BD: UsuarioCriacao
    public string? UsuarioAtualizacao { get; set; }       // Coluna BD: UsuarioAtualizacao
    public DateTime DataCriacao { get; set; }             // Coluna BD: DataCriacao
    public DateTime? DataAtualizacao { get; set; }        // Coluna BD: DataAtualizacao
    
    // Navegação
    public ICollection<Usuario> Usuarios { get; set; }
}
```

### Exemplo de Configuração Fluent API (DbContext):
Como as colunas do BD seguem o mesmo padrão das propriedades, o mapeamento é mínimo:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Configuração Usuario
    modelBuilder.Entity<Usuario>(entity =>
    {
        entity.ToTable("Usuario");
        entity.HasKey(e => e.Id);
        
        // Configurar tamanhos máximos
        entity.Property(e => e.NomeCompleto).HasMaxLength(200);
        entity.Property(e => e.PathImagem).HasMaxLength(500);
        entity.Property(e => e.Login).HasMaxLength(100);
        entity.Property(e => e.SenhaHash).HasMaxLength(500);
        entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
        
        // Relacionamento
        entity.HasOne(e => e.Perfil)
              .WithMany(p => p.Usuarios)
              .HasForeignKey(e => e.PerfilId);
    });
    
    // Configuração Perfil
    modelBuilder.Entity<Perfil>(entity =>
    {
        entity.ToTable("Perfil");
        entity.HasKey(e => e.Id);
        
        entity.Property(e => e.Nome).HasMaxLength(100);
        entity.Property(e => e.UsuarioCriacao).HasMaxLength(100);
        entity.Property(e => e.UsuarioAtualizacao).HasMaxLength(100);
    });
}
```

**Nota:** Com essa abordagem, não é necessário usar `HasColumnName()` pois as colunas do BD têm os mesmos nomes das propriedades C#.

---

**Documento criado em:** 2025-01-11  
**Versão:** 2.0  
**Última atualização:** 2025-01-11  
**Atualizado por:** Sistema de Documentação  
**Mudança:** Migrado de padrão húngaro para padrão .NET moderno 2025


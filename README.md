# SGR - Sistema de Gerenciamento de Restaurantes

Sistema completo de gerenciamento multi-tenant desenvolvido com **Angular 20** (frontend) e **ASP.NET Core 9** (backend), implementando arquitetura **Schema per Tenant** no PostgreSQL.

---

## ðŸ“‹ Ãndice

1. [VisÃ£o Geral](#-visÃ£o-geral)
2. [Tecnologias](#-tecnologias)
3. [PrÃ©-requisitos e InstalaÃ§Ã£o](#-prÃ©-requisitos-e-instalaÃ§Ã£o)
4. [ConfiguraÃ§Ã£o](#-configuraÃ§Ã£o)
5. [Arquitetura Multi-Tenant](#-arquitetura-multi-tenant)
6. [Estrutura de Pastas](#-estrutura-de-pastas)
7. [PadrÃµes e ConvenÃ§Ãµes](#-padrÃµes-e-convenÃ§Ãµes)
8. [Funcionalidades Implementadas](#-funcionalidades-implementadas)
9. [Guias de Uso](#-guias-de-uso)
10. [Build e Deploy](#-build-e-deploy)

---

## ðŸŽ¯ VisÃ£o Geral

O **SGR** Ã© uma plataforma completa de gerenciamento que permite:

- **Backoffice**: Sistema administrativo para gerenciar tenants, usuÃ¡rios administrativos e perfis do backoffice
- **Multi-Tenancy**: Cada tenant (restaurante/empresa) possui seu prÃ³prio schema no banco de dados, garantindo isolamento completo de dados
- **AutenticaÃ§Ã£o**: Sistema de autenticaÃ§Ã£o JWT separado para backoffice e tenants
- **CRUD GenÃ©rico**: Sistema padronizado de operaÃ§Ãµes CRUD que facilita a criaÃ§Ã£o de novos mÃ³dulos
- **ValidaÃ§Ãµes**: ValidaÃ§Ã£o de CPF/CNPJ via BrasilApi, validaÃ§Ã£o de dados em tempo real
- **Upload de Arquivos**: Sistema de upload de avatares e imagens
- **Interface Moderna**: Interface responsiva com Angular Material 3 e tema escuro/claro

---

## ðŸ› ï¸ Tecnologias

### Backend
- **.NET 9** - Framework principal
- **ASP.NET Core Web API** - API REST
- **Entity Framework Core** - ORM
- **PostgreSQL** - Banco de dados
- **JWT** - AutenticaÃ§Ã£o
- **BCrypt.Net** - Hash de senhas
- **BrasilApi** - ValidaÃ§Ã£o de CPF/CNPJ e busca de dados de CNPJ

### Frontend
- **Angular 20** - Framework principal
- **Angular Material 3** - Componentes UI
- **RxJS** - ProgramaÃ§Ã£o reativa
- **TypeScript** - Linguagem
- **SCSS** - EstilizaÃ§Ã£o

---

## ðŸ“‹ PrÃ©-requisitos e InstalaÃ§Ã£o

### PrÃ©-requisitos
- **.NET 9 SDK**
- **Node.js 18+** e **npm**
- **PostgreSQL 14+**
- **Angular CLI 20+**

### InstalaÃ§Ã£o

#### Backend
```bash
cd src/SGR.Api
dotnet restore
```

#### Frontend
```bash
cd web
npm install
```

---

## âš™ï¸ ConfiguraÃ§Ã£o

### ConfiguraÃ§Ãµes AutomÃ¡ticas

O sistema possui algumas configuraÃ§Ãµes que sÃ£o aplicadas automaticamente:

- **Migrations**: As migrations do Entity Framework sÃ£o aplicadas automaticamente na inicializaÃ§Ã£o da aplicaÃ§Ã£o
- **InicializaÃ§Ã£o de Dados**: O `DbInitializer` cria automaticamente:
  - Categorias padrÃ£o de tenants (Alimentos, Bebidas, Outros)
  - Perfil "Administrador" no backoffice
  - UsuÃ¡rio padrÃ£o do backoffice (verificar `DbInitializer.cs` para credenciais)

### Backend

1. **Configure a connection string** em `src/SGR.Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "ConfigConnection": "Host=localhost;Port=5432;Database=sgr_config;Username=postgres;Password=sua_senha",
    "TenantsConnection": "Host=localhost;Port=5432;Database=sgr_tenants;Username=postgres;Password=sua_senha"
  }
}
```

2. **Configure o JWT** em `appsettings.json`:
```json
{
  "Jwt": {
    "SecretKey": "sua_chave_secreta_super_segura_aqui",
    "Issuer": "SGR.Api",
    "Audience": "SGR.Frontend",
    "ExpirationMinutes": 60
  }
}
```

3. **Configure o CORS** em `appsettings.json`:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "https://localhost:4200"
    ]
  }
}
```

4. **Execute a API**:
```bash
dotnet run
```

A API estarÃ¡ disponÃ­vel em `http://localhost:5281`.

**Nota**: As migrations sÃ£o aplicadas automaticamente na inicializaÃ§Ã£o da aplicaÃ§Ã£o. O sistema tambÃ©m inicializa automaticamente os dados padrÃ£o (categorias, perfil administrador e usuÃ¡rio padrÃ£o) atravÃ©s do `DbInitializer`.

**Importante**: 
- A porta padrÃ£o da API Ã© `5281` (configurada em `launchSettings.json`)
- Em desenvolvimento, o HTTPS redirection Ã© desabilitado para evitar problemas com CORS
- O OpenAPI estÃ¡ disponÃ­vel apenas em ambiente de desenvolvimento

### Frontend

1. **Configure a URL da API** em `web/src/environments/environment.ts`:
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5281/api'
};
```

2. **Execute o frontend**:
```bash
cd web
ng serve
```

A aplicaÃ§Ã£o estarÃ¡ disponÃ­vel em `http://localhost:4200`.

---

## ðŸ¢ Arquitetura Multi-Tenant

### EstratÃ©gia: Schema per Tenant

O sistema utiliza uma arquitetura **Schema per Tenant** no PostgreSQL, onde cada tenant possui seu prÃ³prio schema, garantindo isolamento completo de dados.

### Bancos de Dados

#### 1. Banco `sgr_config` (Backoffice)
**Schema: `public`**
- `CategoriaTenant` - Categorias de tenants (Alimentos, Bebidas, Outros)
- `Tenant` - Lista de todos os tenants cadastrados
- `BackofficeUsuario` - UsuÃ¡rios administrativos do backoffice
- `BackofficePerfil` - Perfis de acesso do backoffice (ex: Administrador, Digitador)

#### 2. Banco `sgr_tenants` (Dados dos Tenants)
**Schemas dinÃ¢micos: `{subdominio}_{id}`** (ex: `vangoghbar_1`, `restaurante_2`)

Cada schema contÃ©m:
- `Perfil` - Perfis de acesso do tenant (especÃ­ficos de cada tenant, ex: GarÃ§om, Cozinheiro, Gerente)
- `Usuario` - UsuÃ¡rios do tenant (especÃ­ficos de cada tenant)
- `CategoriaInsumo` - Categorias de insumos (ex: Hortifruti, Carne, Bebidas)
- `UnidadeMedida` - Unidades de medida (ex: kg, g, L, mL, unidade)
- `Insumo` - Insumos do restaurante (ingredientes e materiais)
- `CategoriaReceita` - Categorias de receitas (ex: Entrada, Prato Principal, Sobremesa)
- `Receita` - Receitas do restaurante
- `ReceitaItem` - Itens de uma receita (insumos utilizados)

### SeparaÃ§Ã£o de Contextos

**Importante**: O sistema mantÃ©m uma separaÃ§Ã£o clara entre:
- **Backoffice**: UsuÃ¡rios e perfis administrativos do sistema (gerenciam tenants)
- **Tenants**: UsuÃ¡rios e perfis especÃ­ficos de cada tenant (clientes do sistema)

Esta separaÃ§Ã£o permite que:
- Cada tenant tenha perfis personalizados conforme sua necessidade
- O backoffice tenha perfis administrativos padronizados
- Ambos os contextos evoluam independentemente
- Maior seguranÃ§a e isolamento entre contextos

### Fluxo de CriaÃ§Ã£o do Tenant

1. **ValidaÃ§Ãµes**:
   - Validar CNPJ/CPF (formato + dÃ­gitos verificadores) via BrasilApi
   - Validar subdomÃ­nio (apenas letras e nÃºmeros, Ãºnico)
   - Validar dados do admin (nome, email, senha)

2. **Criar Tenant**:
   - Criar registro em `Tenant` (banco `sgr_config`)
   - Gerar `NomeSchema = "{subdominio}_{id}"`

3. **Criar Banco `sgr_tenants`** (se nÃ£o existir):
   - `CREATE DATABASE sgr_tenants;`

4. **Criar Schema do Tenant**:
   - `CREATE SCHEMA {NomeSchema};`

5. **Executar Migrations no Schema**:
   - Criar tabelas: `Perfil`, `Usuario`, `CategoriaInsumo`, `UnidadeMedida`, `Insumo`, `CategoriaReceita`, `Receita`, `ReceitaItem`
   - Inserir unidades de medida padrÃ£o (kg, g, L, mL, un, dz, pct, cx)
   - Inserir categorias de receita padrÃ£o (Entrada, Prato Principal, Sobremesa, Bebida, Acompanhamento, Salada, Sopa, Outros)

6. **Inicializar Dados do Tenant**:
   - Criar Perfil "Administrador" (IsAtivo: true) na tabela `Perfil`
   - Criar Usuario admin (com perfil Administrador) na tabela `Usuario`

### IdentificaÃ§Ã£o do Tenant

#### Em ProduÃ§Ã£o
- Middleware lÃª o header `Host` (ex: `vangoghbar.sgr.com.br`)
- Extrai subdomÃ­nio: `vangoghbar`
- Busca tenant no banco `sgr_config`
- Configura `TenantDbContext` para usar schema do tenant

#### Em Desenvolvimento
- Frontend envia header `X-Tenant-Subdomain` (via combobox no login)
- Middleware lÃª header e identifica tenant
- Configura `TenantDbContext` para usar schema do tenant

---

## ðŸ“ Estrutura de Pastas

### Backend (`src/SGR.Api/`)

```
SGR.Api/
â”œâ”€â”€ Controllers/
â”‚   â”œâ”€â”€ Backoffice/
â”‚   â”‚   â”œâ”€â”€ BaseController.cs          # Controller base genÃ©rico
â”‚   â”‚   â”œâ”€â”€ AuthController.cs          # AutenticaÃ§Ã£o backoffice
â”‚   â”‚   â”œâ”€â”€ UsuariosController.cs      # CRUD usuÃ¡rios backoffice
â”‚   â”‚   â”œâ”€â”€ PerfisController.cs        # CRUD perfis backoffice
â”‚   â”‚   â”œâ”€â”€ TenantsController.cs       # CRUD tenants
â”‚   â”‚   â”œâ”€â”€ CategoriaTenantsController.cs # CRUD categorias
â”‚   â”‚   â””â”€â”€ UploadsController.cs       # Upload de arquivos
â”‚   â””â”€â”€ Tenant/
â”‚       â”œâ”€â”€ BaseController.cs          # Controller base para tenants
â”‚       â”œâ”€â”€ AuthController.cs          # AutenticaÃ§Ã£o tenant
â”‚       â”œâ”€â”€ UsuariosController.cs      # CRUD usuÃ¡rios tenant
â”‚       â”œâ”€â”€ PerfisController.cs        # CRUD perfis tenant
â”‚       â”œâ”€â”€ CategoriasInsumoController.cs  # CRUD categorias de insumo
â”‚       â”œâ”€â”€ UnidadesMedidaController.cs    # CRUD unidades de medida
â”‚       â”œâ”€â”€ InsumosController.cs           # CRUD insumos
â”‚       â”œâ”€â”€ CategoriasReceitaController.cs # CRUD categorias de receita
â”‚       â””â”€â”€ ReceitasController.cs          # CRUD receitas
â”œâ”€â”€ Services/
â”‚   â”œâ”€â”€ Backoffice/
â”‚   â”‚   â”œâ”€â”€ Interfaces/
â”‚   â”‚   â”‚   â”œâ”€â”€ IBackofficeUsuarioService.cs
â”‚   â”‚   â”‚   â””â”€â”€ IBackofficePerfilService.cs
â”‚   â”‚   â””â”€â”€ Implementations/
â”‚   â”‚       â”œâ”€â”€ BackofficeUsuarioService.cs
â”‚   â”‚       â””â”€â”€ BackofficePerfilService.cs
â”‚   â”œâ”€â”€ Tenant/
â”‚   â”‚   â”œâ”€â”€ Interfaces/
â”‚   â”‚   â”‚   â”œâ”€â”€ ITenantUsuarioService.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ ITenantPerfilService.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ ICategoriaInsumoService.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ IUnidadeMedidaService.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ IInsumoService.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ ICategoriaReceitaService.cs
â”‚   â”‚   â”‚   â””â”€â”€ IReceitaService.cs
â”‚   â”‚   â””â”€â”€ Implementations/
â”‚   â”‚       â”œâ”€â”€ TenantUsuarioService.cs
â”‚   â”‚       â”œâ”€â”€ TenantPerfilService.cs
â”‚   â”‚       â”œâ”€â”€ CategoriaInsumoService.cs
â”‚   â”‚       â”œâ”€â”€ UnidadeMedidaService.cs
â”‚   â”‚       â””â”€â”€ InsumoService.cs
â”‚   â”œâ”€â”€ Common/
â”‚   â”‚   â””â”€â”€ BaseService.cs             # Service base genÃ©rico
â”‚   â”œâ”€â”€ Interfaces/
â”‚   â”‚   â”œâ”€â”€ IBaseService.cs            # Interface base genÃ©rica
â”‚   â”‚   â”œâ”€â”€ ITenantService.cs
â”‚   â”‚   â”œâ”€â”€ IAuthService.cs
â”‚   â”‚   â”œâ”€â”€ ITenantAuthService.cs
â”‚   â”‚   â”œâ”€â”€ ICpfCnpjValidationService.cs
â”‚   â”‚   â””â”€â”€ ICnpjDataService.cs
â”‚   â””â”€â”€ Implementations/
â”‚       â”œâ”€â”€ TenantService.cs
â”‚       â”œâ”€â”€ AuthService.cs
â”‚       â”œâ”€â”€ TenantAuthService.cs
â”‚       â”œâ”€â”€ CpfCnpjValidationService.cs
â”‚       â””â”€â”€ CnpjDataService.cs
â”œâ”€â”€ Models/
â”‚   â”œâ”€â”€ Backoffice/
â”‚   â”‚   â”œâ”€â”€ Entities/
â”‚   â”‚   â”‚   â”œâ”€â”€ BackofficeUsuario.cs
â”‚   â”‚   â”‚   â””â”€â”€ BackofficePerfil.cs
â”‚   â”‚   â””â”€â”€ DTOs/
â”‚   â”‚       â”œâ”€â”€ BackofficeUsuarioDto.cs
â”‚   â”‚       â”œâ”€â”€ BackofficePerfilDto.cs
â”‚   â”‚       â”œâ”€â”€ CreateBackofficeUsuarioRequest.cs
â”‚   â”‚       â”œâ”€â”€ UpdateBackofficeUsuarioRequest.cs
â”‚   â”‚       â”œâ”€â”€ CreateBackofficePerfilRequest.cs
â”‚   â”‚       â””â”€â”€ UpdateBackofficePerfilRequest.cs
â”‚   â”œâ”€â”€ Tenant/
â”‚   â”‚   â”œâ”€â”€ Entities/
â”‚   â”‚   â”‚   â”œâ”€â”€ TenantUsuario.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ TenantPerfil.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ CategoriaInsumo.cs
â”‚   â”‚   â”‚   â”œâ”€â”€ UnidadeMedida.cs
â”‚   â”‚   â”‚   â””â”€â”€ Insumo.cs
â”‚   â”‚   â””â”€â”€ DTOs/
â”‚   â”‚       â”œâ”€â”€ TenantUsuarioDto.cs
â”‚   â”‚       â”œâ”€â”€ TenantPerfilDto.cs
â”‚   â”‚       â”œâ”€â”€ CategoriaInsumoDto.cs
â”‚   â”‚       â”œâ”€â”€ UnidadeMedidaDto.cs
â”‚   â”‚       â”œâ”€â”€ InsumoDto.cs
â”‚   â”‚       â”œâ”€â”€ CreateTenantUsuarioRequest.cs
â”‚   â”‚       â”œâ”€â”€ UpdateTenantUsuarioRequest.cs
â”‚   â”‚       â”œâ”€â”€ CreateTenantPerfilRequest.cs
â”‚   â”‚       â”œâ”€â”€ UpdateTenantPerfilRequest.cs
â”‚   â”‚       â”œâ”€â”€ CreateCategoriaInsumoRequest.cs
â”‚   â”‚       â”œâ”€â”€ UpdateCategoriaInsumoRequest.cs
â”‚   â”‚       â”œâ”€â”€ CreateUnidadeMedidaRequest.cs
â”‚   â”‚       â”œâ”€â”€ UpdateUnidadeMedidaRequest.cs
â”‚   â”‚       â”œâ”€â”€ CreateInsumoRequest.cs
â”‚   â”‚       â””â”€â”€ UpdateInsumoRequest.cs
â”‚   â”œâ”€â”€ Entities/
â”‚   â”‚   â”œâ”€â”€ Tenant.cs
â”‚   â”‚   â””â”€â”€ CategoriaTenant.cs
â”‚   â””â”€â”€ DTOs/
â”‚       â”œâ”€â”€ TenantDto.cs
â”‚       â”œâ”€â”€ CreateTenantRequest.cs
â”‚       â”œâ”€â”€ UpdateTenantRequest.cs
â”‚       â”œâ”€â”€ CategoriaTenantDto.cs
â”‚       â”œâ”€â”€ CnpjDataResponse.cs
â”‚       â”œâ”€â”€ LoginRequest.cs
â”‚       â”œâ”€â”€ LoginResponse.cs
â”‚       â””â”€â”€ PagedResult.cs
â”œâ”€â”€ Data/
â”‚   â”œâ”€â”€ ApplicationDbContext.cs        # Contexto sgr_config
â”‚   â”œâ”€â”€ TenantDbContext.cs             # Contexto sgr_tenants (schema dinÃ¢mico)
â”‚   â””â”€â”€ DbInitializer.cs               # Inicializador do banco
â”œâ”€â”€ Extensions/
â”‚   â””â”€â”€ ServiceCollectionExtensions.cs # Extension methods para DI
â”œâ”€â”€ Middleware/
â”‚   â”œâ”€â”€ ExceptionHandlingMiddleware.cs # Tratamento global de exceÃ§Ãµes
â”‚   â””â”€â”€ TenantIdentificationMiddleware.cs # IdentificaÃ§Ã£o do tenant
â”œâ”€â”€ Exceptions/
â”‚   â”œâ”€â”€ BusinessException.cs           # ExceÃ§Ã£o de negÃ³cio
â”‚   â””â”€â”€ NotFoundException.cs           # ExceÃ§Ã£o de nÃ£o encontrado
â”œâ”€â”€ Migrations/                        # Migrations do EF Core
â”œâ”€â”€ wwwroot/                          # Arquivos estÃ¡ticos
â”‚   â””â”€â”€ avatars/                      # Avatares dos usuÃ¡rios
â””â”€â”€ Program.cs                         # ConfiguraÃ§Ã£o da aplicaÃ§Ã£o
```

### Frontend (`web/src/app/`)

```
app/
â”œâ”€â”€ backoffice/                        # Backoffice
â”‚   â”œâ”€â”€ components/
â”‚   â”‚   â”œâ”€â”€ listagens/                # Componentes de listagem
â”‚   â”‚   â”‚   â”œâ”€â”€ usuario/
â”‚   â”‚   â”‚   â”œâ”€â”€ perfil/
â”‚   â”‚   â”‚   â””â”€â”€ tenants/
â”‚   â”‚   â””â”€â”€ cadastros/                # Componentes de formulÃ¡rios
â”‚   â”‚       â”œâ”€â”€ usuario/
â”‚   â”‚       â”œâ”€â”€ perfil/
â”‚   â”‚       â””â”€â”€ tenants/
â”‚   â””â”€â”€ login/                        # Login do backoffice
â”‚       â””â”€â”€ backoffice-login.component.*
â”œâ”€â”€ tenant/                            # Tenant
â”‚   â”œâ”€â”€ components/
â”‚   â”‚   â”œâ”€â”€ listagens/                # Componentes de listagem
â”‚   â”‚   â”‚   â”œâ”€â”€ usuario/
â”‚   â”‚   â”‚   â”œâ”€â”€ perfil/
â”‚   â”‚   â”‚   â”œâ”€â”€ categoria-insumo/
â”‚   â”‚   â”‚   â”œâ”€â”€ unidade-medida/
â”‚   â”‚   â”‚   â””â”€â”€ insumo/
â”‚   â”‚   â””â”€â”€ cadastros/                # Componentes de formulÃ¡rios
â”‚   â”‚       â”œâ”€â”€ usuario/
â”‚   â”‚       â”œâ”€â”€ perfil/
â”‚   â”‚       â”œâ”€â”€ categoria-insumo/
â”‚   â”‚       â”œâ”€â”€ unidade-medida/
â”‚   â”‚       â””â”€â”€ insumo/
â”‚   â””â”€â”€ login/                        # Login do tenant
â”‚       â””â”€â”€ tenant-login.component.*
â”œâ”€â”€ core/                              # Funcionalidades core
â”‚   â”œâ”€â”€ guards/
â”‚   â”‚   â”œâ”€â”€ auth.guard.ts            # Guard de autenticaÃ§Ã£o
â”‚   â”‚   â””â”€â”€ state.guard.ts           # Guard de estado
â”‚   â”œâ”€â”€ interceptors/
â”‚   â”‚   â”œâ”€â”€ auth.interceptor.ts      # Interceptor de autenticaÃ§Ã£o
â”‚   â”‚   â”œâ”€â”€ error.interceptor.ts     # Interceptor de erros
â”‚   â”‚   â””â”€â”€ tenant.interceptor.ts    # Interceptor de tenant
â”‚   â”œâ”€â”€ services/
â”‚   â”‚   â”œâ”€â”€ auth.service.ts          # Service de autenticaÃ§Ã£o
â”‚   â”‚   â”œâ”€â”€ toast.service.ts         # Service de notificaÃ§Ãµes
â”‚   â”‚   â”œâ”€â”€ confirmation.service.ts  # Service de confirmaÃ§Ãµes
â”‚   â”‚   â”œâ”€â”€ api.service.ts           # Service base da API
â”‚   â”‚   â””â”€â”€ layout.service.ts        # Service de layout
â”‚   â”œâ”€â”€ utils/
â”‚   â”‚   â”œâ”€â”€ mask.utils.ts            # UtilitÃ¡rios de mÃ¡scara
â”‚   â”‚   â””â”€â”€ subdomain.utils.ts       # UtilitÃ¡rios de subdomÃ­nio
â”‚   â””â”€â”€ models/
â”‚       â”œâ”€â”€ auth.model.ts
â”‚       â””â”€â”€ menu-item.model.ts
â”œâ”€â”€ shared/                            # Componentes compartilhados
â”‚   â””â”€â”€ components/
â”‚       â”œâ”€â”€ loading/
â”‚       â”‚   â””â”€â”€ loading.component.*
â”‚       â””â”€â”€ confirmation-dialog/       # DiÃ¡logo de confirmaÃ§Ã£o
â”‚           â””â”€â”€ confirmation-dialog.component.*
â”œâ”€â”€ features/                          # Services por feature
â”‚   â”œâ”€â”€ usuarios/
â”‚   â”‚   â””â”€â”€ services/
â”‚   â”‚       â”œâ”€â”€ usuario.service.ts
â”‚   â”‚       â””â”€â”€ upload.service.ts
â”‚   â”œâ”€â”€ perfis/
â”‚   â”‚   â””â”€â”€ services/
â”‚   â”‚       â””â”€â”€ perfil.service.ts
â”‚   â”œâ”€â”€ tenants/
â”‚   â”‚   â””â”€â”€ services/
â”‚   â”‚       â”œâ”€â”€ tenant.service.ts
â”‚   â”‚       â””â”€â”€ categoria-tenant.service.ts
â”‚   â”œâ”€â”€ tenant-usuarios/
â”‚   â”‚   â””â”€â”€ services/
â”‚   â”‚       â””â”€â”€ tenant-usuario.service.ts
â”‚   â”œâ”€â”€ tenant-perfis/
â”‚   â”‚   â””â”€â”€ services/
â”‚   â”‚       â””â”€â”€ tenant-perfil.service.ts
â”‚   â”œâ”€â”€ tenant-categorias-insumo/
â”‚   â”‚   â””â”€â”€ services/
â”‚   â”‚       â””â”€â”€ categoria-insumo.service.ts
â”‚   â”œâ”€â”€ tenant-unidades-medida/
â”‚   â”‚   â””â”€â”€ services/
â”‚   â”‚       â””â”€â”€ unidade-medida.service.ts
â”‚   â””â”€â”€ tenant-insumos/
â”‚       â””â”€â”€ services/
â”‚           â””â”€â”€ insumo.service.ts
â”œâ”€â”€ shell/                             # Layout principal
â”‚   â”œâ”€â”€ shell.component.ts
â”‚   â”œâ”€â”€ shell.component.html
â”‚   â””â”€â”€ shell.component.scss
â”œâ”€â”€ app.routes.ts                      # ConfiguraÃ§Ã£o de rotas
â””â”€â”€ app.config.ts                      # ConfiguraÃ§Ã£o da aplicaÃ§Ã£o
```

---

## ðŸ“ PadrÃµes e ConvenÃ§Ãµes

### Backend (.NET C#)

#### 1. Controllers

**PadrÃ£o**: Herdar de `BaseController` para operaÃ§Ãµes CRUD padrÃ£o.

```csharp
[ApiController]
[Route("api/backoffice/[controller]")]
[Authorize]
public class MinhaEntidadeController 
    : BaseController<IMinhaEntidadeService, MinhaEntidadeDto, CreateMinhaEntidadeRequest, UpdateMinhaEntidadeRequest>
{
    public MinhaEntidadeController(
        IMinhaEntidadeService service, 
        ILogger<MinhaEntidadeController> logger) 
        : base(service, logger)
    {
    }

    // MÃ©todos especÃ­ficos podem ser adicionados aqui
    // Os mÃ©todos CRUD padrÃ£o (GetAll, GetById, Create, Update, Delete) 
    // jÃ¡ estÃ£o disponÃ­veis via BaseController
}
```

#### 2. Services

**PadrÃ£o**: Herdar de `BaseService` (em `Services/Common/`) e implementar mÃ©todos abstratos.

**Para Backoffice** (usa `ApplicationDbContext`):
```csharp
public interface IBackofficeMinhaEntidadeService 
    : IBaseService<BackofficeMinhaEntidade, BackofficeMinhaEntidadeDto, CreateBackofficeMinhaEntidadeRequest, UpdateBackofficeMinhaEntidadeRequest>
{
}

public class BackofficeMinhaEntidadeService 
    : BaseService<ApplicationDbContext, BackofficeMinhaEntidade, BackofficeMinhaEntidadeDto, CreateBackofficeMinhaEntidadeRequest, UpdateBackofficeMinhaEntidadeRequest>,
      IBackofficeMinhaEntidadeService
{
    public BackofficeMinhaEntidadeService(
        ApplicationDbContext context, 
        ILogger<BackofficeMinhaEntidadeService> logger) 
        : base(context, logger)
    {
    }

    protected override Expression<Func<BackofficeMinhaEntidade, BackofficeMinhaEntidadeDto>> MapToDto()
    {
        return e => new BackofficeMinhaEntidadeDto
        {
            Id = e.Id,
            Nome = e.Nome,
            // ... outros campos
        };
    }

    protected override BackofficeMinhaEntidade MapToEntity(CreateBackofficeMinhaEntidadeRequest request)
    {
        return new BackofficeMinhaEntidade
        {
            Nome = request.Nome,
            // ... outros campos
        };
    }

    protected override void UpdateEntity(BackofficeMinhaEntidade entity, UpdateBackofficeMinhaEntidadeRequest request)
    {
        entity.Nome = request.Nome;
        // ... outros campos
    }

    // Opcional: Customizar busca
    protected override IQueryable<BackofficeMinhaEntidade> ApplySearch(IQueryable<BackofficeMinhaEntidade> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        return query.Where(e => e.Nome.Contains(search));
    }
}
```

**Para Tenant** (usa `TenantDbContext`):
```csharp
public interface ITenantMinhaEntidadeService 
    : IBaseService<TenantMinhaEntidade, TenantMinhaEntidadeDto, CreateTenantMinhaEntidadeRequest, UpdateTenantMinhaEntidadeRequest>
{
}

public class TenantMinhaEntidadeService 
    : BaseService<TenantDbContext, TenantMinhaEntidade, TenantMinhaEntidadeDto, CreateTenantMinhaEntidadeRequest, UpdateTenantMinhaEntidadeRequest>,
      ITenantMinhaEntidadeService
{
    public TenantMinhaEntidadeService(
        TenantDbContext context, 
        ILogger<TenantMinhaEntidadeService> logger) 
        : base(context, logger)
    {
    }

    protected override Expression<Func<TenantMinhaEntidade, TenantMinhaEntidadeDto>> MapToDto()
    {
        return e => new TenantMinhaEntidadeDto
        {
            Id = e.Id,
            Nome = e.Nome,
            // ... outros campos
        };
    }

    protected override TenantMinhaEntidade MapToEntity(CreateTenantMinhaEntidadeRequest request)
    {
        return new TenantMinhaEntidade
        {
            Nome = request.Nome,
            // ... outros campos
        };
    }

    protected override void UpdateEntity(TenantMinhaEntidade entity, UpdateTenantMinhaEntidadeRequest request)
    {
        entity.Nome = request.Nome;
        // ... outros campos
    }

    // Opcional: Customizar busca
    protected override IQueryable<TenantMinhaEntidade> ApplySearch(IQueryable<TenantMinhaEntidade> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        return query.Where(e => e.Nome.Contains(search));
    }
}
```

#### 3. DTOs

**PadrÃ£o**: Usar Data Annotations para validaÃ§Ã£o.

```csharp
public class CreateMinhaEntidadeRequest
{
    [Required(ErrorMessage = "O nome Ã© obrigatÃ³rio")]
    [MaxLength(100, ErrorMessage = "O nome deve ter no mÃ¡ximo 100 caracteres")]
    public string Nome { get; set; } = string.Empty;
}

public class MinhaEntidadeDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public string? UsuarioCriacao { get; set; }
}
```

#### 4. Entidades

- Campos de auditoria: `UsuarioCriacao`, `DataCriacao`, `UsuarioAtualizacao`, `DataAtualizacao`
- Usar `[MaxLength]` para strings
- Configurar relacionamentos no `OnModelCreating`

#### 5. Registro de Services

**PadrÃ£o**: Registrar no `ServiceCollectionExtensions.cs`.

```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    // Backoffice Services
    services.AddScoped<IBackofficeMinhaEntidadeService, BackofficeMinhaEntidadeService>();
    
    // Tenant Services
    services.AddScoped<ITenantMinhaEntidadeService, TenantMinhaEntidadeService>();
    
    return services;
}
```

### Frontend (Angular 20)

#### 1. Componentes

**PadrÃ£o**: Todos os componentes sÃ£o standalone com `OnPush` change detection.

```typescript
import { Component, ChangeDetectionStrategy, signal, computed, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-example',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './example.component.html',
  styleUrls: ['./example.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush // âœ… Sempre usar OnPush
})
export class ExampleComponent {
  private destroyRef = inject(DestroyRef);
  private service = inject(ExampleService);
  
  readonly isLoading = signal(false);
  readonly data = signal<Item[]>([]);
  
  constructor() {
    this.service.getData()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => this.data.set(data)
      });
  }
}
```

#### 2. Sintaxe Angular 20

**âš ï¸ Diretivas Antigas (DEPRECIADAS)**: `*ngIf`, `*ngFor`, `*ngSwitch`

**âœ… Nova Sintaxe**:

```html
<!-- @if (substitui *ngIf) -->
@if (isLoading()) {
  <app-loading></app-loading>
} @else {
  <div>ConteÃºdo</div>
}

<!-- @for (substitui *ngFor) -->
@for (item of items(); track item.id) {
  <div>{{ item.name }}</div>
} @empty {
  <div>Nenhum item encontrado</div>
}

<!-- @switch (substitui *ngSwitch) -->
@switch (status) {
  @case ('loading') {
    <div>Carregando...</div>
  }
  @case ('success') {
    <div>Sucesso!</div>
  }
  @default {
    <div>PadrÃ£o</div>
  }
}
```

**VariÃ¡veis disponÃ­veis no `@for`**:
- `$index` - Ã­ndice do item
- `$first` - primeiro item
- `$last` - Ãºltimo item
- `$even` - Ã­ndice par
- `$odd` - Ã­ndice Ã­mpar
- `$count` - total de itens

#### 3. Template-Driven Forms

**PadrÃ£o**: Usar Template-Driven Forms para formulÃ¡rios simples.

```html
<form #f="ngForm" (ngSubmit)="save()">
  <mat-form-field appearance="outline" class="form-field-spacing">
    <mat-label>Nome</mat-label>
    <input matInput name="nome" [(ngModel)]="model.nome" required />
    @if (f.controls['nome']?.errors?.['required'] && f.controls['nome']?.touched) {
      <mat-error>Nome Ã© obrigatÃ³rio</mat-error>
    }
  </mat-form-field>
</form>
```

#### 4. Classes Globais de FormulÃ¡rios

Todas as classes abaixo estÃ£o disponÃ­veis globalmente em `styles.scss`:

- **`.form-container`** - Container principal do formulÃ¡rio
  - Padding: 16px (12px no mobile)
  - Max-width: 800px
  - Centralizado automaticamente

- **`.form-title`** - TÃ­tulo do formulÃ¡rio
  - Font-size: 1.75rem (1.5rem tablet, 1.25rem mobile)
  - Margin-bottom: 32px (24px tablet, 20px mobile)

- **`.form-field-spacing`** - EspaÃ§amento entre campos
  - Width: 100%
  - Margin-bottom: 24px (20px tablet, 16px mobile)

- **`.form-section`** - Agrupar seÃ§Ãµes relacionadas
  - Margin-bottom: 32px

- **`.form-actions`** - Container para botÃµes
  - Display: flex
  - Gap: 12px (8px no mobile)
  - Justify-content: flex-end
  - Margin-top: 32px (24px no mobile)

- **`.form-toggle-wrapper`** - Wrapper para `mat-slide-toggle`
  - Margin-bottom: 24px (20px tablet, 16px mobile)

- **`.form-error`** - Mensagem de erro
  - Cor: var(--mat-error)
  - Background: var(--mat-error-container)
  - Padding: 12px 16px

**Exemplo completo**:
```html
<div class="form-container">
  <h2 class="form-title">Cadastro de Nova Entidade</h2>
  
  <form #f="ngForm" (ngSubmit)="save()">
    <mat-form-field appearance="outline" class="form-field-spacing">
      <mat-label>Nome</mat-label>
      <input matInput name="nome" [(ngModel)]="model.nome" required />
    </mat-form-field>
    
    <div class="form-toggle-wrapper">
      <mat-slide-toggle name="isAtivo" [(ngModel)]="model.isAtivo">Ativo</mat-slide-toggle>
    </div>
    
    <div class="form-actions">
      <button mat-stroked-button type="button" routerLink="/lista">Voltar</button>
      <button mat-raised-button color="primary" type="submit">Salvar</button>
    </div>
  </form>
</div>
```

#### 5. Services

**PadrÃ£o**: Services injetÃ¡veis com mÃ©todos tipados.

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class MinhaEntidadeService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;
  private readonly baseUrl = `${this.apiUrl}/backoffice/minhaentidade`;

  getAll(search?: string, page: number = 1, pageSize: number = 10): Observable<PagedResult<MinhaEntidadeDto>> {
    let params = new HttpParams().set('page', page.toString()).set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<MinhaEntidadeDto>>(this.baseUrl, { params });
  }

  getById(id: number): Observable<MinhaEntidadeDto> {
    return this.http.get<MinhaEntidadeDto>(`${this.baseUrl}/${id}`);
  }

  create(request: CreateMinhaEntidadeRequest): Observable<MinhaEntidadeDto> {
    return this.http.post<MinhaEntidadeDto>(this.baseUrl, request);
  }

  update(id: number, request: UpdateMinhaEntidadeRequest): Observable<MinhaEntidadeDto> {
    return this.http.put<MinhaEntidadeDto>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
```

#### 6. Nomenclatura

- **Componentes**: kebab-case (ex: `user-form.component.ts`), classe PascalCase (ex: `UserFormComponent`)
- **Services**: kebab-case (ex: `usuario.service.ts`), classe PascalCase (ex: `UsuarioService`)
- **VariÃ¡veis**: camelCase (ex: `isLoading`, `userName`)
- **Signals**: Acessar com `()` no template (ex: `isLoading()`, `users()`)
- **MÃ©todos**: camelCase (ex: `loadData()`, `onSubmit()`)
- **Constantes**: UPPER_SNAKE_CASE (ex: `MAX_FILE_SIZE`)

#### 7. PadrÃµes de BotÃµes e UI

**âš ï¸ IMPORTANTE**: Nunca use `alert()`, `confirm()` ou `prompt()` do navegador. Use sempre o `ConfirmationService`.

##### BotÃµes de AÃ§Ã£o em Listagens

**Ordem PadrÃ£o** (sempre nesta ordem):
1. **Visualizar** - `mat-icon-button` (sem cor), Ã­cone `visibility`
2. **Editar** - `mat-icon-button` (sem cor), Ã­cone `edit`
3. **AÃ§Ãµes EspecÃ­ficas** - `mat-icon-button` com cor quando aplicÃ¡vel
   - Ativar: `color="primary"`, Ã­cone `check_circle`
   - Inativar: `color="warn"`, Ã­cone `block`
4. **Excluir** - `mat-icon-button` com `color="warn"`, Ã­cone `delete` (sempre Ãºltimo)

**Exemplo**:
```html
<ng-container matColumnDef="acoes">
  <th mat-header-cell *matHeaderCellDef>AÃ§Ãµes</th>
  <td mat-cell *matCellDef="let e">
    <button mat-icon-button (click)="view(e.id)" matTooltip="Visualizar">
      <mat-icon>visibility</mat-icon>
    </button>
    <button mat-icon-button (click)="edit(e.id)" matTooltip="Editar">
      <mat-icon>edit</mat-icon>
    </button>
    <button mat-icon-button (click)="delete(e.id)" color="warn" matTooltip="Excluir">
      <mat-icon>delete</mat-icon>
    </button>
  </td>
</ng-container>
```

##### BotÃ£o "Adicionar" em Listagens

**PadrÃ£o**: Sempre usar o formato "Novo [Entidade]"
- Texto: "Novo Tenant", "Novo UsuÃ¡rio", "Novo Perfil", etc.
- Estilo: `mat-raised-button color="primary"` com Ã­cone `add`

**Exemplo**:
```html
<a mat-raised-button color="primary" routerLink="/backoffice/entidades/cadastro" class="add-button">
  <mat-icon>add</mat-icon>
  <span class="button-text">Novo [Entidade]</span>
</a>
```

##### BotÃµes em FormulÃ¡rios

**PadrÃ£o**:
- **Voltar**: `mat-stroked-button` (sem cor)
- **Salvar**: `mat-raised-button color="primary"`
- **AÃ§Ãµes Destrutivas SecundÃ¡rias**: `mat-button color="warn"`
- **AÃ§Ãµes Neutras SecundÃ¡rias**: `mat-stroked-button`

**Exemplo**:
```html
<div class="form-actions">
  <button mat-stroked-button type="button" routerLink="/backoffice/entidades">Voltar</button>
  <button mat-raised-button color="primary" type="submit">Salvar</button>
</div>
```

##### Uso do ConfirmationService

**Exemplo de ExclusÃ£o**:
```typescript
import { ConfirmationService } from '../../../../core/services/confirmation.service';

export class MinhaListaComponent {
  private confirmationService = inject(ConfirmationService);

  delete(id: number) {
    this.confirmationService.confirmDelete('este item')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (!confirmed) return;
        
        // Executar exclusÃ£o
        this.service.delete(id).subscribe(/* ... */);
      });
  }
}
```

**Exemplo de Ativar/Inativar**:
```typescript
toggleActive(id: number, currentStatus: boolean) {
  const action = currentStatus ? 'inativar' : 'ativar';
  const warningMessage = currentStatus 
    ? 'Este item nÃ£o poderÃ¡ mais ser usado.'
    : undefined;

  this.confirmationService.confirmToggleActive(action, 'este item', warningMessage)
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe(confirmed => {
      if (!confirmed) return;
      
      // Executar aÃ§Ã£o
      this.service.toggleActive(id).subscribe(/* ... */);
    });
}
```

**Exemplo de ConfirmaÃ§Ã£o Customizada**:
```typescript
this.confirmationService.confirm({
  title: 'Confirmar aÃ§Ã£o',
  message: 'Tem certeza que deseja executar esta aÃ§Ã£o?',
  confirmText: 'Executar',
  cancelText: 'Cancelar',
  confirmColor: 'warn'
}).subscribe(confirmed => {
  if (confirmed) {
    // Executar aÃ§Ã£o
  }
});
```

---

## âœ¨ Funcionalidades Implementadas

### Backend

#### 1. CRUD GenÃ©rico
- `BaseController` e `BaseService` para padronizar operaÃ§Ãµes CRUD
- Suporte a paginaÃ§Ã£o, ordenaÃ§Ã£o e busca
- ValidaÃ§Ã£o automÃ¡tica via Data Annotations
- Tratamento de exceÃ§Ãµes padronizado

#### 2. AutenticaÃ§Ã£o
- JWT para autenticaÃ§Ã£o
- Endpoints separados:
  - `/api/backoffice/auth/login` - Login do backoffice
  - `/api/tenant/auth/login` - Login do tenant
- Hash de senhas com BCrypt.Net

#### 3. Entidades e CRUDs

**Backoffice:**
- âœ… **UsuÃ¡rios** (`/api/backoffice/usuarios`)
  - CRUD completo
  - Upload de avatar
  - Troca de senha opcional na atualizaÃ§Ã£o
  - ValidaÃ§Ã£o de email Ãºnico
  - Usa entidade `BackofficeUsuario` e `BackofficePerfil`

- âœ… **Perfis** (`/api/backoffice/perfis`)
  - CRUD completo
  - Bloqueio de exclusÃ£o se houver usuÃ¡rios vinculados
  - Usa entidade `BackofficePerfil`

- âœ… **Tenants** (`/api/backoffice/tenants`)
  - CRUD completo
  - CriaÃ§Ã£o automÃ¡tica de schema e dados iniciais
  - ValidaÃ§Ã£o de CNPJ/CPF via BrasilApi
  - Busca automÃ¡tica de dados do CNPJ
  - CriaÃ§Ã£o automÃ¡tica de perfil "Administrador" e usuÃ¡rio admin no tenant

- âœ… **Categorias de Tenant** (`/api/backoffice/categoriatenants`)
  - CRUD completo
  - Endpoint pÃºblico para listar categorias ativas

**Tenant:**
- âœ… **UsuÃ¡rios** (`/api/tenant/usuarios`)
  - CRUD completo
  - Upload de avatar
  - Troca de senha opcional na atualizaÃ§Ã£o
  - ValidaÃ§Ã£o de email Ãºnico (dentro do schema do tenant)
  - Usa entidade `TenantUsuario` e `TenantPerfil`

- âœ… **Perfis** (`/api/tenant/perfis`)
  - CRUD completo
  - Bloqueio de exclusÃ£o se houver usuÃ¡rios vinculados
  - Perfis especÃ­ficos de cada tenant
  - Usa entidade `TenantPerfil`

- âœ… **Categorias de Insumo** (`/api/tenant/categorias-insumo`)
  - CRUD completo
  - ValidaÃ§Ã£o de nome Ãºnico (dentro do schema do tenant)
  - Usa entidade `CategoriaInsumo`

- âœ… **Unidades de Medida** (`/api/tenant/unidades-medida`)
  - CRUD completo
  - ValidaÃ§Ã£o de nome e sigla Ãºnicos (dentro do schema do tenant)
  - Unidades padrÃ£o criadas automaticamente ao criar tenant (kg, g, L, mL, un, dz, pct, cx)
  - Usa entidade `UnidadeMedida`

- âœ… **Insumos** (`/api/tenant/insumos`)
  - CRUD completo
  - Relacionamento com CategoriaInsumo e UnidadeMedida
  - Campos: Nome, Categoria, Unidade de Compra, Unidade de Uso, Quantidade por Embalagem, Custo UnitÃ¡rio, Fator de CorreÃ§Ã£o, Estoque MÃ­nimo, DescriÃ§Ã£o, Imagem
  - Usa entidade `Insumo`

- âœ… **Categorias de Receita** (`/api/tenant/categorias-receita`)
  - CRUD completo
  - ValidaÃ§Ã£o de nome Ãºnico (dentro do schema do tenant)
  - Categorias padrÃ£o criadas automaticamente ao criar tenant (Entrada, Prato Principal, Sobremesa, Bebida, Acompanhamento, Salada, Sopa, Outros)
  - Usa entidade `CategoriaReceita`

- âœ… **Receitas** (`/api/tenant/receitas`)
  - CRUD completo
  - Relacionamento com CategoriaReceita e Insumo (via ReceitaItem)
  - CÃ¡lculo automÃ¡tico de custos (CustoTotal e CustoPorPorcao)
  - Campos: Nome, Categoria, DescriÃ§Ã£o, InstruÃ§Ãµes de Empratamento, Rendimento, Peso por PorÃ§Ã£o, TolerÃ¢ncia de Peso, Fator de Rendimento, Tempo de Preparo, VersÃ£o, Imagem
  - Itens da receita: Insumo, Quantidade, Ordem, ObservaÃ§Ãµes
  - Funcionalidade de duplicar receita
  - CÃ¡lculo automÃ¡tico considera:
    - FatorCorrecao dos insumos (perdas no preparo/limpeza de cada insumo)
    - QuantidadePorEmbalagem dos insumos
    - FatorRendimento da receita (perdas no preparo da receita completa, ex: evaporaÃ§Ã£o, queima)
  - Controle de versÃ£o para rastreabilidade
  - Controle de peso por porÃ§Ã£o e tolerÃ¢ncia para padronizaÃ§Ã£o
  - Usa entidades `Receita` e `ReceitaItem`

- âœ… AutenticaÃ§Ã£o de usuÃ¡rios do tenant
- âœ… IdentificaÃ§Ã£o automÃ¡tica via middleware

#### 4. ValidaÃ§Ãµes
- ValidaÃ§Ã£o de CPF/CNPJ via BrasilApi
- ValidaÃ§Ã£o de formato e dÃ­gitos verificadores
- ValidaÃ§Ã£o de subdomÃ­nio (Ãºnico, apenas letras e nÃºmeros)

#### 5. Upload de Arquivos
- **Upload**: `POST /api/uploads/avatar` para upload de avatares
  - Limite de tamanho: 10 MB
  - Formatos suportados: PNG e JPG
  - Arquivos salvos em `wwwroot/avatars/` com nome Ãºnico (GUID)
  - Retorna URL completa do arquivo: `{baseUrl}/avatars/{nome}`
- **Delete**: `DELETE /api/uploads/avatar?url=...` ou `?name=...` para remover avatares
- Arquivos servidos estaticamente via `UseStaticFiles()` em `/avatars/{nome}`

#### 6. Health Checks
- Endpoint `/health` para verificaÃ§Ã£o de saÃºde do banco de dados
- Verifica especificamente a conexÃ£o com o `ApplicationDbContext`

#### 7. OpenAPI/Swagger
- Em desenvolvimento, OpenAPI disponÃ­vel em `/openapi/v1.json`
- Configurado via `app.MapOpenApi()` no `Program.cs`

#### 8. SerializaÃ§Ã£o JSON
- API configurada para usar `camelCase` na serializaÃ§Ã£o JSON (padrÃ£o do Angular)
- Configurado via `AddJsonOptions` no `Program.cs`

#### 9. InicializaÃ§Ã£o AutomÃ¡tica
- **Migrations**: Aplicadas automaticamente na inicializaÃ§Ã£o da aplicaÃ§Ã£o
- **DbInitializer**: Inicializa dados padrÃ£o automaticamente:
  - Categorias de tenant: "Alimentos", "Bebidas", "Outros"
  - Perfil "Administrador" no backoffice
  - UsuÃ¡rio padrÃ£o do backoffice (verificar `DbInitializer.cs` para credenciais)

#### 10. Logging
- Logging estruturado em todos os services e controllers
- Uso de `ILogger<T>` para logs contextuais

### Frontend

#### 1. Interface Responsiva
- Layout responsivo com Angular Material 3
- Tema escuro/claro configurÃ¡vel
- Sidebar colapsÃ¡vel com estado persistente
- PrevenÃ§Ã£o de scroll horizontal
- **NavegaÃ§Ã£o hierÃ¡rquica com submenus** - OrganizaÃ§Ã£o lÃ³gica de funcionalidades

#### 2. Componentes

**Listagens:**
- âœ… Listagem de UsuÃ¡rios (backoffice)
- âœ… Listagem de Perfis (backoffice)
- âœ… Listagem de Tenants (backoffice)
  - Coluna Categoria (substituiu SubdomÃ­nio)
  - OrdenaÃ§Ã£o por categoria
  - PadrÃ£o de botÃµes padronizado
- âœ… Listagem de UsuÃ¡rios (tenant)
- âœ… Listagem de Perfis (tenant)
- âœ… Listagem de Categorias de Insumo (tenant)
- âœ… Listagem de Unidades de Medida (tenant)
- âœ… Listagem de Insumos (tenant)
- âœ… Listagem de Categorias de Receita (tenant)
- âœ… Listagem de Receitas (tenant)
  - ExibiÃ§Ã£o de custo por porÃ§Ã£o
  - FormataÃ§Ã£o de tempo de preparo
  - Funcionalidade de duplicar receita
- PaginaÃ§Ã£o server-side
- OrdenaÃ§Ã£o por colunas
- Busca com debounce
- VisualizaÃ§Ã£o mobile-friendly (cards)

**FormulÃ¡rios:**
- âœ… FormulÃ¡rio de UsuÃ¡rio (backoffice)
  - Upload de avatar com preview
  - ValidaÃ§Ã£o de email
  - Troca de senha opcional

- âœ… FormulÃ¡rio de Perfil (backoffice)
  - ValidaÃ§Ã£o de nome Ãºnico

- âœ… FormulÃ¡rio de Tenant (backoffice)
  - MÃ¡scara dinÃ¢mica de CPF/CNPJ
  - Busca automÃ¡tica de dados do CNPJ
  - GeraÃ§Ã£o automÃ¡tica de subdomÃ­nio
  - SeleÃ§Ã£o de categoria
  - CriaÃ§Ã£o de administrador
- âœ… FormulÃ¡rio de UsuÃ¡rio (tenant)
- âœ… FormulÃ¡rio de Perfil (tenant)
- âœ… FormulÃ¡rio de Categoria de Insumo (tenant)
- âœ… FormulÃ¡rio de Unidade de Medida (tenant)
- âœ… FormulÃ¡rio de Insumo (tenant)
- âœ… FormulÃ¡rio de Categoria de Receita (tenant)
- âœ… FormulÃ¡rio de Receita (tenant)
  - Tabela editÃ¡vel de itens
  - Adicionar/remover/reordenar itens
  - ValidaÃ§Ã£o de itens obrigatÃ³rios

#### 3. AutenticaÃ§Ã£o
- Login separado para backoffice e tenant
- Guard de autenticaÃ§Ã£o
- Interceptor para adicionar token JWT
- Interceptor para identificar tenant (header `X-Tenant-Subdomain`)

#### 4. UtilitÃ¡rios
- MÃ¡scaras de CPF/CNPJ dinÃ¢micas
- GeraÃ§Ã£o de subdomÃ­nio a partir de nome fantasia
- Toast notifications padronizadas
- Loading global
- **ConfirmationService** - DiÃ¡logos de confirmaÃ§Ã£o padronizados

#### 5. DiÃ¡logos de ConfirmaÃ§Ã£o
- âœ… **ConfirmationService** - Service centralizado para confirmaÃ§Ãµes
  - Substitui `confirm()` e `alert()` do navegador
  - DiÃ¡logos do Angular Material para melhor UX
  - MÃ©todos helpers para casos comuns:
    - `confirmDelete(itemName?)` - ConfirmaÃ§Ã£o de exclusÃ£o
    - `confirmToggleActive(action, itemName?, warningMessage?)` - ConfirmaÃ§Ã£o de ativar/inativar
    - `confirmSimple(message, title?)` - ConfirmaÃ§Ã£o simples
    - `confirm(data)` - ConfirmaÃ§Ã£o customizada completa
  - Retorna `Observable<boolean>` para integraÃ§Ã£o reativa

#### 6. PadrÃµes Modernos
- Standalone Components
- OnPush Change Detection
- Signals para estado reativo
- Template-Driven Forms
- Nova sintaxe Angular 20 (`@if`, `@for`, `@switch`)
- `takeUntilDestroyed` para gerenciamento de subscriptions

---

## ðŸ“– Guias de Uso

### Criar um Novo CRUD

#### Backend

1. **Criar Entidade**:
   - **Backoffice**: em `Models/Backoffice/Entities/BackofficeMinhaEntidade.cs`
   - **Tenant**: em `Models/Tenant/Entities/TenantMinhaEntidade.cs`
```csharp
// Backoffice
public class BackofficeMinhaEntidade
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    // ... outros campos
}

// Tenant
public class TenantMinhaEntidade
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    // ... outros campos
}
```

2. **Adicionar ao DbContext**:
   - **Backoffice**: `Data/ApplicationDbContext.cs`
   ```csharp
   public DbSet<BackofficeMinhaEntidade> BackofficeMinhasEntidades { get; set; }
   ```
   - **Tenant**: `Data/TenantDbContext.cs`
   ```csharp
   public DbSet<TenantMinhaEntidade> TenantMinhasEntidades { get; set; }
   ```

3. **Criar DTOs**:
   - **Backoffice**: em `Models/Backoffice/DTOs/`
     - `BackofficeMinhaEntidadeDto.cs`
     - `CreateBackofficeMinhaEntidadeRequest.cs`
     - `UpdateBackofficeMinhaEntidadeRequest.cs`
   - **Tenant**: em `Models/Tenant/DTOs/`
     - `TenantMinhaEntidadeDto.cs`
     - `CreateTenantMinhaEntidadeRequest.cs`
     - `UpdateTenantMinhaEntidadeRequest.cs`

4. **Criar Interface e Service**:
   - **Backoffice**:
     - Interface em `Services/Backoffice/Interfaces/IBackofficeMinhaEntidadeService.cs`
     - Service em `Services/Backoffice/Implementations/BackofficeMinhaEntidadeService.cs` (herdar de `BaseService<ApplicationDbContext, ...>`)
   - **Tenant**:
     - Interface em `Services/Tenant/Interfaces/ITenantMinhaEntidadeService.cs`
     - Service em `Services/Tenant/Implementations/TenantMinhaEntidadeService.cs` (herdar de `BaseService<TenantDbContext, ...>`)

5. **Criar Controller**:
   - **Backoffice**: em `Controllers/Backoffice/MinhaEntidadeController.cs` (herdar de `BaseController`)
   - **Tenant**: em `Controllers/Tenant/MinhaEntidadeController.cs` (herdar de `BaseController`)

6. **Registrar Service** em `Extensions/ServiceCollectionExtensions.cs`:
```csharp
// Backoffice
services.AddScoped<IBackofficeMinhaEntidadeService, BackofficeMinhaEntidadeService>();

// Tenant
services.AddScoped<ITenantMinhaEntidadeService, TenantMinhaEntidadeService>();
```

7. **Criar Migration**:
```bash
dotnet ef migrations add AddMinhaEntidade --context ApplicationDbContext
dotnet ef database update --context ApplicationDbContext
```

#### Frontend

1. **Criar Service** em `features/minhaentidade/services/minha-entidade.service.ts`

2. **Criar Componente de Listagem** em `backoffice/components/listagens/minhaentidade/`

3. **Criar Componente de FormulÃ¡rio** em `backoffice/components/cadastros/minhaentidade/`

4. **Adicionar Rotas** em `app.routes.ts`:
```typescript
{
  path: 'backoffice/minhaentidade',
  loadComponent: () => import('./backoffice/components/listagens/minhaentidade/minhaentidade-list.component')
    .then(m => m.MinhaEntidadeListComponent)
}
```

5. **Adicionar Item no Menu** em `shell/shell.component.ts`

---

## ðŸ“¦ Build e Deploy

### Backend

```bash
cd src/SGR.Api
dotnet build
dotnet publish -c Release
```

### Frontend

```bash
cd web
ng build --configuration production
```

---

## ðŸŽ¯ Resumo

O **SGR** Ã© um sistema completo de gerenciamento multi-tenant que permite:

1. **Gerenciar mÃºltiplos tenants** (restaurantes/empresas) de forma isolada
2. **Criar e gerenciar usuÃ¡rios e perfis** separadamente no backoffice e em cada tenant
   - Backoffice: perfis administrativos (Administrador, Digitador, etc.)
   - Tenants: perfis personalizados por tenant (GarÃ§om, Cozinheiro, Gerente, etc.)
3. **Validar e buscar dados** de CNPJ automaticamente via BrasilApi
4. **Autenticar usuÃ¡rios** separadamente no backoffice e nos tenants
5. **Fazer upload de arquivos** (avatares, imagens)
6. **Operar com CRUD genÃ©rico** que facilita a criaÃ§Ã£o de novos mÃ³dulos
7. **Interface responsiva e moderna** com Angular Material 3
8. **Arquitetura escalÃ¡vel** com separaÃ§Ã£o clara de contextos e isolamento de dados

O sistema estÃ¡ preparado para escalar horizontalmente, com isolamento completo de dados por tenant e arquitetura modular que facilita a manutenÃ§Ã£o e evoluÃ§Ã£o.

---

## ðŸ§­ Estrutura de NavegaÃ§Ã£o

O sistema utiliza uma estrutura de navegaÃ§Ã£o hierÃ¡rquica com submenus, organizada de forma lÃ³gica baseada em sistemas de referÃªncia como TOTVS, iFood GestÃ£o e Kitchen Display.

### PrincÃ­pios de OrganizaÃ§Ã£o

1. **Agrupamento LÃ³gico**: Funcionalidades relacionadas sÃ£o agrupadas em submenus
2. **Hierarquia Clara**: Submenus dividem categorias amplas em seÃ§Ãµes especÃ­ficas
3. **Ordem de Uso**: Funcionalidades mais utilizadas aparecem primeiro
4. **Nomenclatura Clara**: Evita termos genÃ©ricos e confusos

### Estrutura do Menu - Tenant

```
ðŸ“Š Dashboard
ðŸ“¦ Cadastros
   â”œâ”€â”€ Insumos
   â”œâ”€â”€ Receitas
   â”œâ”€â”€ Categorias de Insumo
   â”œâ”€â”€ Categorias de Receita
   â””â”€â”€ Unidades de Medida
âš™ï¸ ConfiguraÃ§Ãµes
   â”œâ”€â”€ UsuÃ¡rios
   â””â”€â”€ Perfis
```

### Estrutura do Menu - Backoffice

```
ðŸ“Š Dashboard
ðŸ¢ GestÃ£o
   â”œâ”€â”€ Tenants
   â”œâ”€â”€ UsuÃ¡rios
   â””â”€â”€ Perfis
```

### Funcionalidades da NavegaÃ§Ã£o

- **Submenus ExpansÃ­veis**: Usa `mat-expansion-panel` do Angular Material
- **ExpansÃ£o AutomÃ¡tica**: Submenus com rotas ativas sÃ£o expandidos automaticamente
- **Destaque Visual**: Itens ativos e submenus com rotas ativas sÃ£o destacados
- **Responsivo**: Em modo colapsado, mostra apenas Ã­cones com tooltips
- **Estado Persistente**: Estado de expansÃ£o dos submenus Ã© mantido durante a navegaÃ§Ã£o

---

**Ãšltima atualizaÃ§Ã£o**: 2025-01-27

**Changelog**:
- âœ… **NavegaÃ§Ã£o HierÃ¡rquica**: ReorganizaÃ§Ã£o da sidebar com submenus expansÃ­veis
  - Agrupamento lÃ³gico: Cadastros e ConfiguraÃ§Ãµes (Tenant), GestÃ£o (Backoffice)
  - Nomenclatura melhorada: "Categorias de Insumo" e "Categorias de Receita" (antes apenas "Categorias")
  - ExpansÃ£o automÃ¡tica de submenus com rotas ativas
  - Destaque visual para itens e submenus ativos
- âœ… Implementado ConfirmationService para substituir alertas do navegador
- âœ… PadronizaÃ§Ã£o de botÃµes em todas as listagens
- âœ… Adicionada coluna Categoria na listagem de Tenants
- âœ… Removida coluna SubdomÃ­nio da listagem de Tenants
- âœ… **RefatoraÃ§Ã£o**: SeparaÃ§Ã£o completa entre usuÃ¡rios/perfis do backoffice e dos tenants
  - Criadas entidades separadas: `BackofficeUsuario`, `BackofficePerfil`, `TenantUsuario`, `TenantPerfil`
  - Services e controllers reorganizados por contexto (Backoffice/Tenant)
  - DTOs separados para cada contexto
  - Melhor isolamento e escalabilidade do sistema
- âœ… **Receitas**: Implementado FatorRendimento para cÃ¡lculo de perdas no preparo da receita
  - Campo FatorRendimento adicionado Ã  entidade Receita
  - CÃ¡lculo automÃ¡tico de custos considera perdas no preparo (evaporaÃ§Ã£o, queima, etc.)
  - Complementa o FatorCorrecao dos insumos (perdas no preparo individual)
- âœ… **Receitas**: Melhorias na padronizaÃ§Ã£o e controle de qualidade
  - Campo Versao para rastreabilidade de mudanÃ§as na receita
  - Campo PesoPorPorcao para controle de gramagem por porÃ§Ã£o
  - Campo InstrucoesEmpratamento separado das instruÃ§Ãµes de preparo

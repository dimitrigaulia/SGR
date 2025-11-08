# SGR - Sistema de Gerenciamento de Restaurantes

Sistema completo de gerenciamento multi-tenant desenvolvido com **Angular 20** (frontend) e **ASP.NET Core 9** (backend), implementando arquitetura **Schema per Tenant** no PostgreSQL.

---

## 📋 Índice

1. [Visão Geral](#-visão-geral)
2. [Tecnologias](#-tecnologias)
3. [Pré-requisitos e Instalação](#-pré-requisitos-e-instalação)
4. [Configuração](#-configuração)
5. [Arquitetura Multi-Tenant](#-arquitetura-multi-tenant)
6. [Estrutura de Pastas](#-estrutura-de-pastas)
7. [Padrões e Convenções](#-padrões-e-convenções)
8. [Funcionalidades Implementadas](#-funcionalidades-implementadas)
9. [Guias de Uso](#-guias-de-uso)
10. [Build e Deploy](#-build-e-deploy)

---

## 🎯 Visão Geral

O **SGR** é uma plataforma completa de gerenciamento que permite:

- **Backoffice**: Sistema administrativo para gerenciar tenants, usuários administrativos e perfis do backoffice
- **Multi-Tenancy**: Cada tenant (restaurante/empresa) possui seu próprio schema no banco de dados, garantindo isolamento completo de dados
- **Autenticação**: Sistema de autenticação JWT separado para backoffice e tenants
- **CRUD Genérico**: Sistema padronizado de operações CRUD que facilita a criação de novos módulos
- **Validações**: Validação de CPF/CNPJ via BrasilApi, validação de dados em tempo real
- **Upload de Arquivos**: Sistema de upload de avatares e imagens
- **Interface Moderna**: Interface responsiva com Angular Material 3 e tema escuro/claro

---

## 🛠️ Tecnologias

### Backend
- **.NET 9** - Framework principal
- **ASP.NET Core Web API** - API REST
- **Entity Framework Core** - ORM
- **PostgreSQL** - Banco de dados
- **JWT** - Autenticação
- **BCrypt.Net** - Hash de senhas
- **BrasilApi** - Validação de CPF/CNPJ e busca de dados de CNPJ

### Frontend
- **Angular 20** - Framework principal
- **Angular Material 3** - Componentes UI
- **RxJS** - Programação reativa
- **TypeScript** - Linguagem
- **SCSS** - Estilização

---

## 📋 Pré-requisitos e Instalação

### Pré-requisitos
- **.NET 9 SDK**
- **Node.js 18+** e **npm**
- **PostgreSQL 14+**
- **Angular CLI 20+**

### Instalação

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

## ⚙️ Configuração

### Configurações Automáticas

O sistema possui algumas configurações que são aplicadas automaticamente:

- **Migrations**: As migrations do Entity Framework são aplicadas automaticamente na inicialização da aplicação
- **Inicialização de Dados**: O `DbInitializer` cria automaticamente:
  - Categorias padrão de tenants (Alimentos, Bebidas, Outros)
  - Perfil "Administrador" no backoffice
  - Usuário padrão do backoffice (verificar `DbInitializer.cs` para credenciais)

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

A API estará disponível em `http://localhost:5281`.

**Nota**: As migrations são aplicadas automaticamente na inicialização da aplicação. O sistema também inicializa automaticamente os dados padrão (categorias, perfil administrador e usuário padrão) através do `DbInitializer`.

**Importante**: 
- A porta padrão da API é `5281` (configurada em `launchSettings.json`)
- Em desenvolvimento, o HTTPS redirection é desabilitado para evitar problemas com CORS
- O OpenAPI está disponível apenas em ambiente de desenvolvimento

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

A aplicação estará disponível em `http://localhost:4200`.

---

## 🏢 Arquitetura Multi-Tenant

### Estratégia: Schema per Tenant

O sistema utiliza uma arquitetura **Schema per Tenant** no PostgreSQL, onde cada tenant possui seu próprio schema, garantindo isolamento completo de dados.

### Bancos de Dados

#### 1. Banco `sgr_config` (Backoffice)
**Schema: `public`**
- `CategoriaTenant` - Categorias de tenants (Alimentos, Bebidas, Outros)
- `Tenant` - Lista de todos os tenants cadastrados
- `Usuario` - Usuários administrativos do backoffice
- `Perfil` - Perfis de acesso do backoffice

#### 2. Banco `sgr_tenants` (Dados dos Tenants)
**Schemas dinâmicos: `{subdominio}_{id}`** (ex: `vangoghbar_1`, `restaurante_2`)

Cada schema contém:
- `Perfil` - Perfis de acesso do tenant
- `Usuario` - Usuários do tenant

### Fluxo de Criação do Tenant

1. **Validações**:
   - Validar CNPJ/CPF (formato + dígitos verificadores) via BrasilApi
   - Validar subdomínio (apenas letras e números, único)
   - Validar dados do admin (nome, email, senha)

2. **Criar Tenant**:
   - Criar registro em `Tenant` (banco `sgr_config`)
   - Gerar `NomeSchema = "{subdominio}_{id}"`

3. **Criar Banco `sgr_tenants`** (se não existir):
   - `CREATE DATABASE sgr_tenants;`

4. **Criar Schema do Tenant**:
   - `CREATE SCHEMA {NomeSchema};`

5. **Executar Migrations no Schema**:
   - Criar tabelas: `Perfil`, `Usuario`

6. **Inicializar Dados do Tenant**:
   - Criar Perfil "Administrador" (IsAtivo: true)
   - Criar Usuario admin (com perfil Administrador)

### Identificação do Tenant

#### Em Produção
- Middleware lê o header `Host` (ex: `vangoghbar.sgr.com.br`)
- Extrai subdomínio: `vangoghbar`
- Busca tenant no banco `sgr_config`
- Configura `TenantDbContext` para usar schema do tenant

#### Em Desenvolvimento
- Frontend envia header `X-Tenant-Subdomain` (via combobox no login)
- Middleware lê header e identifica tenant
- Configura `TenantDbContext` para usar schema do tenant

---

## 📁 Estrutura de Pastas

### Backend (`src/SGR.Api/`)

```
SGR.Api/
├── Controllers/
│   ├── Backoffice/
│   │   ├── BaseController.cs          # Controller base genérico
│   │   ├── AuthController.cs          # Autenticação backoffice
│   │   ├── UsuariosController.cs      # CRUD usuários
│   │   ├── PerfisController.cs        # CRUD perfis
│   │   ├── TenantsController.cs       # CRUD tenants
│   │   ├── CategoriaTenantsController.cs # CRUD categorias
│   │   └── UploadsController.cs       # Upload de arquivos
│   └── Tenant/
│       └── AuthController.cs          # Autenticação tenant
├── Services/
│   ├── Interfaces/
│   │   ├── IBaseService.cs            # Interface base genérica
│   │   ├── IUsuarioService.cs
│   │   ├── IPerfilService.cs
│   │   ├── ITenantService.cs
│   │   ├── IAuthService.cs
│   │   ├── ITenantAuthService.cs
│   │   ├── ICpfCnpjValidationService.cs
│   │   └── ICnpjDataService.cs
│   └── Implementations/
│       ├── BaseService.cs             # Service base genérico
│       ├── UsuarioService.cs
│       ├── PerfilService.cs
│       ├── TenantService.cs
│       ├── AuthService.cs
│       ├── TenantAuthService.cs
│       ├── CpfCnpjValidationService.cs
│       └── CnpjDataService.cs
├── Models/
│   ├── Entities/
│   │   ├── Usuario.cs
│   │   ├── Perfil.cs
│   │   ├── Tenant.cs
│   │   └── CategoriaTenant.cs
│   └── DTOs/
│       ├── UsuarioDto.cs
│       ├── CreateUsuarioRequest.cs
│       ├── UpdateUsuarioRequest.cs
│       ├── PerfilDto.cs
│       ├── CreatePerfilRequest.cs
│       ├── UpdatePerfilRequest.cs
│       ├── TenantDto.cs
│       ├── CreateTenantRequest.cs
│       ├── UpdateTenantRequest.cs
│       ├── CategoriaTenantDto.cs
│       ├── CnpjDataResponse.cs
│       ├── LoginRequest.cs
│       └── LoginResponse.cs
├── Data/
│   ├── ApplicationDbContext.cs        # Contexto sgr_config
│   ├── TenantDbContext.cs             # Contexto sgr_tenants (schema dinâmico)
│   └── DbInitializer.cs               # Inicializador do banco
├── Extensions/
│   └── ServiceCollectionExtensions.cs # Extension methods para DI
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs # Tratamento global de exceções
│   └── TenantIdentificationMiddleware.cs # Identificação do tenant
├── Exceptions/
│   ├── BusinessException.cs           # Exceção de negócio
│   └── NotFoundException.cs           # Exceção de não encontrado
├── Migrations/                        # Migrations do EF Core
├── wwwroot/                          # Arquivos estáticos
│   └── avatars/                      # Avatares dos usuários
└── Program.cs                         # Configuração da aplicação
```

### Frontend (`web/src/app/`)

```
app/
├── backoffice/                        # Backoffice
│   ├── components/
│   │   ├── listagens/                # Componentes de listagem
│   │   │   ├── usuario/
│   │   │   ├── perfil/
│   │   │   └── tenants/
│   │   └── cadastros/                # Componentes de formulários
│   │       ├── usuario/
│   │       ├── perfil/
│   │       └── tenants/
│   └── login/                        # Login do backoffice
│       └── backoffice-login.component.*
├── tenant/                            # Tenant
│   └── login/                        # Login do tenant
│       └── tenant-login.component.*
├── core/                              # Funcionalidades core
│   ├── guards/
│   │   ├── auth.guard.ts            # Guard de autenticação
│   │   └── state.guard.ts           # Guard de estado
│   ├── interceptors/
│   │   ├── auth.interceptor.ts      # Interceptor de autenticação
│   │   ├── error.interceptor.ts     # Interceptor de erros
│   │   └── tenant.interceptor.ts    # Interceptor de tenant
│   ├── services/
│   │   ├── auth.service.ts          # Service de autenticação
│   │   ├── toast.service.ts         # Service de notificações
│   │   ├── api.service.ts           # Service base da API
│   │   └── layout.service.ts        # Service de layout
│   ├── utils/
│   │   ├── mask.utils.ts            # Utilitários de máscara
│   │   └── subdomain.utils.ts       # Utilitários de subdomínio
│   └── models/
│       ├── auth.model.ts
│       └── menu-item.model.ts
├── shared/                            # Componentes compartilhados
│   └── components/
│       └── loading/
│           └── loading.component.*
├── features/                          # Services por feature
│   ├── usuarios/
│   │   └── services/
│   │       ├── usuario.service.ts
│   │       └── upload.service.ts
│   ├── perfis/
│   │   └── services/
│   │       └── perfil.service.ts
│   └── tenants/
│       └── services/
│           ├── tenant.service.ts
│           └── categoria-tenant.service.ts
├── shell/                             # Layout principal
│   ├── shell.component.ts
│   ├── shell.component.html
│   └── shell.component.scss
├── app.routes.ts                      # Configuração de rotas
└── app.config.ts                      # Configuração da aplicação
```

---

## 📐 Padrões e Convenções

### Backend (.NET C#)

#### 1. Controllers

**Padrão**: Herdar de `BaseController` para operações CRUD padrão.

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

    // Métodos específicos podem ser adicionados aqui
    // Os métodos CRUD padrão (GetAll, GetById, Create, Update, Delete) 
    // já estão disponíveis via BaseController
}
```

#### 2. Services

**Padrão**: Herdar de `BaseService` e implementar métodos abstratos.

```csharp
public interface IMinhaEntidadeService 
    : IBaseService<MinhaEntidade, MinhaEntidadeDto, CreateMinhaEntidadeRequest, UpdateMinhaEntidadeRequest>
{
}

public class MinhaEntidadeService 
    : BaseService<MinhaEntidade, MinhaEntidadeDto, CreateMinhaEntidadeRequest, UpdateMinhaEntidadeRequest>,
      IMinhaEntidadeService
{
    public MinhaEntidadeService(
        ApplicationDbContext context, 
        ILogger<MinhaEntidadeService> logger) 
        : base(context, logger)
    {
    }

    protected override Expression<Func<MinhaEntidade, MinhaEntidadeDto>> MapToDto()
    {
        return e => new MinhaEntidadeDto
        {
            Id = e.Id,
            Nome = e.Nome,
            // ... outros campos
        };
    }

    protected override MinhaEntidade MapToEntity(CreateMinhaEntidadeRequest request)
    {
        return new MinhaEntidade
        {
            Nome = request.Nome,
            // ... outros campos
        };
    }

    protected override void UpdateEntity(MinhaEntidade entity, UpdateMinhaEntidadeRequest request)
    {
        entity.Nome = request.Nome;
        // ... outros campos
    }

    // Opcional: Customizar busca
    protected override IQueryable<MinhaEntidade> ApplySearch(IQueryable<MinhaEntidade> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;
        return query.Where(e => e.Nome.Contains(search));
    }
}
```

#### 3. DTOs

**Padrão**: Usar Data Annotations para validação.

```csharp
public class CreateMinhaEntidadeRequest
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [MaxLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres")]
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

**Padrão**: Registrar no `ServiceCollectionExtensions.cs`.

```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddScoped<IMinhaEntidadeService, MinhaEntidadeService>();
    return services;
}
```

### Frontend (Angular 20)

#### 1. Componentes

**Padrão**: Todos os componentes são standalone com `OnPush` change detection.

```typescript
import { Component, ChangeDetectionStrategy, signal, computed, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-example',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './example.component.html',
  styleUrls: ['./example.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush // ✅ Sempre usar OnPush
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

**⚠️ Diretivas Antigas (DEPRECIADAS)**: `*ngIf`, `*ngFor`, `*ngSwitch`

**✅ Nova Sintaxe**:

```html
<!-- @if (substitui *ngIf) -->
@if (isLoading()) {
  <app-loading></app-loading>
} @else {
  <div>Conteúdo</div>
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
    <div>Padrão</div>
  }
}
```

**Variáveis disponíveis no `@for`**:
- `$index` - índice do item
- `$first` - primeiro item
- `$last` - último item
- `$even` - índice par
- `$odd` - índice ímpar
- `$count` - total de itens

#### 3. Template-Driven Forms

**Padrão**: Usar Template-Driven Forms para formulários simples.

```html
<form #f="ngForm" (ngSubmit)="save()">
  <mat-form-field appearance="outline" class="form-field-spacing">
    <mat-label>Nome</mat-label>
    <input matInput name="nome" [(ngModel)]="model.nome" required />
    @if (f.controls['nome']?.errors?.['required'] && f.controls['nome']?.touched) {
      <mat-error>Nome é obrigatório</mat-error>
    }
  </mat-form-field>
</form>
```

#### 4. Classes Globais de Formulários

Todas as classes abaixo estão disponíveis globalmente em `styles.scss`:

- **`.form-container`** - Container principal do formulário
  - Padding: 16px (12px no mobile)
  - Max-width: 800px
  - Centralizado automaticamente

- **`.form-title`** - Título do formulário
  - Font-size: 1.75rem (1.5rem tablet, 1.25rem mobile)
  - Margin-bottom: 32px (24px tablet, 20px mobile)

- **`.form-field-spacing`** - Espaçamento entre campos
  - Width: 100%
  - Margin-bottom: 24px (20px tablet, 16px mobile)

- **`.form-section`** - Agrupar seções relacionadas
  - Margin-bottom: 32px

- **`.form-actions`** - Container para botões
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

**Padrão**: Services injetáveis com métodos tipados.

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
- **Variáveis**: camelCase (ex: `isLoading`, `userName`)
- **Signals**: Acessar com `()` no template (ex: `isLoading()`, `users()`)
- **Métodos**: camelCase (ex: `loadData()`, `onSubmit()`)
- **Constantes**: UPPER_SNAKE_CASE (ex: `MAX_FILE_SIZE`)

---

## ✨ Funcionalidades Implementadas

### Backend

#### 1. CRUD Genérico
- `BaseController` e `BaseService` para padronizar operações CRUD
- Suporte a paginação, ordenação e busca
- Validação automática via Data Annotations
- Tratamento de exceções padronizado

#### 2. Autenticação
- JWT para autenticação
- Endpoints separados:
  - `/api/backoffice/auth/login` - Login do backoffice
  - `/api/tenant/auth/login` - Login do tenant
- Hash de senhas com BCrypt.Net

#### 3. Entidades e CRUDs

**Backoffice:**
- ✅ **Usuários** (`/api/backoffice/usuarios`)
  - CRUD completo
  - Upload de avatar
  - Troca de senha opcional na atualização
  - Validação de email único

- ✅ **Perfis** (`/api/backoffice/perfis`)
  - CRUD completo
  - Bloqueio de exclusão se houver usuários vinculados

- ✅ **Tenants** (`/api/backoffice/tenants`)
  - CRUD completo
  - Criação automática de schema e dados iniciais
  - Validação de CNPJ/CPF via BrasilApi
  - Busca automática de dados do CNPJ
  - Criação automática de perfil "Administrador" e usuário admin

- ✅ **Categorias de Tenant** (`/api/backoffice/categoriatenants`)
  - CRUD completo
  - Endpoint público para listar categorias ativas

**Tenant:**
- ✅ Autenticação de usuários do tenant
- ✅ Identificação automática via middleware

#### 4. Validações
- Validação de CPF/CNPJ via BrasilApi
- Validação de formato e dígitos verificadores
- Validação de subdomínio (único, apenas letras e números)

#### 5. Upload de Arquivos
- **Upload**: `POST /api/uploads/avatar` para upload de avatares
  - Limite de tamanho: 10 MB
  - Formatos suportados: PNG e JPG
  - Arquivos salvos em `wwwroot/avatars/` com nome único (GUID)
  - Retorna URL completa do arquivo: `{baseUrl}/avatars/{nome}`
- **Delete**: `DELETE /api/uploads/avatar?url=...` ou `?name=...` para remover avatares
- Arquivos servidos estaticamente via `UseStaticFiles()` em `/avatars/{nome}`

#### 6. Health Checks
- Endpoint `/health` para verificação de saúde do banco de dados
- Verifica especificamente a conexão com o `ApplicationDbContext`

#### 7. OpenAPI/Swagger
- Em desenvolvimento, OpenAPI disponível em `/openapi/v1.json`
- Configurado via `app.MapOpenApi()` no `Program.cs`

#### 8. Serialização JSON
- API configurada para usar `camelCase` na serialização JSON (padrão do Angular)
- Configurado via `AddJsonOptions` no `Program.cs`

#### 9. Inicialização Automática
- **Migrations**: Aplicadas automaticamente na inicialização da aplicação
- **DbInitializer**: Inicializa dados padrão automaticamente:
  - Categorias de tenant: "Alimentos", "Bebidas", "Outros"
  - Perfil "Administrador" no backoffice
  - Usuário padrão do backoffice (verificar `DbInitializer.cs` para credenciais)

#### 10. Logging
- Logging estruturado em todos os services e controllers
- Uso de `ILogger<T>` para logs contextuais

### Frontend

#### 1. Interface Responsiva
- Layout responsivo com Angular Material 3
- Tema escuro/claro configurável
- Sidebar colapsável com estado persistente
- Prevenção de scroll horizontal

#### 2. Componentes

**Listagens:**
- ✅ Listagem de Usuários (backoffice)
- ✅ Listagem de Perfis (backoffice)
- ✅ Listagem de Tenants (backoffice)
- Paginação server-side
- Ordenação por colunas
- Busca com debounce
- Visualização mobile-friendly (cards)

**Formulários:**
- ✅ Formulário de Usuário (backoffice)
  - Upload de avatar com preview
  - Validação de email
  - Troca de senha opcional

- ✅ Formulário de Perfil (backoffice)
  - Validação de nome único

- ✅ Formulário de Tenant (backoffice)
  - Máscara dinâmica de CPF/CNPJ
  - Busca automática de dados do CNPJ
  - Geração automática de subdomínio
  - Seleção de categoria
  - Criação de administrador

#### 3. Autenticação
- Login separado para backoffice e tenant
- Guard de autenticação
- Interceptor para adicionar token JWT
- Interceptor para identificar tenant (header `X-Tenant-Subdomain`)

#### 4. Utilitários
- Máscaras de CPF/CNPJ dinâmicas
- Geração de subdomínio a partir de nome fantasia
- Toast notifications padronizadas
- Loading global

#### 5. Padrões Modernos
- Standalone Components
- OnPush Change Detection
- Signals para estado reativo
- Template-Driven Forms
- Nova sintaxe Angular 20 (`@if`, `@for`, `@switch`)
- `takeUntilDestroyed` para gerenciamento de subscriptions

---

## 📖 Guias de Uso

### Criar um Novo CRUD

#### Backend

1. **Criar Entidade** em `Models/Entities/`:
```csharp
public class MinhaEntidade
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    // ... outros campos
}
```

2. **Adicionar ao DbContext** (`Data/ApplicationDbContext.cs`):
```csharp
public DbSet<MinhaEntidade> MinhasEntidades { get; set; }
```

3. **Criar DTOs** em `Models/DTOs/`:
   - `MinhaEntidadeDto.cs`
   - `CreateMinhaEntidadeRequest.cs`
   - `UpdateMinhaEntidadeRequest.cs`

4. **Criar Interface e Service**:
   - Interface em `Services/Interfaces/IMinhaEntidadeService.cs`
   - Service em `Services/Implementations/MinhaEntidadeService.cs` (herdar de `BaseService`)

5. **Criar Controller** em `Controllers/Backoffice/MinhaEntidadeController.cs` (herdar de `BaseController`)

6. **Registrar Service** em `Extensions/ServiceCollectionExtensions.cs`:
```csharp
services.AddScoped<IMinhaEntidadeService, MinhaEntidadeService>();
```

7. **Criar Migration**:
```bash
dotnet ef migrations add AddMinhaEntidade --context ApplicationDbContext
dotnet ef database update --context ApplicationDbContext
```

#### Frontend

1. **Criar Service** em `features/minhaentidade/services/minha-entidade.service.ts`

2. **Criar Componente de Listagem** em `backoffice/components/listagens/minhaentidade/`

3. **Criar Componente de Formulário** em `backoffice/components/cadastros/minhaentidade/`

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

## 📦 Build e Deploy

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

## 🎯 Resumo

O **SGR** é um sistema completo de gerenciamento multi-tenant que permite:

1. **Gerenciar múltiplos tenants** (restaurantes/empresas) de forma isolada
2. **Criar e gerenciar usuários e perfis** tanto no backoffice quanto em cada tenant
3. **Validar e buscar dados** de CNPJ automaticamente via BrasilApi
4. **Autenticar usuários** separadamente no backoffice e nos tenants
5. **Fazer upload de arquivos** (avatares, imagens)
6. **Operar com CRUD genérico** que facilita a criação de novos módulos
7. **Interface responsiva e moderna** com Angular Material 3

O sistema está preparado para escalar horizontalmente, com isolamento completo de dados por tenant e arquitetura modular que facilita a manutenção e evolução.

---

**Última atualização**: 2025-01-27

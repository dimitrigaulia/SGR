# Estrutura e Padrões do Projeto SGR

Este documento descreve a estrutura do projeto, padrões de código e guias passo a passo para criar novos componentes, controllers e services.

## 📁 Estrutura do Projeto

### Backend (`src/SGR.Api/`)

```
SGR.Api/
├── Controllers/
│   ├── Backoffice/                    # Controllers do backoffice
│   │   ├── BaseController.cs          # Controller base genérico para CRUD
│   │   ├── UsuariosController.cs      # Controller de usuários
│   │   ├── PerfisController.cs        # Controller de perfis
│   │   ├── TenantsController.cs       # Controller de tenants
│   │   ├── AuthController.cs          # Controller de autenticação do backoffice
│   │   └── UploadsController.cs       # Controller de uploads
│   └── Tenant/                        # Controllers do tenant
│       └── AuthController.cs          # Controller de autenticação do tenant
├── Services/
│   ├── Interfaces/
│   │   ├── IBaseService.cs            # Interface base genérica
│   │   ├── IUsuarioService.cs         # Interface de usuários
│   │   ├── IPerfilService.cs          # Interface de perfis
│   │   ├── ITenantService.cs          # Interface de tenants
│   │   ├── IAuthService.cs            # Interface de autenticação (backoffice)
│   │   ├── ITenantAuthService.cs      # Interface de autenticação (tenant)
│   │   └── ICpfCnpjValidationService.cs # Interface de validação CPF/CNPJ
│   └── Implementations/
│       ├── BaseService.cs             # Service base genérico
│       ├── UsuarioService.cs          # Service de usuários
│       ├── PerfilService.cs           # Service de perfis
│       ├── TenantService.cs           # Service de tenants
│       ├── AuthService.cs             # Service de autenticação (backoffice)
│       ├── TenantAuthService.cs       # Service de autenticação (tenant)
│       └── CpfCnpjValidationService.cs # Service de validação CPF/CNPJ
├── Models/
│   ├── Entities/                      # Entidades do domínio
│   │   ├── Usuario.cs                 # Usuário (backoffice e tenant)
│   │   ├── Perfil.cs                  # Perfil (backoffice e tenant)
│   │   ├── Tenant.cs                  # Tenant (banco sgr_config)
│   │   └── TipoPessoa.cs              # Tipo de Pessoa (schema do tenant)
│   └── DTOs/                          # Data Transfer Objects
│       ├── UsuarioDto.cs
│       ├── CreateUsuarioRequest.cs
│       ├── UpdateUsuarioRequest.cs
│       ├── PerfilDto.cs
│       ├── CreatePerfilRequest.cs
│       ├── UpdatePerfilRequest.cs
│       ├── TenantDto.cs
│       ├── CreateTenantRequest.cs
│       ├── UpdateTenantRequest.cs
│       ├── LoginRequest.cs
│       └── LoginResponse.cs
├── Data/
│   ├── ApplicationDbContext.cs        # Contexto do EF Core (banco sgr_config)
│   ├── TenantDbContext.cs             # Contexto do EF Core (banco sgr_tenants)
│   └── DbInitializer.cs               # Inicializador do banco
├── Extensions/
│   └── ServiceCollectionExtensions.cs # Extension methods para DI
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs # Tratamento global de exceções
│   └── TenantIdentificationMiddleware.cs # Identificação do tenant
├── Exceptions/
│   ├── BusinessException.cs           # Exceção de negócio
│   └── NotFoundException.cs           # Exceção de não encontrado
└── Program.cs                         # Configuração da aplicação
```

### Frontend (`web/src/app/`)

```
app/
├── backoffice/                       # Backoffice
│   ├── components/
│   │   ├── listagens/               # Componentes de listagem
│   │   │   ├── usuario/
│   │   │   ├── perfil/
│   │   │   └── tenants/
│   │   └── cadastros/               # Componentes de formulários
│   │       ├── usuario/
│   │       ├── perfil/
│   │       └── tenants/
│   └── login/                       # Login do backoffice
│       ├── backoffice-login.component.ts
│       ├── backoffice-login.component.html
│       └── backoffice-login.component.scss
├── tenant/                           # Tenant
│   ├── components/                  # Para futuros componentes do tenant
│   └── login/                       # Login do tenant
│       ├── tenant-login.component.ts
│       ├── tenant-login.component.html
│       └── tenant-login.component.scss
├── core/                             # Funcionalidades core da aplicação
│   ├── guards/
│   │   ├── auth.guard.ts            # Guard de autenticação
│   │   └── state.guard.ts           # Guard de estado de navegação
│   ├── interceptors/
│   │   ├── auth.interceptor.ts      # Interceptor de autenticação
│   │   ├── error.interceptor.ts     # Interceptor de erros
│   │   └── tenant.interceptor.ts    # Interceptor de tenant (header X-Tenant-Subdomain)
│   ├── services/
│   │   ├── auth.service.ts          # Service de autenticação
│   │   ├── layout.service.ts        # Service de layout/tema
│   │   └── toast.service.ts         # Service de notificações
│   └── models/
│       ├── auth.model.ts            # Modelos de autenticação
│       └── menu-item.model.ts       # Modelos de menu
├── shared/                           # Componentes e recursos compartilhados
│   └── components/
│       └── loading/
│           └── loading.component.ts # Componente de loading global
├── features/                         # Funcionalidades específicas (services)
│   ├── usuarios/
│   │   └── services/
│   │       ├── usuario.service.ts   # Service de usuários
│   │       └── upload.service.ts    # Service de upload
│   ├── perfis/
│   │   └── services/
│   │       └── perfil.service.ts    # Service de perfis
│   └── tenants/
│       └── services/
│           └── tenant.service.ts    # Service de tenants
├── shell/                            # Layout principal
│   ├── shell.component.ts
│   ├── shell.component.html
│   └── shell.component.scss
├── app.routes.ts                     # Configuração de rotas
└── app.config.ts                     # Configuração da aplicação
```

## 🎯 Padrões de Código

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
    // Métodos específicos podem ser adicionados aqui
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

    // Implementar métodos abstratos obrigatórios
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
        
        return query.Where(e => 
            e.Nome.Contains(search) || 
            e.Descricao.Contains(search));
    }

    // Opcional: Validações antes de criar/atualizar/excluir
    protected override async Task BeforeCreateAsync(MinhaEntidade entity, CreateMinhaEntidadeRequest request, string? usuarioCriacao)
    {
        // Validações específicas
        if (await _dbSet.AnyAsync(e => e.Nome == request.Nome))
        {
            throw new BusinessException("Já existe uma entidade com este nome.");
        }
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

    [MaxLength(500, ErrorMessage = "A descrição deve ter no máximo 500 caracteres")]
    public string? Descricao { get; set; }
}

public class UpdateMinhaEntidadeRequest
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [MaxLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres")]
    public string Nome { get; set; } = string.Empty;
}

public class MinhaEntidadeDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public DateTime DataCriacao { get; set; }
    public string? UsuarioCriacao { get; set; }
}
```

#### 4. Registro de Services

**Padrão**: Registrar no `ServiceCollectionExtensions.cs`.

```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    // ... outros services
    
    services.AddScoped<IMinhaEntidadeService, MinhaEntidadeService>();
    
    return services;
}
```

### Frontend (Angular)

#### 1. Componentes Standalone

**Padrão**: Todos os componentes são standalone com `OnPush` change detection.

```typescript
import { Component, ChangeDetectionStrategy, signal, computed, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-minha-entidade-list',
  standalone: true,
  imports: [CommonModule, FormsModule, /* outros imports */],
  templateUrl: './minha-entidade-list.component.html',
  styleUrls: ['./minha-entidade-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MinhaEntidadeListComponent {
  private destroyRef = inject(DestroyRef);
  private minhaEntidadeService = inject(MinhaEntidadeService);
  private toast = inject(ToastService);
  private router = inject(Router);

  // Signals para estado reativo
  readonly isLoading = signal(false);
  readonly data = signal<MinhaEntidadeDto[]>([]);
  readonly total = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);

  constructor() {
    this.loadData();
  }

  loadData() {
    this.isLoading.set(true);
    this.minhaEntidadeService.getAll(
      this.searchTerm(),
      this.pageIndex() + 1,
      this.pageSize()
    ).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (result) => {
        this.data.set(result.data);
        this.total.set(result.total);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.toast.showError('Erro ao carregar dados');
        this.isLoading.set(false);
      }
    });
  }
}
```

#### 2. Services

**Padrão**: Services injetáveis com métodos tipados.

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { PagedResult } from '../../core/models/paged-result.model';

@Injectable({
  providedIn: 'root'
})
export class MinhaEntidadeService {
  private api = inject(ApiService);
  private http = inject(HttpClient);

  private readonly baseUrl = '/api/backoffice/minhaentidade';

  getAll(search?: string, page: number = 1, pageSize: number = 10, sort?: string, order?: string): Observable<PagedResult<MinhaEntidadeDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    
    if (search) params = params.set('search', search);
    if (sort) params = params.set('sort', sort);
    if (order) params = params.set('order', order);

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

#### 3. Template-Driven Forms

**Padrão**: Usar Template-Driven Forms para formulários simples.

```html
<form #form="ngForm" (ngSubmit)="onSubmit()">
  <mat-form-field appearance="outline" class="full-width">
    <mat-label>Nome</mat-label>
    <input matInput 
           name="nome" 
           [(ngModel)]="model.nome" 
           required 
           maxlength="100"
           #nome="ngModel">
    <mat-error *ngIf="nome.invalid && nome.touched">
      <span *ngIf="nome.errors?.['required']">O nome é obrigatório</span>
      <span *ngIf="nome.errors?.['maxlength']">Máximo de 100 caracteres</span>
    </mat-error>
  </mat-form-field>

  <div class="form-actions">
    <button mat-button type="button" (click)="onCancel()">Cancelar</button>
    <button mat-raised-button color="primary" type="submit" [disabled]="form.invalid || isLoading()">
      Salvar
    </button>
  </div>
</form>
```

#### 4. Loading States

**Padrão**: Usar o componente `LoadingComponent` global.

```html
<app-loading *ngIf="isLoading()"></app-loading>

<div *ngIf="!isLoading()">
  <!-- Conteúdo -->
</div>
```

## 📝 Guias Passo a Passo

### Criar um Novo CRUD no Backend

1. **Criar a Entidade** (`Models/Entities/MinhaEntidade.cs`)
   ```csharp
   public class MinhaEntidade
   {
       public long Id { get; set; }
       public string Nome { get; set; } = string.Empty;
       // ... outros campos
   }
   ```

2. **Adicionar ao DbContext** (`Data/ApplicationDbContext.cs`)
   ```csharp
   public DbSet<MinhaEntidade> MinhasEntidades { get; set; }
   ```

3. **Criar os DTOs** (`Models/DTOs/`)
   - `MinhaEntidadeDto.cs`
   - `CreateMinhaEntidadeRequest.cs`
   - `UpdateMinhaEntidadeRequest.cs`

4. **Criar a Interface do Service** (`Services/Interfaces/IMinhaEntidadeService.cs`)
   ```csharp
   public interface IMinhaEntidadeService 
       : IBaseService<MinhaEntidade, MinhaEntidadeDto, CreateMinhaEntidadeRequest, UpdateMinhaEntidadeRequest>
   {
   }
   ```

5. **Criar o Service** (`Services/Implementations/MinhaEntidadeService.cs`)
   - Herdar de `BaseService`
   - Implementar métodos abstratos
   - Adicionar validações específicas se necessário

6. **Criar o Controller** (`Controllers/Backoffice/MinhaEntidadeController.cs`)
   - Herdar de `BaseController`
   - Métodos CRUD já disponíveis automaticamente

7. **Registrar o Service** (`Extensions/ServiceCollectionExtensions.cs`)
   ```csharp
   services.AddScoped<IMinhaEntidadeService, MinhaEntidadeService>();
   ```

8. **Criar Migration**
   ```bash
   dotnet ef migrations add AddMinhaEntidade
   dotnet ef database update
   ```

### Criar um Novo CRUD no Frontend

1. **Criar o Service** (`features/minha-entidade/services/minha-entidade.service.ts`)
   - Seguir o padrão de `UsuarioService` ou `PerfilService`

2. **Criar o Model** (`core/models/minha-entidade.model.ts`)
   ```typescript
   export interface MinhaEntidadeDto {
     id: number;
     nome: string;
     // ... outros campos
   }
   ```

3. **Criar o Componente de Listagem** (`components/listagens/minha-entidade/minha-entidade-list.component.ts`)
   - Usar `OnPush` change detection
   - Usar signals para estado
   - Usar `takeUntilDestroyed` para subscriptions
   - Usar `LoadingComponent` para loading states

4. **Criar o Componente de Formulário** (`backoffice/components/cadastros/minha-entidade/minha-entidade-form.component.ts`)
   - Usar Template-Driven Forms
   - Usar `OnPush` change detection
   - Usar Navigation State para edição

5. **Adicionar Rotas** (`app.routes.ts`)
   ```typescript
   {
     path: "backoffice",
     component: ShellComponent,
     canActivate: [authGuard],
     children: [
       // ... outras rotas
       {
         path: "minha-entidade",
         loadComponent: () => import("./backoffice/components/listagens/minha-entidade/minha-entidade-list.component")
           .then(m => m.MinhaEntidadeListComponent)
       },
       {
         path: "minha-entidade/cadastro",
         canActivate: [stateGuard],
         loadComponent: () => import("./backoffice/components/cadastros/minha-entidade/minha-entidade-form.component")
           .then(m => m.MinhaEntidadeFormComponent)
       }
     ]
   }
   ```

6. **Adicionar ao Menu** (`shell/shell.component.ts`)
   ```typescript
   readonly navItems: NavItem[] = [
     // ... outros itens
     { icon: 'icon_name', label: 'Minha Entidade', route: '/minha-entidade' },
   ];
   ```

## 🔧 Configurações Importantes

### Backend

- **CORS**: Configurado em `appsettings.json` e aplicado via `AddApplicationCors`
- **JWT**: Configurado em `appsettings.json` com `SecretKey`, `Issuer`, `Audience`
- **Database**: PostgreSQL com connection string em `appsettings.json`
- **Health Checks**: Endpoint `/health` para verificação de saúde

### Frontend

- **API Base URL**: Configurado em `core/services/api.service.ts`
- **Tema**: Angular Material 3 com dark theme padrão
- **Responsividade**: Breakpoints do Angular CDK (`Breakpoints.Handset`, `Breakpoints.TabletPortrait`)
- **Loading**: CSS global em `styles.scss` (`.loading-container`)

## 📚 Recursos Adicionais

- [Documentação Angular](https://angular.dev)
- [Angular Material](https://material.angular.io)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)

## 📝 Guias Específicos

- **[Guia de Formulários](./GUIA_FORMULARIOS.md)** - Classes globais reutilizáveis para criar formulários consistentes e responsivos
- **[Padrões Angular 20](./ANGULAR_20_PADROES.md)** - Sintaxe moderna de controle de fluxo (@if, @for, @switch) e padrões de nomenclatura


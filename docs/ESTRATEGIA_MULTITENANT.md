# Estratégia Multitenant - Schema per Tenant

## 📋 Visão Geral

Sistema multitenant utilizando **Schema per Tenant** no PostgreSQL, onde:
- **Banco `sgr_config`**: Backoffice (gerenciamento de tenants, usuários admin, perfis admin)
- **Banco `sgr_tenants`**: Dados dos tenants (cada tenant em seu próprio schema)

## 🏗️ Estrutura de Bancos

### Banco `sgr_config` (Backoffice)
**Schema: `public`**
- `TipoPessoa` (compartilhado - usado apenas para referência no Tenant)
- `Tenant` (lista de tenants)
- `Usuario` (usuários admin do backoffice)
- `Perfil` (perfis do backoffice)

### Banco `sgr_tenants` (Tenants)
**Schemas: `{subdominio}_{id}`** (ex: `vangoghbar_1`, `vangoghcopa_2`)

Cada schema contém:
- `TipoPessoa` (PF e PJ - criados automaticamente)
- `Perfil` (perfis do tenant)
- `Usuario` (usuários do tenant)

## 📝 Entidades

### Tenant (banco `sgr_config`)
```csharp
public class Tenant
{
    public long Id { get; set; }
    public string RazaoSocial { get; set; }
    public string NomeFantasia { get; set; }
    public long TipoPessoaId { get; set; } // Referência (PF ou PJ)
    public string CpfCnpj { get; set; } // CNPJ ou CPF
    public string Subdominio { get; set; } // Ex: "vangoghbar"
    public string NomeSchema { get; set; } // Gerado: "vangoghbar_1"
    public bool IsAtivo { get; set; }
    // Campos de auditoria
}
```

### TipoPessoa (schema do tenant)
```csharp
public class TipoPessoa
{
    public long Id { get; set; }
    public string Nome { get; set; } // "Pessoa Física" ou "Pessoa Jurídica"
}
```

## 🔄 Fluxo de Criação do Tenant

1. **Validações**:
   - Validar CNPJ/CPF (formato + dígitos verificadores) - BrasilApi
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
   - Criar tabelas: `TipoPessoa`, `Perfil`, `Usuario`

6. **Inicializar Dados do Tenant**:
   - Criar TipoPessoa "Pessoa Física" (Id: 1)
   - Criar TipoPessoa "Pessoa Jurídica" (Id: 2)
   - Criar Perfil "Administrador" (IsAtivo: true)
   - Criar Usuario admin (com perfil Administrador)

## 🔐 Identificação do Tenant

### Em Produção
- Middleware lê header `Host` (ex: `vangoghbar.sgr.com.br`)
- Extrai subdomínio: `vangoghbar`
- Busca tenant no banco `sgr_config`
- Configura DbContext para usar schema do tenant

### Em Desenvolvimento
- Frontend envia header `X-Tenant-Subdomain` (via combobox no login)
- Middleware lê header e identifica tenant
- Configura DbContext para usar schema do tenant

## 🎨 Frontend

### Estrutura
```
app/
├── backoffice/          # Backoffice
│   ├── components/
│   │   ├── listagens/  # Componentes de listagem
│   │   │   ├── usuario/
│   │   │   ├── perfil/
│   │   │   └── tenants/
│   │   └── cadastros/  # Componentes de formulários
│   │       ├── usuario/
│   │       ├── perfil/
│   │       └── tenants/
│   └── login/          # Login do backoffice
├── tenant/              # Tenant
│   ├── components/     # Para futuros componentes do tenant
│   └── login/          # Login do tenant (com combobox)
├── core/               # Funcionalidades core
│   ├── guards/
│   ├── interceptors/
│   ├── services/
│   └── models/
├── features/           # Services por feature
│   ├── usuarios/
│   ├── perfis/
│   └── tenants/
├── shared/             # Componentes compartilhados
│   └── components/
│       └── loading/
└── shell/              # Layout principal
```

### Rotas
- `/backoffice/login` → Login do backoffice
- `/backoffice/dashboard` → Dashboard do backoffice
- `/backoffice/usuarios` → Listagem de usuários (backoffice)
- `/backoffice/perfis` → Listagem de perfis (backoffice)
- `/backoffice/tenants` → Listagem de tenants (backoffice)
- `/tenant/login` → Login do tenant (com combobox de tenants ativos)
- `/tenant/dashboard` → Dashboard do tenant

### Interceptor
- `tenantInterceptor`: Adiciona header `X-Tenant-Subdomain` em todas as requisições do tenant
- Configurado em `app.config.ts` via `provideHttpClient(withInterceptors([...]))`

## 📦 Dependências

### Backend
- Validação CNPJ/CPF: Usar biblioteca NuGet ou implementação própria
- PostgreSQL: Suporte a schemas

### Frontend
- Mesma estrutura atual (Angular Material 3)

## ✅ Validações

### Subdomínio
- Apenas letras minúsculas e números
- Único no banco
- Não pode ser alterado após criação

### CNPJ/CPF
- Validar formato (máscara)
- Validar dígitos verificadores
- Usar BrasilApi ou biblioteca NuGet

### Admin do Tenant
- Nome completo (obrigatório)
- Email (obrigatório, único no tenant)
- Senha (obrigatório, mínimo 6 caracteres)
- Confirmar senha (deve ser igual à senha)

## 🔧 Implementação

### Backend
1. ✅ Criar entidade `TipoPessoa` (schema do tenant)
2. ✅ Criar entidade `Tenant` (banco sgr_config)
3. ✅ Criar DTOs (`TenantDto`, `CreateTenantRequest`, `UpdateTenantRequest`, `CreateAdminRequest`)
4. ✅ Criar `TenantDbContext` (schema dinâmico para banco sgr_tenants)
5. ✅ Criar migrations para tabelas do tenant (via SQL direto no `TenantService`)
6. ✅ Criar `TenantService` com `CreateTenantAsync` (criação completa de tenant)
7. ✅ Integrar validação CNPJ/CPF (`CpfCnpjValidationService`)
8. ✅ Criar `TenantIdentificationMiddleware` (identifica tenant via header ou Host)
9. ✅ Atualizar `ApplicationDbContext` para incluir `Tenant`
10. ✅ Criar `TenantAuthService` para autenticação de tenants
11. ✅ Criar `TenantsController` no backoffice
12. ✅ Criar `AuthController` no tenant
13. ✅ Configurar endpoints: `/api/backoffice/auth/login` e `/api/tenant/auth/login`
14. ✅ Registrar `TenantDbContext` e services no `ServiceCollectionExtensions`
15. ✅ Configurar middleware no `Program.cs`

### Frontend
1. ✅ Reorganizar estrutura (backoffice/tenant/shared)
2. ✅ Criar componente login do tenant (`TenantLoginComponent`)
3. ✅ Criar componente login do backoffice (`BackofficeLoginComponent`)
4. ✅ Criar `TenantService` para buscar tenants
5. ✅ Configurar interceptor para header `X-Tenant-Subdomain` (`tenantInterceptor`)
6. ✅ Criar rotas separadas (`/backoffice/*` e `/tenant/*`)
7. ✅ Atualizar `AuthService` com métodos `loginBackoffice()` e `loginTenant()`
8. ✅ Atualizar `authGuard` para verificar contexto (backoffice/tenant)
9. ✅ Criar componentes de listagem e cadastro de tenants
10. ✅ Atualizar `ShellComponent` para exibir menu baseado no contexto


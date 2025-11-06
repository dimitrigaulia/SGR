# SGR - Sistema de Gerenciamento de Restaurantes

Sistema completo de gerenciamento desenvolvido com **Angular** (frontend) e **ASP.NET Core** (backend).

## 📚 Documentação

- **[Estrutura e Padrões](./docs/ESTRUTURA_E_PADROES.md)** - Guia completo sobre estrutura do projeto, padrões de código e passo a passo para criar novos componentes, controllers e services.
- **[Guia de Formulários](./docs/GUIA_FORMULARIOS.md)** - Classes globais reutilizáveis para criar formulários consistentes e responsivos.
- **[Padrões Angular 20](./docs/ANGULAR_20_PADROES.md)** - Sintaxe moderna de controle de fluxo (@if, @for, @switch) e padrões de nomenclatura recomendados.
- **[Progresso do Backoffice](./docs/PROGRESSO_BACKOFFICE.md)** - Histórico de implementações e funcionalidades concluídas.

## 🏗️ Estrutura do Projeto

```
SGR/
├── src/
│   └── SGR.Api/              # Backend ASP.NET Core
├── web/                      # Frontend Angular
└── docs/                     # Documentação do projeto
```

## 🚀 Tecnologias

### Backend
- **.NET 8** - Framework principal
- **ASP.NET Core Web API** - API REST
- **Entity Framework Core** - ORM
- **PostgreSQL** - Banco de dados
- **JWT** - Autenticação
- **BCrypt.Net** - Hash de senhas

### Frontend
- **Angular 20** - Framework principal
- **Angular Material 3** - Componentes UI
- **RxJS** - Programação reativa
- **TypeScript** - Linguagem

## 📋 Pré-requisitos

- **.NET 8 SDK**
- **Node.js 18+** e **npm**
- **PostgreSQL 14+**
- **Angular CLI 20+**

## 🔧 Configuração

### Backend

1. Configure a connection string do PostgreSQL em `src/SGR.Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=sgr;Username=postgres;Password=sua_senha"
  }
}
```

2. Configure o JWT em `appsettings.json`:
```json
{
  "Jwt": {
    "SecretKey": "sua_chave_secreta_super_segura_aqui",
    "Issuer": "SGR",
    "Audience": "SGR"
  }
}
```

3. Execute as migrations:
```bash
cd src/SGR.Api
dotnet ef database update
```

4. Execute a API:
```bash
dotnet run
```

### Frontend

1. Instale as dependências:
```bash
cd web
npm install
```

2. Configure a URL da API em `web/src/app/core/services/api.service.ts`:
```typescript
private readonly baseUrl = 'http://localhost:5000';
```

3. Execute o frontend:
```bash
ng serve
```

A aplicação estará disponível em `http://localhost:4200`.

## 📖 Guias Rápidos

### Criar um Novo CRUD

Consulte o guia completo em [ESTRUTURA_E_PADROES.md](./docs/ESTRUTURA_E_PADROES.md#-guias-passo-a-passo).

**Resumo Backend:**
1. Criar entidade
2. Criar DTOs (Dto, CreateRequest, UpdateRequest)
3. Criar interface e service (herdar de `BaseService`)
4. Criar controller (herdar de `BaseController`)
5. Registrar service em `ServiceCollectionExtensions`
6. Criar migration

**Resumo Frontend:**
1. Criar service em `features/`
2. Criar componente de listagem em `components/listagens/`
3. Criar componente de formulário em `components/cadastros/`
4. Adicionar rotas em `app.routes.ts`
5. Adicionar item no menu do shell

## 🎯 Padrões Principais

### Backend
- **CRUD Genérico**: Use `BaseController` e `BaseService` para operações padrão
- **DTOs**: Sempre use DTOs para comunicação com a API
- **Validação**: Data Annotations nos DTOs
- **Logging**: Use `ILogger` em todos os services e controllers
- **Exceções**: Use `BusinessException` e `NotFoundException` para erros de negócio

### Frontend
- **Standalone Components**: Todos os componentes são standalone
- **OnPush Change Detection**: Use em todos os componentes
- **Signals**: Use signals para estado reativo
- **Template-Driven Forms**: Padrão para formulários
- **takeUntilDestroyed**: Use para gerenciar subscriptions

## 📝 Estrutura de Pastas

### Backend
- `Controllers/` - Controllers da API
- `Services/` - Lógica de negócio
- `Models/` - Entidades e DTOs
- `Data/` - DbContext e configurações do EF
- `Extensions/` - Extension methods
- `Middleware/` - Middlewares customizados
- `Exceptions/` - Exceções customizadas

### Frontend
- `core/` - Funcionalidades core (guards, interceptors, services base)
- `shared/` - Componentes e recursos compartilhados
- `features/` - Services específicos de funcionalidades
- `components/` - Componentes de UI (listagens, cadastros)
- `shell/` - Layout principal da aplicação

## 🔐 Autenticação

O sistema usa JWT para autenticação. O token é armazenado no `localStorage` e enviado automaticamente via `AuthInterceptor`.

## 🎨 Tema

O sistema usa Angular Material 3 com tema escuro padrão. O tema pode ser alternado via `LayoutService`.

## 📦 Build

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

## 🧪 Testes

### Backend
```bash
cd src/SGR.Api
dotnet test
```

### Frontend
```bash
cd web
ng test
```

## 📄 Licença

Este projeto é privado e de uso interno.

## 👥 Contribuindo

Siga os padrões documentados em [ESTRUTURA_E_PADROES.md](./docs/ESTRUTURA_E_PADROES.md) ao contribuir com o projeto.


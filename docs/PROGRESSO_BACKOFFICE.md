# Andamento Backoffice — SGR

Data: 2025-11-05

## Escopo concluído

- Tema Angular Material (dark nativo) com alternância clara/escura por classe (`.dark-theme`/`.light-theme`).
- Normalização de encoding (UTF‑8) e correção de acentuação em arquivos críticos (HTML/TS/CS).
- Correção do fluxo de login (redirect para `/dashboard`) e botão Sair (logout + navegação para login).
- Padronização do layout (Shell) para tokens de app do Material (`--mat-app-background-color`, `--mat-app-text-color`).
- Backend (ASP.NET Core + EF Core/PostgreSQL):
  - CRUD Perfis (`/api/backoffice/perfis`): listar, obter, criar, atualizar, excluir.
  - CRUD Usuários (`/api/backoffice/usuarios`): listar, obter, criar, atualizar (inclui troca de senha opcional), excluir.
  - Services adicionados: `IPerfilService`/`PerfilService`, `IUsuarioService`/`UsuarioService`.
  - Registro de serviços no `Program.cs` e proteção via JWT `[Authorize]`.
- Frontend (Angular):
  - Rotas lazy de backoffice: `/usuarios`, `/usuarios/novo`, `/usuarios/:id/editar`, `/perfis`, `/perfis/novo`, `/perfis/:id/editar`.
  - Páginas (standalone): listas e formulários com Angular Material.
  - Services front: `UsuarioService`, `PerfilService` com chamadas ao backend.
  - Sidebar ajustada para: Dashboard, Usuários, Perfis.

## Endpoints (resumo)

- `GET /api/backoffice/perfis?search=` — lista
- `GET /api/backoffice/perfis/{id}` — detalhe
- `POST /api/backoffice/perfis` — cria
- `PUT /api/backoffice/perfis/{id}` — atualiza
- `DELETE /api/backoffice/perfis/{id}` — exclui

- `GET /api/backoffice/usuarios?search=` — lista
- `GET /api/backoffice/usuarios/{id}` — detalhe
- `POST /api/backoffice/usuarios` — cria (hash Bcrypt)
- `PUT /api/backoffice/usuarios/{id}` — atualiza (troca de senha opcional)
- `DELETE /api/backoffice/usuarios/{id}` — exclui

## Próximos passos sugeridos

- Validar UI/UX dos formulários (mensagens, validações sincrônicas assíncronas como e‑mail único).
- Adicionar paginação/ordenação na listagem (backend e frontend).
- Auditar campos de criação/atualização (usuário logado) onde aplicável.
- Iniciar cadastro de Restaurantes no backoffice e, ao salvar, provisionar banco “clone” (template).

## Observações

- Migrations existentes atendem as entidades `Usuario` e `Perfil` conforme DDL fornecida.
- Caso necessário, normalizar texto remanescente com acentos em arquivos menos críticos.



## Atualizações adicionais em 2025-11-05
- Auditoria padronizada em Usuario (UsuarioCriacao, DataCriacao) + migration AddUsuarioAuditFields criada.
- Bloqueio de exclusão de Perfil com usuários vinculados (HTTP 409).
- Reorganização Angular: listagens/ e cadastros/ por contexto (usuarios, perfis).
- Rotas lazy atualizadas e sidebar ajustada para o escopo atual.
- Rotas de edição sem ID na URL (usa Navigation state); servidor continua responsável por autorização.
- Snackbars padronizados via ToastService (sucesso/erro/info) aplicados em criar/atualizar/excluir.
- Avatar de usuário (campo PathImagem) em cadastro e listagem, com preview.

- Rotas renomeadas para 'cadastro' (criar/editar) e suporte a visualização por query param ?view=1 (campos desabilitados) nas telas de Usuários e Perfis.

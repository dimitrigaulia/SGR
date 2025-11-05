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
  - Rotas lazy de backoffice: `/usuarios`, `/usuarios/cadastro`, `/perfis`, `/perfis/cadastro` (uso de Navigation State em vez de IDs na URL).
  - Guard `stateGuard` impede abrir "visualizar" sem `state.id`.
  - Páginas (standalone) com Angular Material; formulários migrados para Template‑Driven Forms (Usuários e Perfis).
  - Listagens com paginação, ordenação e busca server‑side (debounce) para Usuários e Perfis.
  - Avatar de usuário com preview, upload (PNG/JPG) e remoção; limpeza automática do avatar antigo ao salvar edição.
  - Services front: `UsuarioService`, `PerfilService`, `UploadService`.
  - Toasts padronizados via `ToastService` (sucesso/erro/info).
  - Sidebar sempre reabrível (botão de menu na toolbar); strings PT‑BR normalizadas.

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

- Uploads (avatar)
  - `POST /api/uploads/avatar` — envia arquivo (PNG/JPG), retorna `{ url }`
  - `DELETE /api/uploads/avatar?url=...` — remove arquivo por URL (ou `?name=...`)

## Próximos passos sugeridos

- Validar UI/UX dos formulários (consolidar `mat-error` onde faltar; mensagens PT‑BR).
- Baseline de migrations do EF para o estado atual do banco e remoção de patches ad‑hoc.
- Auditar campos de criação/atualização (usuário logado) em todas as entidades.
- Iniciar cadastro de Restaurantes no backoffice e, ao salvar, provisionar banco “clone” (template).

## Observações

- Migrations: gerar/rodar localmente (CLI `dotnet` indisponível aqui para build).
- Encoding: revisadas telas principais; normalizar qualquer resquício para UTF‑8 conforme encontrado.



## Atualizações adicionais em 2025-11-05
- Auditoria padronizada em Usuario (UsuarioCriacao, DataCriacao) + migration AddUsuarioAuditFields criada.
- Bloqueio de exclusão de Perfil com usuários vinculados (HTTP 409).
- Reorganização Angular: listagens/ e cadastros/ por contexto (usuarios, perfis).
- Rotas lazy atualizadas e sidebar ajustada para o escopo atual.
- Rotas de cadastro sem ID na URL (usa Navigation State); servidor continua responsável por autorização.
- Snackbars padronizados via ToastService (sucesso/erro/info) aplicados em criar/atualizar/excluir.
- Avatar de usuário (campo PathImagem) em cadastro e listagem, com preview e limpeza do arquivo anterior ao salvar.
- Migração de formulários para Template‑Driven (Usuários e Perfis); `perfil-form.component.html` atualizado.

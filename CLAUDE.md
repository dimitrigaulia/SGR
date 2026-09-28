# SGR

Sistema com API .NET + Web Angular + Docker compose (Caddy reverso, nginx servindo Angular, API .NET).

## Stack
- **API**: `src/SGR.Api/` — .NET
- **Tests**: `src/SGR.Tests/`
- **Web**: `web/` — Angular (separado da pasta `src`)
- **Solution**: `SGR.slnx` (formato novo XML solution)
- **Containers**: `Dockerfile.api`, `Dockerfile.web`, `Dockerfile.nginx`
- **Proxy**: Caddy (`caddy/`) + nginx (`nginx/`)
- **Orquestração**: `docker-compose.yml`

## Comandos
- Backend: `dotnet build SGR.slnx` / `dotnet test`
- Frontend: `cd web && npm start` / `npx ng build`
- Stack completa: `docker compose up --build`

## Convenções
- `plan.md` na raiz — roadmap/decisões arquiteturais (consultar antes de mudança grande)
- Separação clara `src/` (backend) vs `web/` (frontend) — não misturar

## Anti-patterns
- Não editar `Dockerfile.*` sem rebuild local da imagem
- Não trocar Caddy/nginx sem entender fluxo proxy (web → nginx → caddy → API)

## Testes: adoção do padrão hermético

Hoje: `src/SGR.Tests`, sem Testcontainers. O repo ainda não está no padrão global. Teste novo de integração nasce em Testcontainers (mesmo motor/versão do banco de produção), com `TimeProvider` e serviço externo atrás de porta com fake.

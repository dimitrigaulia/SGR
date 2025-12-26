# Guia de Deploy - SGR na VPS Hostinger

Este documento descreve o processo completo de deploy do sistema SGR na VPS da Hostinger.

## Pré-requisitos

- VPS Hostinger com Ubuntu 24.04 LTS
- Docker instalado
- Docker Compose instalado
- Acesso SSH à VPS (root@31.97.247.109)
- Senha root: definida no painel da Hostinger

## Informações da VPS

- **IP**: 31.97.247.109
- **Hostname**: srv1227036.hstgr.cloud
- **SO**: Ubuntu 24.04 LTS
- **Usuário SSH**: root
- **PostgreSQL**: usuário e senha definidos no arquivo `.env`

## Arquitetura do Deploy

O deploy utiliza Docker Compose com os seguintes serviços:

```
┌─────────────────┐
│   Nginx (80/443) │  ← Reverse Proxy
└────────┬─────────┘
         │
    ┌────┴────┐
    │         │
┌───▼───┐ ┌──▼────┐
│  API  │ │  Web  │  ← Backend .NET e Frontend Angular
└───┬───┘ └───────┘
    │
┌───▼────────┐
│ PostgreSQL │  ← Banco de dados
└────────────┘
```

### Serviços

1. **postgres**: PostgreSQL 16 (porta interna 5432)
   - Bancos: `sgr_config` e `sgr_tenants`
   - Volume persistente para dados

2. **api**: Backend .NET 9.0 (porta interna 5000)
   - Aplica migrations automaticamente
   - Inicializa dados padrão

3. **web**: Frontend Angular (porta interna 80)
   - Build de produção servido via Nginx

4. **nginx**: Reverse Proxy (portas 80/443)
   - Roteia `/api/*` para backend
   - Serve frontend em `/`

## Passo a Passo do Deploy

### 1. Conectar à VPS

```bash
ssh root@31.97.247.109
# Senha: definida no painel da Hostinger
```

### 2. Instalar Docker e Docker Compose (se necessário)

```bash
# Atualizar sistema
apt update && apt upgrade -y

# Instalar Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Instalar Docker Compose
apt install docker-compose-plugin -y

# Verificar instalação
docker --version
docker compose version
```

### 3. Preparar Diretório do Projeto

```bash
# Criar diretório para o projeto
mkdir -p /opt/sgr
cd /opt/sgr

# Ou clonar do repositório Git (se aplicável)
# git clone <seu-repositorio> .
```

### 4. Enviar Arquivos do Projeto

Você pode usar `scp` ou `rsync` para enviar os arquivos:

```bash
# Do seu computador local (Windows PowerShell ou Linux)
scp -r . root@31.97.247.109:/opt/sgr/

# Ou usando rsync (mais eficiente)
rsync -avz --exclude 'node_modules' --exclude 'bin' --exclude 'obj' --exclude '.git' . root@31.97.247.109:/opt/sgr/
```

**Arquivos necessários:**
- `Dockerfile.api`
- `Dockerfile.web`
- `.dockerignore`
- `docker-compose.yml`
- `nginx/` (diretório completo)
- `src/` (diretório completo)
- `web/` (diretório completo)
- `scripts/init-db.sql`

### 5. Executar Deploy

Na VPS:

```bash
cd /opt/sgr

# Dar permissão de execução ao script (se usar)
chmod +x deploy.sh

# Executar deploy
./deploy.sh

# Ou manualmente:
docker compose build --no-cache
docker compose up -d
```

### 6. Verificar Status

```bash
# Ver status dos containers
docker compose ps

# Ver logs
docker compose logs -f

# Ver logs de um serviço específico
docker compose logs -f api
docker compose logs -f web
docker compose logs -f nginx
docker compose logs -f postgres
```

### 7. Testar Aplicação

Acesse no navegador:
- **Frontend**: http://31.97.247.109
- **API Health**: http://31.97.247.109/health
- **API Swagger** (se habilitado): http://31.97.247.109/swagger

## Configurações Importantes

### Connection Strings

As connection strings estão configuradas para usar o serviço `postgres` do Docker Compose e são definidas via variáveis de ambiente no `docker-compose.yml`:

```
Host=postgres;Port=5432;Database=sgr_config;Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD};
```

Os valores de `POSTGRES_USER` e `POSTGRES_PASSWORD` vêm do arquivo `.env` (não versionado).

### CORS

O CORS está configurado para permitir:
- `http://31.97.247.109`
- `https://31.97.247.109`
- `http://localhost:4200` (desenvolvimento)

### Multitenancy

O sistema funciona via header `X-Tenant-Subdomain` quando acessado por IP. Quando o DNS estiver configurado, funcionará também por subdomínio.

## Comandos Úteis

### Gerenciamento de Containers

```bash
# Parar todos os serviços
docker compose down

# Parar e remover volumes (CUIDADO: apaga dados do banco)
docker compose down -v

# Reiniciar um serviço específico
docker compose restart api

# Rebuild e restart
docker compose up -d --build api
```

### Logs

```bash
# Ver logs de todos os serviços
docker compose logs -f

# Ver últimas 100 linhas
docker compose logs --tail=100

# Ver logs de um serviço específico
docker compose logs -f api
```

### Banco de Dados

```bash
# Conectar ao PostgreSQL
docker compose exec postgres psql -U postgres -d sgr_config

# Backup do banco
docker compose exec postgres pg_dump -U postgres sgr_config > backup_config.sql
docker compose exec postgres pg_dump -U postgres sgr_tenants > backup_tenants.sql

# Restaurar backup
docker compose exec -T postgres psql -U postgres sgr_config < backup_config.sql
```

### Atualizar Aplicação

```bash
# 1. Fazer pull das alterações (se usar Git)
git pull

# 2. Rebuild das imagens
docker compose build --no-cache

# 3. Recriar containers
docker compose up -d --force-recreate

# 4. Verificar logs
docker compose logs -f
```

## Troubleshooting

### Container não inicia

```bash
# Ver logs detalhados
docker compose logs <nome-do-servico>

# Verificar se porta está em uso
netstat -tulpn | grep :80
netstat -tulpn | grep :5432
```

### Erro de conexão com banco

```bash
# Verificar se PostgreSQL está rodando
docker compose ps postgres

# Verificar logs do PostgreSQL
docker compose logs postgres

# Testar conexão
docker compose exec postgres psql -U postgres -c "SELECT version();"
```

### Erro de permissão

```bash
# Ajustar permissões (se necessário)
chmod -R 755 /opt/sgr
```

### Limpar tudo e recomeçar

```bash
# CUIDADO: Isso remove todos os dados!
docker compose down -v
docker system prune -a
docker compose build --no-cache
docker compose up -d
```

## Configuração de DNS (Futuro)

Quando o DNS estiver pronto:

1. Atualizar `nginx/default.conf` com o domínio
2. Configurar SSL (Let's Encrypt)
3. Atualizar `appsettings.Production.json` com novos origins no CORS
4. Atualizar `environment.production.ts` com a URL do domínio

### Exemplo de configuração SSL com Let's Encrypt

```bash
# Instalar Certbot
apt install certbot python3-certbot-nginx -y

# Obter certificado
certbot --nginx -d seu-dominio.com.br

# Renovação automática
certbot renew --dry-run
```

## Segurança

### Firewall

Configure o firewall para permitir apenas portas necessárias:

```bash
# Instalar UFW (se não estiver instalado)
apt install ufw -y

# Permitir SSH
ufw allow 22/tcp

# Permitir HTTP e HTTPS
ufw allow 80/tcp
ufw allow 443/tcp

# Ativar firewall
ufw enable

# Ver status
ufw status
```

### Senhas

**IMPORTANTE**: Configure senhas fortes em produção:

1. **PostgreSQL**: Defina `POSTGRES_PASSWORD` no arquivo `.env` (não versionado)
2. **JWT Secret Key**: Defina `JWT_SECRET` no arquivo `.env` (não versionado)
3. **Root da VPS**: Altere a senha padrão no painel da Hostinger

**Nunca commite o arquivo `.env` no repositório!** Use `.env.example` como referência.

### Backup

Configure backups automáticos do banco de dados:

```bash
# Criar script de backup
cat > /opt/sgr/backup.sh << 'EOF'
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="/opt/sgr/backups"
mkdir -p $BACKUP_DIR

docker compose exec -T postgres pg_dump -U postgres sgr_config > $BACKUP_DIR/sgr_config_$DATE.sql
docker compose exec -T postgres pg_dump -U postgres sgr_tenants > $BACKUP_DIR/sgr_tenants_$DATE.sql

# Manter apenas últimos 7 dias
find $BACKUP_DIR -name "*.sql" -mtime +7 -delete
EOF

chmod +x /opt/sgr/backup.sh

# Adicionar ao crontab (backup diário às 2h da manhã)
crontab -e
# Adicionar linha:
# 0 2 * * * /opt/sgr/backup.sh
```

## Monitoramento

### Health Checks

Os serviços têm health checks configurados. Verifique:

```bash
# Health check do backend
curl http://31.97.247.109/health

# Status dos containers
docker compose ps
```

### Recursos do Sistema

```bash
# Uso de recursos
docker stats

# Espaço em disco
df -h
docker system df
```

## Suporte

Em caso de problemas:

1. Verifique os logs: `docker compose logs -f`
2. Verifique o status: `docker compose ps`
3. Verifique recursos: `docker stats`
4. Consulte este documento

## Próximos Passos

- [ ] Configurar DNS
- [ ] Configurar SSL/HTTPS
- [ ] Configurar backups automáticos
- [ ] Configurar monitoramento
- [ ] Otimizar configurações de produção
- [ ] Configurar CI/CD (opcional)


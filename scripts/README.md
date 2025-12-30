# Scripts

Este diretório contém scripts essenciais para a inicialização e configuração do sistema.

## Arquivos

### `init-db.sql`

Script de inicialização dos bancos de dados PostgreSQL. Este script é executado automaticamente quando o container PostgreSQL é criado pela primeira vez.

**O que faz:**
- Cria o banco de dados `sgr_config` (backoffice)
- Cria o banco de dados `sgr_tenants` (multitenancy)
- Cria a extensão `uuid-ossp` em ambos os bancos

**Uso:**
O script é montado automaticamente no container PostgreSQL via `docker-compose.yml`:
```yaml
volumes:
  - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init-db.sql:ro
```

O PostgreSQL executa automaticamente todos os scripts em `/docker-entrypoint-initdb.d/` na primeira inicialização.



#!/bin/bash

# Script de deploy do SGR na VPS Hostinger
# Uso: ./deploy.sh

set -e  # Parar em caso de erro

echo "=========================================="
echo "  Deploy do SGR na VPS Hostinger"
echo "=========================================="
echo ""

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Verificar se Docker está instalado
if ! command -v docker &> /dev/null; then
    echo -e "${RED}Erro: Docker não está instalado.${NC}"
    exit 1
fi

# Verificar se Docker Compose está instalado
if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
    echo -e "${RED}Erro: Docker Compose não está instalado.${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Docker e Docker Compose encontrados${NC}"
echo ""

# Parar containers existentes (se houver)
echo "Parando containers existentes..."
docker-compose down 2>/dev/null || docker compose down 2>/dev/null || true
echo -e "${GREEN}✓ Containers parados${NC}"
echo ""

# Remover imagens antigas (opcional - descomentar se necessário)
# echo "Removendo imagens antigas..."
# docker-compose rm -f 2>/dev/null || docker compose rm -f 2>/dev/null || true

# Build das imagens
echo "Construindo imagens Docker..."
if command -v docker-compose &> /dev/null; then
    docker-compose build --no-cache
else
    docker compose build --no-cache
fi
echo -e "${GREEN}✓ Imagens construídas${NC}"
echo ""

# Iniciar serviços
echo "Iniciando serviços..."
if command -v docker-compose &> /dev/null; then
    docker-compose up -d
else
    docker compose up -d
fi
echo -e "${GREEN}✓ Serviços iniciados${NC}"
echo ""

# Aguardar serviços ficarem prontos
echo "Aguardando serviços ficarem prontos..."
sleep 10

# Verificar status dos containers
echo ""
echo "Status dos containers:"
if command -v docker-compose &> /dev/null; then
    docker-compose ps
else
    docker compose ps
fi

echo ""
echo -e "${GREEN}=========================================="
echo "  Deploy concluído com sucesso!"
echo "==========================================${NC}"
echo ""
echo "Acesse a aplicação em: http://31.97.247.109"
echo ""
echo "Para ver os logs:"
echo "  docker-compose logs -f"
echo ""
echo "Para parar os serviços:"
echo "  docker-compose down"
echo ""


#!/bin/bash

# Script para renovação manual do certificado wildcard *.fichapro.ia.br
# Requer criação manual de registro TXT no DNS (Registro.br)

set -e

DOMAIN="fichapro.ia.br"
WILDCARD_DOMAIN="*.${DOMAIN}"

echo "=========================================="
echo "Renovação de Certificado SSL/TLS"
echo "=========================================="
echo ""
echo "Este script renovará o certificado que cobre:"
echo "  - ${DOMAIN} (domínio raiz)"
echo "  - www.${DOMAIN} (www)"
echo "  - ${WILDCARD_DOMAIN} (todos os subdomínios)"
echo ""

# Verificar se o certificado existe
if [ ! -d "/var/lib/docker/volumes/sgr_letsencrypt/_data/live/${DOMAIN}" ]; then
    echo "Certificado não encontrado. Use o script certbot-issue-wildcard.sh para emitir."
    exit 1
fi

# Verificar validade atual
echo "Verificando validade do certificado atual..."
docker compose run --rm certbot certificates

echo ""
read -p "Deseja renovar o certificado agora? (s/N): " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Ss]$ ]]; then
    echo "Renovação cancelada."
    exit 0
fi

echo ""
echo "Iniciando renovação do certificado via DNS-01..."
echo ""
echo "O Certbot solicitará a criação de um registro TXT no DNS."
echo "Você precisará:"
echo "  1. Copiar o valor TXT fornecido pelo Certbot"
echo "  2. Acessar o Registro.br"
echo "  3. Atualizar o registro TXT para _acme-challenge.${DOMAIN}"
echo "  4. Aguardar a propagação DNS (pode levar alguns minutos)"
echo "  5. Pressionar ENTER no terminal quando o registro estiver atualizado"
echo ""
read -p "Pressione ENTER quando estiver pronto para começar..."

# Executar certbot em modo manual DNS para renovação
docker compose run --rm certbot certonly \
    --manual \
    --preferred-challenges dns \
    --agree-tos \
    --no-eff-email \
    --manual-public-ip-logging-ok \
    --email admin@${DOMAIN} \
    --force-renewal \
    -d "${DOMAIN}" \
    -d "www.${DOMAIN}" \
    -d "${WILDCARD_DOMAIN}"

if [ $? -eq 0 ]; then
    echo ""
    echo "=========================================="
    echo "✓ Certificado renovado com sucesso!"
    echo "=========================================="
    echo ""
    echo "Reiniciando Nginx para aplicar o novo certificado..."
    docker compose restart nginx
    
    echo ""
    echo "Verificando se o Nginx reiniciou corretamente..."
    sleep 3
    
    if docker compose ps nginx | grep -q "Up"; then
        echo "✓ Nginx reiniciado com sucesso!"
        echo ""
        echo "Próximos passos:"
        echo "  1. Verificar se o certificado está funcionando:"
        echo "     curl -I https://${DOMAIN}"
        echo ""
        echo "  2. Verificar validade do certificado:"
        echo "     ./scripts/certbot-check-expiry.sh"
        echo ""
    else
        echo "⚠ Atenção: Nginx pode não ter reiniciado corretamente."
        echo "Verifique manualmente: docker compose ps nginx"
    fi
else
    echo ""
    echo "=========================================="
    echo "✗ Erro ao renovar certificado"
    echo "=========================================="
    echo ""
    echo "Verifique:"
    echo "  - Se o registro TXT foi atualizado corretamente no DNS"
    echo "  - Se o DNS propagou (pode levar alguns minutos)"
    echo "  - Se o domínio está apontando para este servidor"
    echo ""
    exit 1
fi


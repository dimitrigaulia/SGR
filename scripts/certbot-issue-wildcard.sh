#!/bin/bash

# Script para emissão inicial do certificado wildcard *.fichapro.ia.br
# Requer criação manual de registro TXT no DNS (Registro.br)

set -e

DOMAIN="fichapro.ia.br"
WILDCARD_DOMAIN="*.${DOMAIN}"

echo "=========================================="
echo "Emissão de Certificado SSL/TLS"
echo "=========================================="
echo ""
echo "Este script emitirá um certificado que cobre:"
echo "  - ${DOMAIN} (domínio raiz)"
echo "  - www.${DOMAIN} (www)"
echo "  - ${WILDCARD_DOMAIN} (todos os subdomínios)"
echo ""
read -p "Pressione ENTER para continuar ou CTRL+C para cancelar..."

# Verificar se o Nginx está rodando
if ! docker compose ps nginx | grep -q "Up"; then
    echo "Iniciando container Nginx..."
    docker compose up -d nginx
    sleep 5
fi

echo ""
echo "Iniciando emissão do certificado via DNS-01..."
echo ""
echo "O Certbot solicitará a criação de um registro TXT no DNS."
echo "Você precisará:"
echo "  1. Copiar o valor TXT fornecido pelo Certbot"
echo "  2. Acessar o Registro.br"
echo "  3. Adicionar um registro TXT para _acme-challenge.${DOMAIN}"
echo "  4. Aguardar a propagação DNS (pode levar alguns minutos)"
echo "  5. Pressionar ENTER no terminal quando o registro estiver criado"
echo ""
read -p "Pressione ENTER quando estiver pronto para começar..."

# Executar certbot em modo manual DNS
docker compose run --rm certbot certonly \
    --manual \
    --preferred-challenges dns \
    --agree-tos \
    --no-eff-email \
    --manual-public-ip-logging-ok \
    --email admin@${DOMAIN} \
    -d "${DOMAIN}" \
    -d "www.${DOMAIN}" \
    -d "${WILDCARD_DOMAIN}"

if [ $? -eq 0 ]; then
    echo ""
    echo "=========================================="
    echo "✓ Certificado emitido com sucesso!"
    echo "=========================================="
    echo ""
    echo "Certificado localizado em:"
    echo "  /etc/letsencrypt/live/${DOMAIN}/"
    echo ""
    echo "Próximos passos:"
    echo "  1. Reiniciar o Nginx:"
    echo "     docker compose restart nginx"
    echo ""
    echo "  2. Verificar se o certificado está funcionando:"
    echo "     curl -I https://${DOMAIN}"
    echo ""
    echo "  3. Verificar validade do certificado:"
    echo "     ./scripts/certbot-check-expiry.sh"
    echo ""
else
    echo ""
    echo "=========================================="
    echo "✗ Erro ao emitir certificado"
    echo "=========================================="
    echo ""
    echo "Verifique:"
    echo "  - Se o registro TXT foi criado corretamente no DNS"
    echo "  - Se o DNS propagou (pode levar alguns minutos)"
    echo "  - Se o domínio está apontando para este servidor"
    echo ""
    exit 1
fi


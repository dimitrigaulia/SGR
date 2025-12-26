#!/bin/bash

# Script para verificar validade do certificado SSL/TLS

set -e

DOMAIN="fichapro.ia.br"
CERT_PATH="/etc/letsencrypt/live/${DOMAIN}/fullchain.pem"

echo "=========================================="
echo "Verificação de Validade do Certificado SSL"
echo "=========================================="
echo ""

# Verificar se o certificado existe
if ! docker compose exec -T nginx test -f "${CERT_PATH}" 2>/dev/null; then
    echo "⚠ Certificado não encontrado em: ${CERT_PATH}"
    echo ""
    echo "Use o script certbot-issue-wildcard.sh para emitir o certificado."
    exit 1
fi

echo "Domínio: ${DOMAIN}"
echo "Caminho do certificado: ${CERT_PATH}"
echo ""

# Extrair informações do certificado
echo "Informações do Certificado:"
echo "----------------------------"

# Obter data de expiração
EXPIRY_DATE=$(docker compose exec -T nginx openssl x509 -in "${CERT_PATH}" -noout -enddate 2>/dev/null | cut -d= -f2)
EXPIRY_EPOCH=$(date -d "${EXPIRY_DATE}" +%s 2>/dev/null || date -j -f "%b %d %H:%M:%S %Y %Z" "${EXPIRY_DATE}" +%s 2>/dev/null)
CURRENT_EPOCH=$(date +%s)
DAYS_UNTIL_EXPIRY=$(( (EXPIRY_EPOCH - CURRENT_EPOCH) / 86400 ))

# Obter assunto do certificado
SUBJECT=$(docker compose exec -T nginx openssl x509 -in "${CERT_PATH}" -noout -subject 2>/dev/null | sed 's/subject=//')

# Obter emissor
ISSUER=$(docker compose exec -T nginx openssl x509 -in "${CERT_PATH}" -noout -issuer 2>/dev/null | sed 's/issuer=//')

echo "Assunto: ${SUBJECT}"
echo "Emissor: ${ISSUER}"
echo "Data de Expiração: ${EXPIRY_DATE}"
echo ""

# Calcular dias até expiração
if [ ${DAYS_UNTIL_EXPIRY} -gt 0 ]; then
    echo "Status: ✓ Válido"
    echo "Dias até expiração: ${DAYS_UNTIL_EXPIRY}"
    echo ""
    
    # Alertas
    if [ ${DAYS_UNTIL_EXPIRY} -lt 30 ]; then
        echo "⚠⚠⚠ ATENÇÃO: Certificado expira em menos de 30 dias!"
        echo ""
        echo "Renove o certificado usando:"
        echo "  ./scripts/certbot-renew-wildcard.sh"
        echo ""
    elif [ ${DAYS_UNTIL_EXPIRY} -lt 60 ]; then
        echo "⚠ Atenção: Certificado expira em menos de 60 dias."
        echo "Considere renovar em breve usando:"
        echo "  ./scripts/certbot-renew-wildcard.sh"
        echo ""
    else
        echo "Certificado válido por mais de 60 dias. Nenhuma ação necessária."
        echo ""
    fi
else
    echo "Status: ✗ EXPIRADO"
    echo "O certificado expirou há $(( -DAYS_UNTIL_EXPIRY )) dias."
    echo ""
    echo "Renove imediatamente usando:"
    echo "  ./scripts/certbot-renew-wildcard.sh"
    echo ""
    exit 1
fi

# Mostrar certificados disponíveis
echo "Certificados disponíveis:"
echo "----------------------------"
docker compose run --rm certbot certificates
echo ""


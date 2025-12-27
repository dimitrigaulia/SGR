#!/bin/sh
set -eu

DOMAIN="${DOMAIN:-fichapro.ia.br}"
EMAIL="${EMAIL:-admin@fichapro.ia.br}"
WEBROOT="/var/www/certbot"

mkdir -p "$WEBROOT"

# Emissão inicial (só se não existir ainda)
if [ ! -f "/etc/letsencrypt/live/${DOMAIN}/fullchain.pem" ]; then
  echo "[certbot] Emitindo certificado inicial para ${DOMAIN} e www.${DOMAIN} (HTTP-01 webroot)..."
  certbot certonly \
    --non-interactive \
    --webroot -w "$WEBROOT" \
    -d "$DOMAIN" -d "www.${DOMAIN}" \
    --email "$EMAIL" \
    --agree-tos --no-eff-email
else
  echo "[certbot] Certificado já existe. Pulando emissão inicial."
fi

# Loop de renovação
while :; do
  echo "[certbot] Rodando renew..."
  certbot renew --non-interactive || true
  sleep 12h
done


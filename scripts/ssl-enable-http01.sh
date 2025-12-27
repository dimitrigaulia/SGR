#!/usr/bin/env bash
set -euo pipefail

DOMAIN="fichapro.ia.br"
EMAIL="admin@fichapro.ia.br"

cd "$(dirname "$0")/.."

echo "[1/4] Aplicando config HTTP..."
cp -f nginx/default.http.conf nginx/default.conf

echo "[2/4] Subindo nginx..."
docker compose up -d nginx

echo "[3/4] Emitindo certificado (HTTP-01 webroot)..."
docker compose run --rm certbot certonly \
  --webroot -w /var/www/certbot \
  -d "$DOMAIN" -d "www.$DOMAIN" \
  --email "$EMAIL" \
  --agree-tos --no-eff-email

echo "[4/4] Ativando HTTPS e reiniciando nginx..."
cp -f nginx/default.https.conf nginx/default.conf
docker compose restart nginx

echo "OK: HTTPS ativo em https://$DOMAIN e https://www.$DOMAIN"

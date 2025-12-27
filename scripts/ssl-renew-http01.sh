#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

echo "Renovando certificado SSL/TLS via HTTP-01..."
docker compose run --rm certbot renew --webroot -w /var/www/certbot

echo "Recarregando configuração do Nginx..."
docker compose exec -T nginx nginx -s reload || docker compose restart nginx

echo "OK: Certificado renovado e Nginx recarregado"
#!/bin/sh
set -eu

DOMAIN="${DOMAIN:-fichapro.ia.br}"

CERTS_DIR="/etc/nginx/certs"
SELF_DIR="/etc/ssl/selfsigned"
LE_FULL="/etc/letsencrypt/live/${DOMAIN}/fullchain.pem"
LE_KEY="/etc/letsencrypt/live/${DOMAIN}/privkey.pem"

mkdir -p "$CERTS_DIR" "$SELF_DIR"

make_self_signed() {
  if [ ! -f "$SELF_DIR/fullchain.pem" ] || [ ! -f "$SELF_DIR/privkey.pem" ]; then
    echo "[nginx] Gerando certificado temporário (self-signed) para evitar conexão recusada..."
    openssl req -x509 -nodes -newkey rsa:2048 \
      -days 7 \
      -subj "/CN=${DOMAIN}" \
      -keyout "$SELF_DIR/privkey.pem" \
      -out "$SELF_DIR/fullchain.pem" >/dev/null 2>&1
  fi
}

link_self() {
  ln -sf "$SELF_DIR/fullchain.pem" "$CERTS_DIR/fullchain.pem"
  ln -sf "$SELF_DIR/privkey.pem" "$CERTS_DIR/privkey.pem"
}

link_le() {
  ln -sf "$LE_FULL" "$CERTS_DIR/fullchain.pem"
  ln -sf "$LE_KEY" "$CERTS_DIR/privkey.pem"
}

# 1) Garante que existe algum cert para o Nginx subir com 443
make_self_signed

if [ -f "$LE_FULL" ] && [ -f "$LE_KEY" ]; then
  echo "[nginx] Certificado Let's Encrypt encontrado. Usando LE."
  link_le
else
  echo "[nginx] Certificado Let's Encrypt ainda não existe. Usando self-signed temporário."
  link_self
fi

# 2) Watcher: quando o LE aparecer, troca e recarrega nginx
(
  while :; do
    if [ -f "$LE_FULL" ] && [ -f "$LE_KEY" ]; then
      # se ainda não está linkado pro LE, troca
      if [ "$(readlink -f "$CERTS_DIR/fullchain.pem" 2>/dev/null || true)" != "$LE_FULL" ]; then
        echo "[nginx] Certificado Let's Encrypt chegou. Trocando symlinks e recarregando Nginx..."
        link_le

        # só tenta reload quando nginx já estiver rodando
        if [ -f /var/run/nginx.pid ]; then
          nginx -s reload || true
        fi
      fi
    fi
    sleep 15
  done
) &

exit 0


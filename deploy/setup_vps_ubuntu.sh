#!/usr/bin/env bash
# ==============================================================================
# Conquer Online 2.0 - Script de Provisionamento 24/7 (Ubuntu 22.04 / 24.04 LTS)
# Instala Wine32 + Xvfb + .NET 4.0 + MariaDB + Firewall UFW + systemd service
# Uso: sudo bash setup_vps_ubuntu.sh
# ==============================================================================
set -euo pipefail

if [[ "${EUID}" -ne 0 ]]; then
    echo "ERRO: Execute como root (ex.: sudo bash setup_vps_ubuntu.sh)"
    exit 1
fi

DEPLOY_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP_USER="conquer"
APP_DIR="/opt/conquer"
SERVER_DIR="${APP_DIR}/server"
WINE_PREFIX="${APP_DIR}/.wine"
LOG_DIR="/var/log/conquer"

# Configuracoes editaveis via variaveis de ambiente
EXTERNAL_IP="${EXTERNAL_IP:-$(curl -fsS https://api.ipify.org 2>/dev/null || echo '127.0.0.1')}"
AUTH_PORT="${AUTH_PORT:-9958}"
GAME_PORT="${GAME_PORT:-5816}"
SERVER_NAME="${SERVER_NAME:-Oblivion}"
DB_NAME="${DB_NAME:-conquer}"
DB_USER="${DB_USER:-conquer}"
DB_PASS="${DB_PASS:-$(openssl rand -hex 12)}"

echo "============================================================"
echo " Provisionando Conquer Online 2.0 na VPS Ubuntu"
echo "   IP Externo : ${EXTERNAL_IP}"
echo "   Portas     : Auth=${AUTH_PORT} / Game=${GAME_PORT}"
echo "   ServerName : ${SERVER_NAME}"
echo "   MySQL DB   : ${DB_NAME} (user: ${DB_USER})"
echo "============================================================"

echo "[1/7] Habilitando arquitetura i386 e instalando pacotes..."
dpkg --add-architecture i386
apt-get update -y
DEBIAN_FRONTEND=noninteractive apt-get install -y \
    wine wine32 wine64 winetricks xvfb cabextract \
    mariadb-server mariadb-client \
    ufw curl netcat-openbsd g++-mingw-w64-i686

echo "[2/7] Criando usuario dedicado '${APP_USER}' e diretorios..."
if ! id -u "${APP_USER}" >/dev/null 2>&1; then
    useradd -r -m -d "${APP_DIR}" -s /bin/bash "${APP_USER}"
fi
mkdir -p "${SERVER_DIR}" "${WINE_PREFIX}" "${LOG_DIR}"
chown -R "${APP_USER}:${APP_USER}" "${APP_DIR}" "${LOG_DIR}"

echo "[3/7] Configurando MariaDB e importando banco de dados..."
systemctl enable --now mariadb
mysql -u root <<SQL
CREATE DATABASE IF NOT EXISTS \`${DB_NAME}\` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS '${DB_USER}'@'127.0.0.1' IDENTIFIED BY '${DB_PASS}';
CREATE USER IF NOT EXISTS '${DB_USER}'@'localhost' IDENTIFIED BY '${DB_PASS}';
ALTER USER '${DB_USER}'@'127.0.0.1' IDENTIFIED BY '${DB_PASS}';
ALTER USER '${DB_USER}'@'localhost' IDENTIFIED BY '${DB_PASS}';
GRANT ALL PRIVILEGES ON \`${DB_NAME}\`.* TO '${DB_USER}'@'127.0.0.1';
GRANT ALL PRIVILEGES ON \`${DB_NAME}\`.* TO '${DB_USER}'@'localhost';
FLUSH PRIVILEGES;
SQL

if [[ -f "${DEPLOY_ROOT}/sql/dbb.sql" ]]; then
    echo "    Importando sql/dbb.sql (pode levar alguns segundos)..."
    mysql -u root "${DB_NAME}" < "${DEPLOY_ROOT}/sql/dbb.sql"
    mysql -u root "${DB_NAME}" -e "UPDATE configuration SET Server='${SERVER_NAME}';" || true
else
    echo "    AVISO: sql/dbb.sql nao encontrado. Importe seu dump SQL manualmente depois."
fi

echo "[4/7] Copiando binarios Release do servidor..."
if [[ -d "${DEPLOY_ROOT}/../dist/server" ]]; then
    cp -a "${DEPLOY_ROOT}/../dist/server/." "${SERVER_DIR}/"
elif [[ -d "${DEPLOY_ROOT}/server" ]]; then
    cp -a "${DEPLOY_ROOT}/server/." "${SERVER_DIR}/"
else
    echo "    AVISO: Pasta dist/server nao encontrada ao lado do script. Copie os binarios para ${SERVER_DIR}/ manualmente."
fi

cat > "${SERVER_DIR}/configuration.ini" <<EOF
[Configuration]
IP=${EXTERNAL_IP}
AuthPort=${AUTH_PORT}
GamePort=${GAME_PORT}
ServerName=${SERVER_NAME}
ConnectionCooldownMs=0

[MySql]
Host=127.0.0.1
Port=3306
Username=${DB_USER}
Password=${DB_PASS}
Database=${DB_NAME}
EOF

cp -f "${DEPLOY_ROOT}/start_server.sh" "${APP_DIR}/start_server.sh"
chmod +x "${APP_DIR}/start_server.sh"
chown -R "${APP_USER}:${APP_USER}" "${APP_DIR}"

echo "[5/7] Inicializando Wine32 prefix e instalando .NET Framework 4.0 (headless via xvfb-run)..."
su - "${APP_USER}" -c "
    export WINEARCH=win32
    export WINEPREFIX='${WINE_PREFIX}'
    export WINEDEBUG=-all
    xvfb-run -a wineboot --init
    xvfb-run -a winetricks -q --force dotnet40 || xvfb-run -a winetricks -q --force dotnet48
    xvfb-run -a winetricks -q vcrun2010 || true
    wineserver -w
"

echo "[6/7] Instalando servico systemd (conquer-server.service)..."
cp -f "${DEPLOY_ROOT}/systemd/conquer-server.service" /etc/systemd/system/conquer-server.service
systemctl daemon-reload
systemctl enable conquer-server.service

echo "[7/7] Configurando Firewall UFW (SSH, Auth ${AUTH_PORT}, Game ${GAME_PORT})..."
ufw allow 22/tcp comment 'SSH' || true
ufw allow "${AUTH_PORT}/tcp" comment 'Conquer AuthServer' || true
ufw allow "${GAME_PORT}/tcp" comment 'Conquer GameServer' || true
ufw --force enable || true

echo ""
echo "============================================================"
echo " INSTALACAO CONCLUIDA!"
echo "   Banco MySQL : ${DB_NAME}"
echo "   Usuario DB  : ${DB_USER}"
echo "   Senha DB    : ${DB_PASS}"
echo "   Config      : ${SERVER_DIR}/configuration.ini"
echo ""
echo " Comandos uteis:"
echo "   sudo systemctl start conquer-server"
echo "   sudo systemctl status conquer-server"
echo "   sudo journalctl -u conquer-server -f"
echo "============================================================"

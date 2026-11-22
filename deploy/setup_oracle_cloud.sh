#!/usr/bin/env bash
# ==============================================================================
# Oblivion Conquer 2.0 - Setup Automatico para Oracle Cloud Always Free (OCI)
# Suporta:
#   - x86_64 (VM.Standard.E2.1.Micro): Swap 4GB + iptables OCI + Wine32 + MariaDB + API Cadastro/Orquestrador
#   - aarch64 (VM.Standard.A1.Flex 24GB): iptables OCI + Docker + binfmt amd64 + Streaming KasmVNC + Caddy HTTPS
# Uso: sudo EXTERNAL_IP="SEU_IP_OCI" bash setup_oracle_cloud.sh
# ==============================================================================
set -euo pipefail

if [[ "${EUID}" -ne 0 ]]; then
    echo "ERRO: Execute como root (sudo bash setup_oracle_cloud.sh)"
    exit 1
fi

ARCH="$(uname -m)"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
EXTERNAL_IP="${EXTERNAL_IP:-$(curl -fsS https://api.ipify.org 2>/dev/null || echo '127.0.0.1')}"
AUTH_PORT="${AUTH_PORT:-9958}"
GAME_PORT="${GAME_PORT:-5816}"
SERVER_NAME="${SERVER_NAME:-Oblivion}"

echo "=================================================================="
echo " Oracle Cloud Always Free Setup - Oblivion Conquer 2.0"
echo "   Arquitetura : ${ARCH}"
echo "   IP Publico  : ${EXTERNAL_IP}"
echo "=================================================================="

# ------------------------------------------------------------------------------
# 1. Swap de 4 GB (crucial em VMs E2.1.Micro de 1 GB RAM)
# ------------------------------------------------------------------------------
if ! swapon --show | grep -q '/swapfile'; then
    echo "[1/5] Criando arquivo Swap de 4 GB (/swapfile)..."
    fallocate -l 4G /swapfile || dd if=/dev/zero of=/swapfile bs=1M count=4096 status=none
    chmod 600 /swapfile
    mkswap /swapfile
    swapon /swapfile
    grep -q '/swapfile' /etc/fstab || echo '/swapfile none swap sw 0 0' >> /etc/fstab
    sysctl -w vm.swappiness=20 >/dev/null
    echo 'vm.swappiness=20' > /etc/sysctl.d/99-conquer-swap.conf
else
    echo "[1/5] Swap ja ativo."
fi

# ------------------------------------------------------------------------------
# 2. Liberar Firewall interno da Oracle Cloud (iptables + netfilter-persistent)
#    Imagens Ubuntu da OCI trazem regra REJECT que bloqueia portas mesmo com UFW!
# ------------------------------------------------------------------------------
echo "[2/5] Liberando portas 22, 80, 443, ${AUTH_PORT}, ${GAME_PORT} no iptables da OCI..."
for port in 22 80 443 "${AUTH_PORT}" "${GAME_PORT}"; do
    iptables -C INPUT -p tcp --dport "${port}" -j ACCEPT 2>/dev/null || \
        iptables -I INPUT 6 -m state --state NEW -p tcp --dport "${port}" -j ACCEPT
done

# Remove eventual regra REJECT bloqueadora se existir antes das nossas
DEBIAN_FRONTEND=noninteractive apt-get update -y
DEBIAN_FRONTEND=noninteractive apt-get install -y iptables-persistent curl ca-certificates gnupg git
netfilter-persistent save || true

# ------------------------------------------------------------------------------
# 3. Instalar Docker Engine + Compose Plugin
# ------------------------------------------------------------------------------
if ! command -v docker >/dev/null 2>&1; then
    echo "[3/5] Instalando Docker Engine + Docker Compose..."
    curl -fsSL https://get.docker.com | sh
    systemctl enable --now docker
else
    echo "[3/5] Docker ja instalado."
fi

# ------------------------------------------------------------------------------
# 4. Provisionamento especifico por Arquitetura
# ------------------------------------------------------------------------------
if [[ "${ARCH}" == "x86_64" ]]; then
    echo "[4/5] Arquitetura x86_64 detectada (VM.Standard.E2.1.Micro)."
    echo "      Executando setup_vps_ubuntu.sh (Wine32 + MariaDB + conquer-server.service)..."
    EXTERNAL_IP="${EXTERNAL_IP}" AUTH_PORT="${AUTH_PORT}" GAME_PORT="${GAME_PORT}" SERVER_NAME="${SERVER_NAME}" \
        bash "${SCRIPT_DIR}/setup_vps_ubuntu.sh"

    # Permite que o container Docker do Orquestrador acesse MariaDB via host-gateway
    sed -i 's/^bind-address\s*=.*/bind-address = 0.0.0.0/' /etc/mysql/mariadb.conf.d/50-server.cnf || true
    systemctl restart mariadb
else
    echo "[4/5] Arquitetura ${ARCH} detectada (VM.Standard.A1.Flex Ampere ARM64)."
    echo "      Habilitando emulacao binfmt amd64 (QEMU) para containers Docker x86..."
    docker run --privileged --rm tonistiigi/binfmt --install amd64 || true
fi

# ------------------------------------------------------------------------------
# 5. Preparar pasta /opt/conquer-production e arquivo .env do Orquestrador
# ------------------------------------------------------------------------------
echo "[5/5] Preparando /opt/conquer/client e configuracao do Orquestrador/Streaming..."
mkdir -p /opt/conquer/client

if [[ ! -f "${REPO_ROOT}/web-client/.env" ]]; then
    cp "${REPO_ROOT}/web-client/.env.example" "${REPO_ROOT}/web-client/.env"
    RAND_JWT="$(openssl rand -hex 32)"
    sed -i "s|MUDE_PARA_UMA_CHAVE_ALEATORIA_DE_64_CARACTERES|${RAND_JWT}|g" "${REPO_ROOT}/web-client/.env"
fi

echo ""
echo "=================================================================="
echo " PROVISIONAMENTO ORACLE CLOUD CONCLUIDO (${ARCH})!"
echo "=================================================================="
echo " LEMBRETE IMPORTANTE NO PAINEL DA ORACLE CLOUD:"
echo "   Va em Networking -> Virtual Cloud Networks -> sua VCN -> Subnet ->"
echo "   Default Security List -> Add Ingress Rules para:"
echo "     Source CIDR: 0.0.0.0/0 | TCP Ports: 80, 443, ${AUTH_PORT}, ${GAME_PORT}"
echo ""
echo " Proximos comandos:"
if [[ "${ARCH}" == "x86_64" ]]; then
echo "   1. Copie dist/server/* para /opt/conquer/server/ (se ainda nao copiou)"
echo "   2. sudo systemctl start conquer-server && sudo journalctl -u conquer-server -f"
echo "   3. Para ativar o cadastro do site (/api/auth/register) nesta mesma VM:"
echo "      cd ${REPO_ROOT}/web-client && nano .env && docker compose up -d --build orchestrator caddy"
else
echo "   1. Copie o cliente limpo (FixClient.ps1) para /opt/conquer/client/"
echo "   2. Edite ${REPO_ROOT}/web-client/.env com MYSQL_HOST/MYSQL_PASSWORD e GAME_SERVER_HOST=game.oblivionco.com.br"
echo "   3. Construa a imagem x86 de streaming e suba o orquestrador:"
echo "      cd ${REPO_ROOT}/web-client"
echo "      DOCKER_DEFAULT_PLATFORM=linux/amd64 docker compose --profile build-only build conquer-web-image"
echo "      docker compose up -d --build orchestrator caddy"
fi
echo "=================================================================="

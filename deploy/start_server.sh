#!/usr/bin/env bash
# ==============================================================================
# Conquer Online 2.0 - Launcher Linux Headless (xvfb-run + Wine32)
# Executado pelo systemd (conquer-server.service) com shutdown gracioso
# ==============================================================================
set -euo pipefail

APP_DIR="${APP_DIR:-/opt/conquer}"
SERVER_DIR="${SERVER_DIR:-${APP_DIR}/server}"
SERVER_EXE="${SERVER_DIR}/Conquer_Online_Server.exe"
LOG_DIR="${LOG_DIR:-/var/log/conquer}"

export WINEARCH=win32
export WINEPREFIX="${WINEPREFIX:-${APP_DIR}/.wine}"
export WINEDEBUG="${WINEDEBUG:--all}"
export LANG=en_US.UTF-8
export LC_ALL=en_US.UTF-8

if [[ ! -f "${SERVER_EXE}" ]]; then
    echo "[ERROR] Binario nao encontrado: ${SERVER_EXE}" >&2
    exit 1
fi

if [[ ! -f "${SERVER_DIR}/configuration.ini" ]]; then
    echo "[ERROR] Arquivo configuration.ini nao encontrado em ${SERVER_DIR}" >&2
    exit 1
fi

# Aguarda MySQL/MariaDB local responder na porta 3306 (ate 60s)
echo "[INFO] Verificando disponibilidade do MySQL (127.0.0.1:3306)..."
for i in $(seq 1 60); do
    if nc -z 127.0.0.1 3306 >/dev/null 2>&1; then
        echo "[INFO] MySQL pronto."
        break
    fi
    if [[ "${i}" -eq 60 ]]; then
        echo "[ERROR] Timeout aguardando MySQL na porta 3306." >&2
        exit 1
    fi
    sleep 1
done

mkdir -p "${LOG_DIR}"
cd "${SERVER_DIR}"

CHILD_PID=""

cleanup() {
    echo "[INFO] Sinal de encerramento recebido. Finalizando Conquer_Online_Server.exe..."
    if [[ -n "${CHILD_PID}" ]] && kill -0 "${CHILD_PID}" 2>/dev/null; then
        kill -TERM "${CHILD_PID}" 2>/dev/null || true
        # Aguarda ate 15s para o processo salvar jogadores e encerrar
        for _ in $(seq 1 15); do
            if ! kill -0 "${CHILD_PID}" 2>/dev/null; then
                break
            fi
            sleep 1
        done
    fi
    wineserver -k 2>/dev/null || true
    exit 0
}

trap cleanup SIGTERM SIGINT

echo "[INFO] Iniciando Conquer_Online_Server.exe via xvfb-run + wine..."
xvfb-run -a --server-args="-screen 0 1024x768x16" \
    wine "${SERVER_EXE}" < /dev/null &
CHILD_PID=$!

wait "${CHILD_PID}"
EXIT_CODE=$?
echo "[WARN] Conquer_Online_Server.exe encerrou com codigo ${EXIT_CODE}."
exit "${EXIT_CODE}"

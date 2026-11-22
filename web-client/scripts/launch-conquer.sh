#!/usr/bin/env bash
# Prepara pasta efemera do cliente (symlinks read-only + conquer_hook.ini gravavel) e inicia Conquer.exe
set -euo pipefail

CLIENT_RO="${CONQUER_CLIENT_DIR:-/opt/conquer-client}"
CLIENT_RUN="/tmp/conquer-run"
HOOK_SRC="/opt/conquer-hooks"

mkdir -p "${CLIENT_RUN}"

# Cria symlinks de todos os assets pesados do cliente read-only para economizar RAM/disco por sessao
if [[ -d "${CLIENT_RO}" ]]; then
    for item in "${CLIENT_RO}"/*; do
        base="$(basename "${item}")"
        case "${base}" in
            play.exe|CSV3Hook.dll|CSV3Hook.dll.lnk|conquer_hook.ini|csv3config.ini|PlayLauncher.exe|ConquerHook.dll)
                continue
                ;;
        esac
        ln -sfn "${item}" "${CLIENT_RUN}/${base}"
    done
fi

cp -f "${HOOK_SRC}/ConquerHook.dll" "${CLIENT_RUN}/ConquerHook.dll"
cp -f "${HOOK_SRC}/PlayLauncher.exe" "${CLIENT_RUN}/PlayLauncher.exe"

# Gera conquer_hook.ini e csv3config.ini apontando para o servidor Conquer
cat > "${CLIENT_RUN}/conquer_hook.ini" <<EOF
[config]
host=${GAME_SERVER_HOST:-game.oblivionco.com.br}
auth_port=${GAME_AUTH_PORT:-9958}
game_port=${GAME_PORT:-5816}
redirect_game_port=0
redirect_all_remote=1
multiclient=1
bypass_antidebug=1
block_autopatch=1
log=0
EOF

cat > "${CLIENT_RUN}/csv3config.ini" <<EOF
[config]
host=${GAME_SERVER_HOST:-game.oblivionco.com.br}
port=${GAME_AUTH_PORT:-9958}
EOF

# Configura resolucao do cliente (1024x768) caso exista ini/GameSetUp.ini
mkdir -p "${CLIENT_RUN}/ini"
if [[ -L "${CLIENT_RUN}/ini" ]]; then
    # Substitui symlink do diretorio ini por diretorio real com symlinks internos
    rm -f "${CLIENT_RUN}/ini"
    mkdir -p "${CLIENT_RUN}/ini"
    for f in "${CLIENT_RO}/ini"/*; do
        ln -sfn "${f}" "${CLIENT_RUN}/ini/$(basename "${f}")"
    done
fi

cat > "${CLIENT_RUN}/ini/GameSetUp.ini" <<EOF
[ScreenMode]
ScreenModeRecord=2
FullScrType=0
EOF

# Inicializa Wine32 prefix leve
export WINEARCH=win32
export WINEPREFIX="${WINEPREFIX:-/tmp/wine-conquer}"
export WINEDEBUG=-all

if [[ ! -d "${WINEPREFIX}/drive_c" ]]; then
    wineboot --init || true
    wineserver -w || true
fi

cd "${CLIENT_RUN}"

if [[ ! -f "${CLIENT_RUN}/Conquer.exe" ]]; then
    echo "[ERROR] Conquer.exe nao encontrado em ${CLIENT_RO}. Monte o volume do cliente em /opt/conquer-client!" >&2
    sleep infinity
fi

# Loop supervisionado: se o jogador fechar o Conquer, reabre automaticamente na sessao
while true; do
    echo "[INFO] Iniciando Conquer.exe via PlayLauncher.exe (host=${GAME_SERVER_HOST}:${GAME_AUTH_PORT})..."
    wine "${CLIENT_RUN}/PlayLauncher.exe" || true
    # Aguarda processo Conquer.exe finalizar antes de relancar
    while pgrep -f "Conquer.exe" >/dev/null 2>&1; do
        sleep 2
    done
    sleep 2
done

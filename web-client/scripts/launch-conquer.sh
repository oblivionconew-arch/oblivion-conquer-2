#!/usr/bin/env bash
set -euo pipefail

CLIENT_RO="${CONQUER_CLIENT_DIR:-/opt/conquer-client}"
CLIENT_RUN="/tmp/conquer-run"
HOOK_SRC="/opt/conquer-hooks"

mkdir -p "${CLIENT_RUN}"

if [[ -d "${CLIENT_RO}" ]]; then
    for item in "${CLIENT_RO}"/*; do
        base="$(basename "${item}")"
        case "${base}" in
            play.exe|CSV3Hook.dll|CSV3Hook.dll.lnk|Ntyr.dll|SeW.dll|OblivionCo.exe|conquer_hook.ini|csv3config.ini|PlayLauncher.exe|ConquerHook.dll|dinput8.dll|ini)
                continue
                ;;
        esac
        ln -sfn "${item}" "${CLIENT_RUN}/${base}"
    done
fi

# Copia proxy DLL nativa dinput8.dll e ConquerHook.dll
cp -f "${CLIENT_RO}/dinput8.dll" "${CLIENT_RUN}/dinput8.dll" 2>/dev/null || cp -f "${HOOK_SRC}/dinput8.dll" "${CLIENT_RUN}/dinput8.dll" 2>/dev/null || true
cp -f "${CLIENT_RO}/ConquerHook.dll" "${CLIENT_RUN}/ConquerHook.dll" 2>/dev/null || cp -f "${HOOK_SRC}/ConquerHook.dll" "${CLIENT_RUN}/ConquerHook.dll" 2>/dev/null || true

# Prepara diretorio ini gravavel com links para os arquivos originais
rm -rf "${CLIENT_RUN}/ini"
mkdir -p "${CLIENT_RUN}/ini"
if [[ -d "${CLIENT_RO}/ini" ]]; then
    for f in "${CLIENT_RO}/ini"/*; do
        ln -sfn "${f}" "${CLIENT_RUN}/ini/$(basename "${f}")"
    done
fi

# Escreve configuracoes de tela windowed borderless (1024x768) compativeis com llvmpipe/Xvfb
cat > "${CLIENT_RUN}/ini/GameSetup.ini" << 'EOINI'
[ScreenMode]
ScrWidth=1024
ScrHeight=768
FullScrType=1
ScreenModeRecord=2
EOINI
cp -f "${CLIENT_RUN}/ini/GameSetup.ini" "${CLIENT_RUN}/ini/GameSetUp.ini"

cat > "${CLIENT_RUN}/conquer_hook.ini" << EOINI
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
EOINI

cat > "${CLIENT_RUN}/csv3config.ini" << EOINI
[config]
host=${GAME_SERVER_HOST:-game.oblivionco.com.br}
port=${GAME_AUTH_PORT:-9958}
EOINI

export WINEARCH=win32
export WINEPREFIX="${WINEPREFIX:-/home/conquer/.wine}"
export WINEDEBUG=-all
export LIBGL_ALWAYS_SOFTWARE=1
export GALLIUM_DRIVER=llvmpipe

cd "${CLIENT_RUN}"

if [[ ! -f "${CLIENT_RUN}/Conquer.exe" ]]; then
    echo "[ERROR] Conquer.exe nao encontrado!" >&2
    sleep infinity
fi

while true; do
    echo "[INFO] Iniciando Conquer.exe blacknull (host=${GAME_SERVER_HOST:-game.oblivionco.com.br}:${GAME_AUTH_PORT:-9958})..."
    wine "${CLIENT_RUN}/Conquer.exe" blacknull || true
    while pgrep -f "Conquer.exe" >/dev/null 2>&1; do
        sleep 2
    done
    sleep 2
done

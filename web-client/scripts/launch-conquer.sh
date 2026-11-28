#!/bin/bash
set -euo pipefail

CLIENT_RO="${CONQUER_CLIENT_DIR:-/opt/conquer-client}"
CLIENT_RUN="/tmp/conquer-run"
HOOK_SRC="/opt/conquer-hooks"

mkdir -p "${CLIENT_RUN}"

if [[ -d "${CLIENT_RO}" ]]; then
    for item in "${CLIENT_RO}"/*; do
        base="$(basename "${item}")"
        case "${base}" in
            ini|conquer_hook.ini|csv3config.ini)
                continue
                ;;
        esac
        ln -sfn "${item}" "${CLIENT_RUN}/${base}"
    done
fi

# Copia proxy DLL nativa e Flash.ocx se existirem
cp -f "${CLIENT_RO}/dinput8.dll" "${CLIENT_RUN}/dinput8.dll" 2>/dev/null || cp -f "${HOOK_SRC}/dinput8.dll" "${CLIENT_RUN}/dinput8.dll" 2>/dev/null || true
cp -f "${CLIENT_RO}/ConquerHook.dll" "${CLIENT_RUN}/ConquerHook.dll" 2>/dev/null || cp -f "${HOOK_SRC}/ConquerHook.dll" "${CLIENT_RUN}/ConquerHook.dll" 2>/dev/null || true
cp -f "${CLIENT_RO}/Flash.ocx" "${CLIENT_RUN}/Flash.ocx" 2>/dev/null || true

# Copia diretorio ini de forma gravavel (nao symlink!)
rm -rf "${CLIENT_RUN}/ini"
mkdir -p "${CLIENT_RUN}/ini"
if [[ -d "${CLIENT_RO}/ini" ]]; then
    cp -r "${CLIENT_RO}/ini"/* "${CLIENT_RUN}/ini/" 2>/dev/null || true
fi

# Desativa flashLogin.ini para garantir exibicao nativa da arte characterInAndOut2.jpg (Oblivion-CO)
rm -f "${CLIENT_RUN}/ini/flashLogin.ini" 2>/dev/null || true

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

export LC_ALL=C.UTF-8
export LANG=C.UTF-8
export WINEARCH=win32
export WINEPREFIX="${WINEPREFIX:-/home/conquer/.wine}"
export WINEDEBUG=-all
export LIBGL_ALWAYS_SOFTWARE=1
export GALLIUM_DRIVER=llvmpipe

# Registra Flash.ocx no Wine silenciosamente
if [[ -f "${CLIENT_RUN}/Flash.ocx" ]]; then
    wine regsvr32 /s "${CLIENT_RUN}/Flash.ocx" 2>/dev/null || true
fi

# Rotina em background para fechar automaticamente qualquer popup de aviso
(
    while true; do
        sleep 1
        for win in $(xdotool search --name "Tip" 2>/dev/null || true); do
            xdotool windowactivate --sync "$win" key Return 2>/dev/null || true
        done
    done
) &

cd "${CLIENT_RUN}"

# Identifica o executavel correto: Oblivion.exe (launcher do 7032) ou Env_DX8/Conquer.exe
if [[ -f "${CLIENT_RUN}/Oblivion.exe" ]]; then
    LAUNCH_CMD="wine Oblivion.exe blacknull"
elif [[ -f "${CLIENT_RUN}/Env_DX8/Conquer.exe" ]]; then
    LAUNCH_CMD="wine Env_DX8/Conquer.exe blacknull"
elif [[ -f "${CLIENT_RUN}/Conquer.exe" ]] && [[ $(wc -c < "${CLIENT_RUN}/Conquer.exe") -gt 1000 ]]; then
    LAUNCH_CMD="wine Conquer.exe blacknull"
elif [[ -f "${CLIENT_RUN}/play.exe" ]]; then
    LAUNCH_CMD="wine play.exe blacknull"
else
    echo "[ERROR] Executavel do Conquer nao encontrado em ${CLIENT_RUN}!" >&2
    sleep infinity
fi

while true; do
    echo "[INFO] Iniciando ${LAUNCH_CMD}..."
    eval "${LAUNCH_CMD}" || true
    while ps -eo pid,stat,args | grep -iE "(Conquer.exe|Oblivion.exe)" | grep -v " Z" >/dev/null 2>&1; do
        sleep 2
    done
    sleep 2
done

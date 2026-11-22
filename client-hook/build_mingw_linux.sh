#!/usr/bin/env bash
# Cross-compile ConquerHook.dll + PlayLauncher.exe (Win32 x86, static CRT) on Ubuntu Linux
# Install toolchain once: sudo apt update && sudo apt install -y g++-mingw-w64-i686

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUT_DIR="${SCRIPT_DIR}/../dist/client"
mkdir -p "${OUT_DIR}"

CXX="i686-w64-mingw32-g++"
if ! command -v "${CXX}" >/dev/null 2>&1; then
    echo "ERRO: ${CXX} nao encontrado. Instale com:"
    echo "  sudo apt update && sudo apt install -y g++-mingw-w64-i686"
    exit 1
fi

echo "[1/2] Compilando ConquerHook.dll (Win32 x86, static)..."
"${CXX}" -m32 -shared -std=c++17 -O2 -DNDEBUG \
    -D_WIN32_WINNT=0x0601 \
    -static -static-libgcc -static-libstdc++ -s \
    -Wl,--kill-at \
    "${SCRIPT_DIR}/src/dllmain.cpp" \
    "${SCRIPT_DIR}/src/config.cpp" \
    "${SCRIPT_DIR}/src/iat_hook.cpp" \
    -lws2_32 -lshell32 -luser32 \
    -o "${OUT_DIR}/ConquerHook.dll"

echo "[2/2] Compilando PlayLauncher.exe (Win32 x86, static, GUI)..."
"${CXX}" -m32 -mwindows -std=c++17 -O2 -DNDEBUG \
    -D_WIN32_WINNT=0x0601 \
    -static -static-libgcc -static-libstdc++ -s \
    "${SCRIPT_DIR}/src/launcher_main.cpp" \
    -luser32 -lkernel32 \
    -o "${OUT_DIR}/PlayLauncher.exe"

cp -f "${SCRIPT_DIR}/conquer_hook.ini" "${OUT_DIR}/conquer_hook.ini"
echo "Concluido! Arquivos gerados em: ${OUT_DIR}"
ls -lh "${OUT_DIR}/ConquerHook.dll" "${OUT_DIR}/PlayLauncher.exe" "${OUT_DIR}/conquer_hook.ini"

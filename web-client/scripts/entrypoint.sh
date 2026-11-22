#!/usr/bin/env bash
set -euo pipefail

# Inicia PulseAudio para audio do jogo no navegador
pulseaudio --start --exit-idle-time=-1 --disable-shm=yes 2>/dev/null || true

# Configura senha efemera do KasmVNC (protegida pelo proxy JWT do Orquestrador)
VNC_PASS="${VNC_PASSWORD:-conquerstream}"
echo -e "${VNC_PASS}\n${VNC_PASS}\n" | kasmvncpasswd -u conquer -w -o >/dev/null 2>&1 || true

# xstartup minimalista: Openbox sem borda + Conquer.exe 1024x768
cat > /home/conquer/.vnc/xstartup <<'EOF'
#!/usr/bin/env bash
export DISPLAY=:1
openbox-session &
sleep 1
exec /usr/local/bin/launch-conquer.sh
EOF
chmod +x /home/conquer/.vnc/xstartup

# Inicia KasmVNC em 1024x768 na porta 6901
vncserver :1 \
    -geometry "${RESOLUTION:-1024x768}" \
    -depth 24 \
    -websocketPort "${VNC_PORT:-6901}" \
    -DisableBasicAuth \
    -select-de manual

echo "[INFO] KasmVNC online em :${VNC_PORT:-6901} (${RESOLUTION:-1024x768})"
tail -F /home/conquer/.vnc/*.log 2>/dev/null || sleep infinity

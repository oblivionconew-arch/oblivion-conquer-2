#!/usr/bin/env bash
# Entrypoint para perfil WebRTC (Selkies-GStreamer + Xvfb + PulseAudio + Wine32)
set -euo pipefail

export DISPLAY=:0
Xvfb :0 -screen 0 1024x768x24 +extension RANDR +extension GLX &
sleep 1

pulseaudio --start --exit-idle-time=-1 --disable-shm=yes 2>/dev/null || true
openbox-session &
sleep 1

/usr/local/bin/launch-conquer.sh &

exec selkies-gstreamer \
    --addr="0.0.0.0" \
    --port="${SELKIES_PORT:-8080}" \
    --web_root="/opt/gst-web" \
    --encoder="${SELKIES_ENCODER:-x264enc}" \
    --framerate="${SELKIES_FRAMERATE:-30}" \
    --enable_audio="${SELKIES_ENABLE_AUDIO:-true}" \
    --enable_resize="false"

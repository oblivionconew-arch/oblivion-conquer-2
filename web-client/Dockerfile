# ==============================================================================
# Stage 1: Cross-compile ConquerHook.dll + PlayLauncher.exe (Win32 x86 static)
# ==============================================================================
FROM ubuntu:22.04 AS hook-builder

ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update && apt-get install -y --no-install-recommends \
    g++-mingw-w64-i686 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY client-hook/ ./client-hook/

RUN mkdir -p /out && \
    i686-w64-mingw32-g++ -m32 -shared -std=c++17 -O2 -DNDEBUG \
      -D_WIN32_WINNT=0x0601 \
      -static -static-libgcc -static-libstdc++ -s -Wl,--kill-at \
      client-hook/src/dllmain.cpp \
      client-hook/src/config.cpp \
      client-hook/src/iat_hook.cpp \
      -lws2_32 -lshell32 -luser32 \
      -o /out/ConquerHook.dll && \
    i686-w64-mingw32-g++ -m32 -mwindows -std=c++17 -O2 -DNDEBUG \
      -D_WIN32_WINNT=0x0601 \
      -static -static-libgcc -static-libstdc++ -s \
      client-hook/src/launcher_main.cpp \
      -luser32 -lkernel32 \
      -o /out/PlayLauncher.exe

# ==============================================================================
# Stage 2: Runtime Streaming Container (Ubuntu 22.04 + Wine32 + KasmVNC + Audio)
# Optimized for 1024x768, low-latency H.264/WebP over WSS (Cloudflare-friendly)
# ==============================================================================
FROM ubuntu:22.04

ENV DEBIAN_FRONTEND=noninteractive \
    DISPLAY=:1 \
    VNC_PORT=6901 \
    RESOLUTION=1024x768 \
    WINEARCH=win32 \
    WINEPREFIX=/tmp/wine-conquer \
    WINEDEBUG=-all \
    GAME_SERVER_HOST=game.oblivionco.com.br \
    GAME_AUTH_PORT=9958 \
    GAME_PORT=5816 \
    CONQUER_CLIENT_DIR=/opt/conquer-client

RUN dpkg --add-architecture i386 && \
    apt-get update && \
    apt-get install -y --no-install-recommends \
      ca-certificates curl wget gnupg2 \
      wine wine32 wine64 winetricks cabextract \
      xvfb x11-utils xdotool openbox \
      pulseaudio pulseaudio-utils \
      fonts-liberation fonts-dejavu-core \
      openssl netcat-openbsd python3 \
    && rm -rf /var/lib/apt/lists/*

# Install KasmVNC (Ubuntu Jammy amd64)
ARG KASMVNC_VERSION=1.3.2
RUN wget -q "https://github.com/kasmtech/KasmVNC/releases/download/v${KASMVNC_VERSION}/kasmvncserver_jammy_${KASMVNC_VERSION}_amd64.deb" -O /tmp/kasmvnc.deb && \
    apt-get update && \
    apt-get install -y /tmp/kasmvnc.deb libdatetime-perl && \
    rm -f /tmp/kasmvnc.deb && \
    rm -rf /var/lib/apt/lists/*

# Create non-root streaming user
RUN useradd -m -s /bin/bash -G ssl-cert conquer && \
    mkdir -p /opt/conquer-client /opt/conquer-hooks /home/conquer/.vnc /home/conquer/.config/openbox && \
    chown -R conquer:conquer /opt/conquer-client /opt/conquer-hooks /home/conquer

# Copy freshly built static Win32 ConquerHook.dll & PlayLauncher.exe
COPY --from=hook-builder /out/ConquerHook.dll /opt/conquer-hooks/ConquerHook.dll
COPY --from=hook-builder /out/PlayLauncher.exe /opt/conquer-hooks/PlayLauncher.exe

# Copy streaming configuration & entrypoint
COPY web-client/config/kasmvnc.yaml /home/conquer/.vnc/kasmvnc.yaml
COPY web-client/config/openbox-rc.xml /home/conquer/.config/openbox/rc.xml
COPY web-client/scripts/entrypoint.sh /usr/local/bin/entrypoint.sh
COPY web-client/scripts/launch-conquer.sh /usr/local/bin/launch-conquer.sh

RUN chmod +x /usr/local/bin/entrypoint.sh /usr/local/bin/launch-conquer.sh && \
    chown -R conquer:conquer /home/conquer/.vnc /home/conquer/.config /opt/conquer-hooks

USER conquer
WORKDIR /home/conquer

EXPOSE 6901

HEALTHCHECK --interval=5s --timeout=3s --start-period=20s --retries=6 \
  CMD curl -ksSf https://127.0.0.1:6901/ >/dev/null 2>&1 || curl -sSf http://127.0.0.1:6901/ >/dev/null 2>&1 || exit 1

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]

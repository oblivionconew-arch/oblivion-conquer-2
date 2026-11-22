/* ============================================================================
 * Oblivion Conquer 2.0 — Web Streaming Client + Controles Touch (PC & Mobile)
 * Conecta ao Orquestrador Node.js (stream.oblivionco.com.br) e encaminha
 * eventos de Joystick, Jump (Ctrl+Clique), Ataque, F1-F10 e Chat Android.
 * ============================================================================ */
(() => {
  const ORCHESTRATOR_URL =
    window.CONQUER_STREAM_API ||
    (location.hostname === 'localhost' || location.hostname === '127.0.0.1'
      ? 'http://localhost:3000'
      : 'https://stream.oblivionco.com.br');

  const GAME_W = 1024;
  const GAME_H = 768;
  const CHAR_CENTER_X = 512;
  const CHAR_CENTER_Y = 384;

  const state = {
    authToken: localStorage.getItem('conquer_auth_token') || '',
    sessionId: null,
    heartbeatTimer: null,
    wakeLock: null,
    touchEnabled: false,
    joyVec: { x: 0, y: 0, active: false },
    joyLoop: null,
  };

  // Elementos DOM
  const lobbyOverlay = document.getElementById('lobbyOverlay');
  const loginForm = document.getElementById('loginForm');
  const lobbyStatus = document.getElementById('lobbyStatus');
  const btnConnect = document.getElementById('btnConnect');
  const streamFrame = document.getElementById('streamFrame');
  const viewportBox = document.getElementById('viewportBox');
  const touchOverlay = document.getElementById('touchOverlay');
  const fKeysDrawer = document.getElementById('fKeysDrawer');
  const btnToggleTouch = document.getElementById('btnToggleTouch');
  const btnToggleFKeys = document.getElementById('btnToggleFKeys');
  const btnFullscreen = document.getElementById('btnFullscreen');
  const btnOpenChat = document.getElementById('btnOpenChat');
  const btnExitSession = document.getElementById('btnExitSession');
  const joystickZone = document.getElementById('joystickZone');
  const joystickKnob = document.getElementById('joystickKnob');
  const chatModal = document.getElementById('chatModal');
  const chatInput = document.getElementById('chatInput');
  const chatSend = document.getElementById('chatSend');

  // Detecta celular / tablet / Capacitor automaticamente
  function isTouchDevice() {
    return (
      window.matchMedia('(pointer: coarse)').matches ||
      'ontouchstart' in window ||
      navigator.maxTouchPoints > 0 ||
      Boolean(window.Capacitor)
    );
  }

  function setTouchOverlay(enabled) {
    state.touchEnabled = enabled;
    touchOverlay.classList.toggle('visible', enabled);
    btnToggleTouch.classList.toggle('active', enabled);
  }

  // WakeLock Web + Capacitor KeepAwake
  async function enableKeepAwake() {
    try {
      if (window.Capacitor && window.Capacitor.Plugins && window.Capacitor.Plugins.KeepAwake) {
        await window.Capacitor.Plugins.KeepAwake.keepAwake();
      }
    } catch (_) {}
    try {
      if ('wakeLock' in navigator) {
        state.wakeLock = await navigator.wakeLock.request('screen');
      }
    } catch (_) {}
  }

  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible' && state.sessionId) {
      enableKeepAwake();
    }
  });

  // ============================================================================
  // Ponte de Eventos para o Stream (iframe KasmVNC / Selkies ou canvas local)
  // ============================================================================
  function getStreamTarget() {
    try {
      if (streamFrame && streamFrame.contentWindow && streamFrame.contentDocument) {
        const innerCanvas = streamFrame.contentDocument.querySelector('canvas');
        if (innerCanvas) return { win: streamFrame.contentWindow, el: innerCanvas, sameOrigin: true };
      }
    } catch (_) {}
    return { win: streamFrame ? streamFrame.contentWindow : window, el: viewportBox, sameOrigin: false };
  }

  function sendKey(key, code, keyCode, ctrlKey = false) {
    const target = getStreamTarget();
    if (target.sameOrigin && target.el) {
      const opts = { key, code, keyCode, which: keyCode, ctrlKey, bubbles: true, cancelable: true };
      target.el.dispatchEvent(new KeyboardEvent('keydown', opts));
      setTimeout(() => target.el.dispatchEvent(new KeyboardEvent('keyup', opts)), 45);
    } else if (target.win) {
      // Fallback cross-origin postMessage (suportado por wrapper customizado)
      target.win.postMessage({ type: 'conquer_key', key, code, keyCode, ctrlKey }, '*');
    }
  }

  function sendGameClick(gameX, gameY, button = 0, ctrlKey = false) {
    const target = getStreamTarget();
    const rect = (target.sameOrigin && target.el ? target.el : viewportBox).getBoundingClientRect();
    const clientX = rect.left + (gameX / GAME_W) * rect.width;
    const clientY = rect.top + (gameY / GAME_H) * rect.height;

    if (target.sameOrigin && target.el) {
      if (ctrlKey) {
        target.el.dispatchEvent(new KeyboardEvent('keydown', { key: 'Control', code: 'ControlLeft', keyCode: 17, ctrlKey: true, bubbles: true }));
      }
      const mOpts = { clientX, clientY, button, buttons: button === 2 ? 2 : 1, ctrlKey, bubbles: true, cancelable: true };
      target.el.dispatchEvent(new MouseEvent('mousemove', mOpts));
      target.el.dispatchEvent(new MouseEvent('mousedown', mOpts));
      setTimeout(() => {
        target.el.dispatchEvent(new MouseEvent('mouseup', { ...mOpts, buttons: 0 }));
        if (ctrlKey) {
          target.el.dispatchEvent(new KeyboardEvent('keyup', { key: 'Control', code: 'ControlLeft', keyCode: 17, ctrlKey: false, bubbles: true }));
        }
      }, 45);
    } else if (target.win) {
      target.win.postMessage({ type: 'conquer_click', gameX, gameY, button, ctrlKey }, '*');
    }
  }

  // ============================================================================
  // Joystick Analogico Virtual (canto inferior esquerdo)
  // ============================================================================
  const JOY_MAX_RADIUS = 48;

  function updateJoystick(touchX, touchY) {
    const rect = joystickZone.getBoundingClientRect();
    const cx = rect.left + rect.width / 2;
    const cy = rect.top + rect.height / 2;
    let dx = touchX - cx;
    let dy = touchY - cy;
    const dist = Math.hypot(dx, dy);
    if (dist > JOY_MAX_RADIUS) {
      dx = (dx / dist) * JOY_MAX_RADIUS;
      dy = (dy / dist) * JOY_MAX_RADIUS;
    }
    joystickKnob.style.transform = `translate(${dx}px, ${dy}px)`;
    state.joyVec = {
      x: dx / JOY_MAX_RADIUS,
      y: dy / JOY_MAX_RADIUS,
      active: dist > 8,
    };
  }

  function resetJoystick() {
    joystickKnob.style.transform = 'translate(0px, 0px)';
    state.joyVec = { x: 0, y: 0, active: false };
    if (state.joyLoop) {
      clearInterval(state.joyLoop);
      state.joyLoop = null;
    }
  }

  function startJoystickLoop() {
    if (state.joyLoop) return;
    state.joyLoop = setInterval(() => {
      if (!state.joyVec.active) return;
      const mag = Math.hypot(state.joyVec.x, state.joyVec.y);
      // Raio curto = andar, raio longo = correr no Conquer
      const radiusPx = mag > 0.65 ? 210 : 95;
      const gx = Math.max(40, Math.min(GAME_W - 40, Math.round(CHAR_CENTER_X + state.joyVec.x * radiusPx)));
      const gy = Math.max(40, Math.min(GAME_H - 60, Math.round(CHAR_CENTER_Y + state.joyVec.y * radiusPx)));
      sendGameClick(gx, gy, 0, false);
    }, 180);
  }

  let activeJoyTouchId = null;
  joystickZone.addEventListener('touchstart', (e) => {
    e.preventDefault();
    const t = e.changedTouches[0];
    activeJoyTouchId = t.identifier;
    updateJoystick(t.clientX, t.clientY);
    startJoystickLoop();
  }, { passive: false });

  joystickZone.addEventListener('touchmove', (e) => {
    e.preventDefault();
    for (const t of e.changedTouches) {
      if (t.identifier === activeJoyTouchId) {
        updateJoystick(t.clientX, t.clientY);
      }
    }
  }, { passive: false });

  joystickZone.addEventListener('touchend', (e) => {
    for (const t of e.changedTouches) {
      if (t.identifier === activeJoyTouchId) {
        activeJoyTouchId = null;
        resetJoystick();
      }
    }
  });
  joystickZone.addEventListener('touchcancel', () => { activeJoyTouchId = null; resetJoystick(); });

  // ============================================================================
  // Botoes de Acao Rapida (Pular / Atacar / HP F1 / MP F2) & Barra F1-F10
  // ============================================================================
  document.getElementById('btnJump').addEventListener('click', () => {
    const vx = state.joyVec.active ? state.joyVec.x : 0.7;
    const vy = state.joyVec.active ? state.joyVec.y : -0.35;
    const gx = Math.max(40, Math.min(GAME_W - 40, Math.round(CHAR_CENTER_X + vx * 240)));
    const gy = Math.max(40, Math.min(GAME_H - 60, Math.round(CHAR_CENTER_Y + vy * 200)));
    sendGameClick(gx, gy, 0, true); // Ctrl + Clique Esquerdo = Jump
  });

  document.getElementById('btnAttack').addEventListener('click', () => {
    const vx = state.joyVec.active ? state.joyVec.x : 0;
    const vy = state.joyVec.active ? state.joyVec.y : 0;
    const gx = Math.round(CHAR_CENTER_X + vx * 120);
    const gy = Math.round(CHAR_CENTER_Y + vy * 120);
    sendGameClick(gx, gy, 2, false); // Botao direito = Skill / Ataque
  });

  document.getElementById('btnHp').addEventListener('click', () => sendKey('F1', 'F1', 112));
  document.getElementById('btnMp').addEventListener('click', () => sendKey('F2', 'F2', 113));

  for (let i = 1; i <= 10; i++) {
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'fkey-btn';
    btn.textContent = `F${i}`;
    btn.addEventListener('click', () => sendKey(`F${i}`, `F${i}`, 111 + i));
    fKeysDrawer.appendChild(btn);
  }

  btnToggleTouch.addEventListener('click', () => setTouchOverlay(!state.touchEnabled));
  btnToggleFKeys.addEventListener('click', () => {
    fKeysDrawer.classList.toggle('collapsed');
    btnToggleFKeys.classList.toggle('active', !fKeysDrawer.classList.contains('collapsed'));
  });

  btnFullscreen.addEventListener('click', async () => {
    try {
      if (!document.fullscreenElement) {
        await document.documentElement.requestFullscreen();
        if (screen.orientation && screen.orientation.lock) {
          await screen.orientation.lock('landscape').catch(() => {});
        }
      } else {
        await document.exitFullscreen();
      }
    } catch (_) {}
  });

  // Chat / Teclado Virtual Android
  btnOpenChat.addEventListener('click', () => {
    const open = chatModal.classList.toggle('open');
    if (open) chatInput.focus();
  });

  function sendChatMessage() {
    const txt = chatInput.value.trim();
    if (!txt) {
      chatModal.classList.remove('open');
      return;
    }
    sendKey('Enter', 'Enter', 13);
    setTimeout(() => {
      for (let i = 0; i < txt.length; i++) {
        const ch = txt[i];
        sendKey(ch, `Key${ch.toUpperCase()}`, ch.charCodeAt(0));
      }
      setTimeout(() => sendKey('Enter', 'Enter', 13), 80);
    }, 80);
    chatInput.value = '';
    chatModal.classList.remove('open');
  }

  chatSend.addEventListener('click', sendChatMessage);
  chatInput.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      sendChatMessage();
    }
  });

  // ============================================================================
  // Fluxo de Autenticacao + Sessao Docker + Heartbeat
  // ============================================================================
  function startHeartbeat(intervalMs) {
    if (state.heartbeatTimer) clearInterval(state.heartbeatTimer);
    state.heartbeatTimer = setInterval(async () => {
      if (!state.sessionId || !state.authToken) return;
      try {
        await fetch(`${ORCHESTRATOR_URL}/api/sessions/heartbeat`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${state.authToken}`,
          },
          body: JSON.stringify({ sessionId: state.sessionId }),
        });
      } catch (_) {}
    }, intervalMs || 30000);
  }

  async function endSession() {
    if (state.heartbeatTimer) clearInterval(state.heartbeatTimer);
    if (state.sessionId && state.authToken) {
      try {
        await fetch(`${ORCHESTRATOR_URL}/api/sessions/${encodeURIComponent(state.sessionId)}`, {
          method: 'DELETE',
          headers: { Authorization: `Bearer ${state.authToken}` },
          keepalive: true,
        });
      } catch (_) {}
    }
    state.sessionId = null;
    streamFrame.src = 'about:blank';
    lobbyOverlay.classList.remove('hidden');
    lobbyStatus.textContent = 'Sessao encerrada com seguranca.';
  }

  btnExitSession.addEventListener('click', () => {
    if (confirm('Deseja encerrar sua sessao de jogo no navegador?')) {
      endSession();
    }
  });

  window.addEventListener('beforeunload', () => {
    if (state.sessionId && state.authToken) {
      fetch(`${ORCHESTRATOR_URL}/api/sessions/${encodeURIComponent(state.sessionId)}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${state.authToken}` },
        keepalive: true,
      }).catch(() => {});
    }
  });

  loginForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    const username = document.getElementById('loginUser').value.trim();
    const password = document.getElementById('loginPass').value;

    btnConnect.disabled = true;
    lobbyStatus.style.color = '#f5b942';
    lobbyStatus.textContent = 'Autenticando conta...';

    try {
      const authRes = await fetch(`${ORCHESTRATOR_URL}/api/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
      });
      const authData = await authRes.json();
      if (!authRes.ok || !authData.ok) {
        throw new Error(authData.message || 'Falha no login.');
      }

      state.authToken = authData.token;
      localStorage.setItem('conquer_auth_token', authData.token);

      lobbyStatus.textContent = 'Iniciando sua sessao exclusiva do Conquer (aguarde ~10s)...';
      const sessRes = await fetch(`${ORCHESTRATOR_URL}/api/sessions/start`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${state.authToken}`,
        },
      });
      const sessData = await sessRes.json();
      if (!sessRes.ok || !sessData.ok) {
        throw new Error(sessData.message || 'Nao foi possivel iniciar a sessao.');
      }

      state.sessionId = sessData.sessionId;
      streamFrame.src = sessData.streamUrl;
      lobbyOverlay.classList.add('hidden');
      startHeartbeat(sessData.heartbeatIntervalMs);
      enableKeepAwake();
    } catch (err) {
      lobbyStatus.style.color = '#f87171';
      lobbyStatus.textContent = err.message || 'Erro ao conectar ao servidor de streaming.';
    } finally {
      btnConnect.disabled = false;
    }
  });

  // Inicializacao automatica
  setTouchOverlay(isTouchDevice());
})();

const http = require('http');
const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');
const httpProxy = require('http-proxy');

const {
  authenticateGameAccount,
  issueUserToken,
  issueStreamToken,
  verifyAnyAuthToken,
  verifyStreamToken,
} = require('./auth');
const {
  CONFIG,
  sessions,
  createOrReuseSession,
  destroySession,
  touchSession,
} = require('./dockerManager');
const { startIdleReaper } = require('./reaper');

const PORT = Number(process.env.PORT || 3000);
const PUBLIC_STREAM_BASE_URL = (process.env.PUBLIC_STREAM_BASE_URL || 'https://stream.oblivionco.com.br').replace(/\/$/, '');
const ALLOWED_ORIGINS = (process.env.ALLOWED_ORIGINS || 'https://oblivionco.com.br,https://www.oblivionco.com.br')
  .split(',')
  .map((s) => s.trim())
  .filter(Boolean);

const app = express();
app.set('trust proxy', 1);

app.use(
  helmet({
    contentSecurityPolicy: false,
    crossOriginEmbedderPolicy: false,
    frameguard: false, // Permite embutir o stream no iframe/canvas da play.html em oblivionco.com.br
  })
);

app.use(
  cors({
    origin(origin, cb) {
      if (!origin || ALLOWED_ORIGINS.includes('*') || ALLOWED_ORIGINS.includes(origin)) {
        return cb(null, true);
      }
      return cb(new Error('Origem nao permitida pelo CORS'));
    },
    credentials: true,
  })
);

app.use(express.json({ limit: '64kb' }));

const authLimiter = rateLimit({
  windowMs: 60 * 1000,
  max: 20,
  standardHeaders: true,
  legacyHeaders: false,
});

function requireAuth(req, res, next) {
  const hdr = req.headers.authorization || '';
  const token = hdr.startsWith('Bearer ') ? hdr.slice(7) : null;
  const user = verifyAnyAuthToken(token);
  if (!user) {
    return res.status(401).json({ ok: false, message: 'Nao autenticado. Faca login primeiro.' });
  }
  req.user = user;
  next();
}

// ============================================================================
// API REST
// ============================================================================
app.get('/api/health', (req, res) => {
  res.json({
    ok: true,
    engine: CONFIG.engine,
    activeSessions: sessions.size,
    maxSessions: CONFIG.maxConcurrentSessions,
    idleTimeoutSeconds: Math.round(CONFIG.idleTimeoutMs / 1000),
  });
});

app.get('/api/sessions/status', (req, res) => {
  res.json({
    ok: true,
    active: sessions.size,
    capacity: CONFIG.maxConcurrentSessions,
    available: Math.max(0, CONFIG.maxConcurrentSessions - sessions.size),
    engine: CONFIG.engine,
  });
});

app.post('/api/auth/login', authLimiter, async (req, res) => {
  try {
    const { username, password } = req.body || {};
    const account = await authenticateGameAccount(username, password);
    if (!account) {
      return res.status(401).json({ ok: false, message: 'Usuario ou senha invalidos.' });
    }
    const token = issueUserToken({ sub: account.username, src: account.source });
    res.json({ ok: true, username: account.username, token });
  } catch (err) {
    res.status(403).json({ ok: false, message: err.message || 'Erro de autenticacao.' });
  }
});

app.post('/api/sessions/start', requireAuth, async (req, res) => {
  try {
    const meta = await createOrReuseSession(req.user.username);
    const streamToken = issueStreamToken(meta.sessionId, meta.username);
    const streamPath = `/s/${meta.sessionId}/?token=${encodeURIComponent(streamToken)}`;
    const streamUrl = `${PUBLIC_STREAM_BASE_URL}${streamPath}`;

    res.json({
      ok: true,
      sessionId: meta.sessionId,
      engine: meta.engine,
      streamUrl,
      streamToken,
      resolution: '1024x768',
      heartbeatIntervalMs: 30000,
      idleTimeoutMs: CONFIG.idleTimeoutMs,
    });
  } catch (err) {
    console.error('[StartSession] Erro:', err);
    res.status(err.statusCode || 500).json({
      ok: false,
      message: err.message || 'Falha ao alocar sessao de jogo.',
    });
  }
});

app.post('/api/sessions/heartbeat', requireAuth, (req, res) => {
  const { sessionId } = req.body || {};
  const meta = sessions.get(sessionId);
  if (!meta || meta.username !== req.user.username) {
    return res.status(404).json({ ok: false, message: 'Sessao expirada ou inexistente.' });
  }
  touchSession(sessionId);
  res.json({ ok: true, lastActivity: meta.lastActivity });
});

app.delete('/api/sessions/:id', requireAuth, async (req, res) => {
  const meta = sessions.get(req.params.id);
  if (!meta || meta.username !== req.user.username) {
    return res.status(404).json({ ok: false, message: 'Sessao nao encontrada.' });
  }
  await destroySession(req.params.id, 'user_logout');
  res.json({ ok: true });
});

// ============================================================================
// Proxy Reverso Dinamico HTTP + WebSocket (/s/:sessionId/* -> container interno)
// ============================================================================
const proxy = httpProxy.createProxyServer({ ws: true, xfwd: true });

proxy.on('error', (err, req, res) => {
  console.warn('[Proxy] Erro:', err.message);
  if (res && typeof res.writeHead === 'function' && !res.headersSent) {
    res.writeHead(502, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ ok: false, message: 'Container de sessao indisponivel.' }));
  }
});

function extractCookie(cookieHeader, name) {
  if (!cookieHeader) return null;
  const parts = cookieHeader.split(';');
  for (const part of parts) {
    const [k, ...v] = part.trim().split('=');
    if (k === name) return decodeURIComponent(v.join('='));
  }
  return null;
}

app.use('/s/:sessionId', (req, res) => {
  const { sessionId } = req.params;
  const meta = sessions.get(sessionId);
  if (!meta) {
    return res.status(404).send('Sessao expirada ou encerrada por inatividade.');
  }

  const queryToken = req.query.token;
  const cookieToken = extractCookie(req.headers.cookie, `conquer_st_${sessionId}`);
  const token = queryToken || cookieToken;

  if (!verifyStreamToken(token, sessionId)) {
    return res.status(401).send('Token de sessao invalido ou expirado.');
  }

  if (queryToken) {
    res.setHeader(
      'Set-Cookie',
      `conquer_st_${sessionId}=${encodeURIComponent(queryToken)}; Path=/s/${sessionId}; HttpOnly; Secure; SameSite=None`
    );
  }

  touchSession(sessionId);
  proxy.web(req, res, {
    target: `http://${meta.containerIp}:${meta.containerPort}`,
  });
});

const server = http.createServer(app);

// Upgrade de WebSockets (KasmVNC / Selkies WebRTC Signaling)
server.on('upgrade', (req, socket, head) => {
  const match = /^\/s\/([a-f0-9]+)(\/.*)?$/i.exec(req.url || '');
  if (!match) {
    socket.destroy();
    return;
  }

  const sessionId = match[1];
  const meta = sessions.get(sessionId);
  if (!meta) {
    socket.destroy();
    return;
  }

  const urlObj = new URL(req.url, 'http://localhost');
  const token = urlObj.searchParams.get('token') || extractCookie(req.headers.cookie, `conquer_st_${sessionId}`);
  if (!verifyStreamToken(token, sessionId)) {
    socket.destroy();
    return;
  }

  meta.activeSockets += 1;
  touchSession(sessionId);
  socket.on('close', () => {
    meta.activeSockets = Math.max(0, meta.activeSockets - 1);
    touchSession(sessionId);
  });

  // Remove o prefixo /s/<sessionId> ao encaminhar para o container
  req.url = match[2] || '/';
  proxy.ws(req, socket, head, {
    target: `http://${meta.containerIp}:${meta.containerPort}`,
  });
});

startIdleReaper();

server.listen(PORT, '0.0.0.0', () => {
  console.log(`[Orchestrator] Online na porta ${PORT} | Engine=${CONFIG.engine} | MaxSessions=${CONFIG.maxConcurrentSessions}`);
});

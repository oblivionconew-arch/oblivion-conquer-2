const http = require('http');
const net = require('net');
const express = require('express');
const cors = require('cors');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');
const httpProxy = require('http-proxy');

const {
  authenticateGameAccount,
  registerGameAccount,
  getTopRanking,
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
const ALLOWED_ORIGINS = Array.from(
  new Set([
    ...(process.env.ALLOWED_ORIGINS || 'https://oblivionco.com.br,https://www.oblivionco.com.br')
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean),
    PUBLIC_STREAM_BASE_URL,
  ])
);

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

app.post('/api/auth/register', authLimiter, async (req, res) => {
  try {
    const { username, password, email } = req.body || {};
    const clientIp = req.ip || req.socket.remoteAddress || '0.0.0.0';
    const created = await registerGameAccount(
      String(username || '').trim(),
      String(password || '').trim(),
      email,
      clientIp
    );
    res.status(201).json({
      ok: true,
      username: created.username,
      message: 'Conta criada com sucesso no servidor! Voce ja pode jogar.',
    });
  } catch (err) {
    res.status(err.statusCode || 500).json({
      ok: false,
      message: err.message || 'Erro ao criar conta no banco de dados.',
    });
  }
});

app.get('/api/ranking', async (req, res) => {
  try {
    const rows = await getTopRanking(req.query.limit || 10);
    res.json({ ok: true, ranking: rows });
  } catch (err) {
    res.status(503).json({ ok: false, ranking: [], message: 'Ranking indisponivel no momento.' });
  }
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

app.post('/api/sessions/quick-start', authLimiter, async (req, res) => {
  try {
    const hdr = req.headers.authorization || '';
    const token = hdr.startsWith('Bearer ') ? hdr.slice(7) : null;
    const authUser = verifyAnyAuthToken(token);
    const username = authUser ? authUser.username : `Player_${Math.floor(1000 + Math.random() * 9000)}`;

    const meta = await createOrReuseSession(username);
    const streamToken = issueStreamToken(meta.sessionId, meta.username);
    const wsPath = `s/${meta.sessionId}/websockify?token=${encodeURIComponent(streamToken)}`;
    const streamPath = `/s/${meta.sessionId}/?token=${encodeURIComponent(streamToken)}&autoconnect=true&resize=scale&path=${encodeURIComponent(wsPath)}`;
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
    console.error('[QuickStart] Erro:', err);
    res.status(err.statusCode || 500).json({
      ok: false,
      message: err.message || 'Falha ao alocar sessao de jogo.',
    });
  }
});

app.post('/api/sessions/start', requireAuth, async (req, res) => {
  try {
    const meta = await createOrReuseSession(req.user.username);
    const streamToken = issueStreamToken(meta.sessionId, meta.username);
    const wsPath = `s/${meta.sessionId}/websockify?token=${encodeURIComponent(streamToken)}`;
    const streamPath = `/s/${meta.sessionId}/?token=${encodeURIComponent(streamToken)}&autoconnect=true&resize=scale&path=${encodeURIComponent(wsPath)}`;
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
const proxy = httpProxy.createProxyServer({ ws: true, xfwd: true, changeOrigin: true, secure: false });

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

function extractRefererToken(refererHeader) {
  if (!refererHeader) return null;
  try {
    return new URL(refererHeader).searchParams.get('token');
  } catch (_) {
    return null;
  }
}

app.use('/s/:sessionId', (req, res) => {
  const { sessionId } = req.params;
  const meta = sessions.get(sessionId);
  if (!meta) {
    return res.status(404).send('Sessao expirada ou encerrada por inatividade.');
  }

  const queryToken = req.query.token;
  const cookieToken = extractCookie(req.headers.cookie, `conquer_st_${sessionId}`);
  const refToken = extractRefererToken(req.headers.referer);
  const token = queryToken || cookieToken || refToken;

  if (!verifyStreamToken(token, sessionId)) {
    return res.status(401).send('Token de sessao invalido ou expirado.');
  }

  if (queryToken) {
    res.setHeader(
      'Set-Cookie',
      `conquer_st_${sessionId}=${encodeURIComponent(queryToken)}; Path=/s/${sessionId}; HttpOnly; Secure; SameSite=None; Partitioned`
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
    console.warn('[WS] Rejeitado URL fora de /s/:', req.url);
    socket.destroy();
    return;
  }

  const sessionId = match[1];
  const meta = sessions.get(sessionId);
  if (!meta) {
    console.warn('[WS] Sessao nao encontrada:', sessionId);
    socket.destroy();
    return;
  }

  const urlObj = new URL(req.url, 'http://localhost');
  const token =
    urlObj.searchParams.get('token') ||
    extractCookie(req.headers.cookie, `conquer_st_${sessionId}`) ||
    extractRefererToken(req.headers.referer);
  if (!verifyStreamToken(token, sessionId)) {
    console.warn('[WS] Token invalido/ausente para sessao:', sessionId, 'url:', req.url);
    socket.destroy();
    return;
  }

  meta.activeSockets += 1;
  touchSession(sessionId);
  socket.on('close', () => {
    meta.activeSockets = Math.max(0, meta.activeSockets - 1);
    touchSession(sessionId);
  });

  // Remove o prefixo /s/<sessionId> e limpa query ?token= ao encaminhar para KasmVNC
  const subPath = (match[2] || '/websockify').split('?')[0] || '/websockify';

  // Tunel TCP direto para KasmVNC garantindo cabecalhos HTTP/1.1 com capitalizacao padrao ("Host:")
  const upstream = net.connect(meta.containerPort, meta.containerIp, () => {
    const lines = [
      `GET ${subPath} HTTP/1.1`,
      `Host: ${meta.containerIp}:${meta.containerPort}`,
      `Upgrade: websocket`,
      `Connection: Upgrade`,
      `Sec-WebSocket-Key: ${req.headers['sec-websocket-key'] || ''}`,
      `Sec-WebSocket-Version: ${req.headers['sec-websocket-version'] || '13'}`,
    ];
    if (req.headers['sec-websocket-protocol']) {
      lines.push(`Sec-WebSocket-Protocol: ${req.headers['sec-websocket-protocol']}`);
    }
    if (req.headers['sec-websocket-extensions']) {
      lines.push(`Sec-WebSocket-Extensions: ${req.headers['sec-websocket-extensions']}`);
    }
    if (req.headers.origin) {
      lines.push(`Origin: ${req.headers.origin}`);
    }
    lines.push('', '');
    upstream.write(lines.join('\r\n'));
    if (head && head.length) upstream.write(head);
    socket.pipe(upstream);
    upstream.pipe(socket);
  });

  upstream.on('error', (err) => {
    console.warn('[WS] Erro upstream KasmVNC:', err.message);
    socket.destroy();
  });
  socket.on('error', () => upstream.destroy());
  socket.on('close', () => upstream.destroy());
  upstream.on('close', () => socket.destroy());
});

startIdleReaper();

server.listen(PORT, '0.0.0.0', () => {
  console.log(`[Orchestrator] Online na porta ${PORT} | Engine=${CONFIG.engine} | MaxSessions=${CONFIG.maxConcurrentSessions}`);
});

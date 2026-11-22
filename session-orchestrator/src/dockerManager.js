const crypto = require('crypto');
const http = require('http');
const Docker = require('dockerode');

const docker = new Docker({ socketPath: process.env.DOCKER_SOCKET || '/var/run/docker.sock' });

const CONFIG = {
  image: process.env.SESSION_IMAGE || 'oblivionco/conquer-web-client:latest',
  engine: process.env.SESSION_ENGINE || 'kasmvnc',
  containerPort: Number(process.env.SESSION_CONTAINER_PORT || 6901),
  dockerNetwork: process.env.DOCKER_NETWORK || 'conquer-stream-net',
  hostClientPath: process.env.HOST_CLIENT_PATH || '/opt/conquer/client',
  gameServerHost: process.env.GAME_SERVER_HOST || 'game.oblivionco.com.br',
  gameAuthPort: String(process.env.GAME_AUTH_PORT || 9958),
  gamePort: String(process.env.GAME_PORT || 5816),
  maxConcurrentSessions: Number(process.env.MAX_CONCURRENT_SESSIONS || 10),
  memoryBytes: Number(process.env.SESSION_MEMORY_MB || 1536) * 1024 * 1024,
  nanoCpus: Math.floor(Number(process.env.SESSION_CPUS || 1.0) * 1e9),
  pidsLimit: 256,
  idleTimeoutMs: Number(process.env.IDLE_TIMEOUT_MS || 5 * 60 * 1000), // 5 minutos
};

// Mapa em memoria: sessionId -> sessionMeta
const sessions = new Map();
// Mapa por usuario: username -> sessionId (1 container por jogador autenticado)
const userSessions = new Map();

function waitForContainerHttp(host, port, timeoutMs = 30000) {
  const deadline = Date.now() + timeoutMs;
  return new Promise((resolve, reject) => {
    const attempt = () => {
      const req = http.get({ host, port, path: '/', timeout: 2000 }, (res) => {
        res.resume();
        resolve(true);
      });
      req.on('error', () => {
        if (Date.now() > deadline) {
          return reject(new Error(`Timeout aguardando container ${host}:${port}`));
        }
        setTimeout(attempt, 1000);
      });
      req.on('timeout', () => {
        req.destroy();
        if (Date.now() > deadline) {
          return reject(new Error(`Timeout aguardando container ${host}:${port}`));
        }
        setTimeout(attempt, 1000);
      });
    };
    attempt();
  });
}

async function createOrReuseSession(username) {
  const existingId = userSessions.get(username);
  if (existingId && sessions.has(existingId)) {
    const existing = sessions.get(existingId);
    existing.lastActivity = Date.now();
    return existing;
  }

  if (sessions.size >= CONFIG.maxConcurrentSessions) {
    const err = new Error(`Servidor de streaming lotado (${sessions.size}/${CONFIG.maxConcurrentSessions}). Tente novamente em instantes.`);
    err.statusCode = 503;
    throw err;
  }

  const sessionId = crypto.randomBytes(12).toString('hex');
  const containerName = `conquer-sess-${sessionId}`;

  const container = await docker.createContainer({
    Image: CONFIG.image,
    name: containerName,
    Env: [
      `GAME_SERVER_HOST=${CONFIG.gameServerHost}`,
      `GAME_AUTH_PORT=${CONFIG.gameAuthPort}`,
      `GAME_PORT=${CONFIG.gamePort}`,
      `RESOLUTION=1024x768`,
      `VNC_PORT=${CONFIG.containerPort}`,
      `SELKIES_PORT=${CONFIG.containerPort}`,
    ],
    Labels: {
      'conquer.session': 'true',
      'conquer.sessionId': sessionId,
      'conquer.username': username,
    },
    HostConfig: {
      AutoRemove: true,
      Memory: CONFIG.memoryBytes,
      MemorySwap: CONFIG.memoryBytes,
      NanoCpus: CONFIG.nanoCpus,
      PidsLimit: CONFIG.pidsLimit,
      ShmSize: 256 * 1024 * 1024,
      Binds: [`${CONFIG.hostClientPath}:/opt/conquer-client:ro`],
      NetworkMode: CONFIG.dockerNetwork,
      SecurityOpt: ['no-new-privileges:true'],
    },
  });

  await container.start();

  const inspectData = await container.inspect();
  const netInfo = inspectData.NetworkSettings.Networks[CONFIG.dockerNetwork];
  const containerIp = netInfo ? netInfo.IPAddress : null;
  if (!containerIp) {
    await container.stop({ t: 2 }).catch(() => {});
    throw new Error('Nao foi possivel obter o IP interno do container na rede Docker.');
  }

  // Aguarda KasmVNC/Selkies iniciar dentro do container
  await waitForContainerHttp(containerIp, CONFIG.containerPort, 35000);

  const meta = {
    sessionId,
    username,
    containerId: container.id,
    containerName,
    containerIp,
    containerPort: CONFIG.containerPort,
    engine: CONFIG.engine,
    createdAt: Date.now(),
    lastActivity: Date.now(),
    activeSockets: 0,
  };

  sessions.set(sessionId, meta);
  userSessions.set(username, sessionId);
  return meta;
}

async function destroySession(sessionId, reason = 'user_request') {
  const meta = sessions.get(sessionId);
  if (!meta) return false;

  sessions.delete(sessionId);
  if (userSessions.get(meta.username) === sessionId) {
    userSessions.delete(meta.username);
  }

  console.log(`[Session] Encerrando ${meta.containerName} (user=${meta.username}, reason=${reason})`);
  try {
    const container = docker.getContainer(meta.containerId);
    await container.stop({ t: 5 }).catch(() => {});
    await container.remove({ force: true }).catch(() => {});
  } catch (_) {}
  return true;
}

function touchSession(sessionId) {
  const meta = sessions.get(sessionId);
  if (meta) {
    meta.lastActivity = Date.now();
    return true;
  }
  return false;
}

async function cleanupOrphanContainers() {
  try {
    const list = await docker.listContainers({
      all: true,
      filters: { label: ['conquer.session=true'] },
    });
    for (const c of list) {
      const sid = c.Labels && c.Labels['conquer.sessionId'];
      if (!sid || !sessions.has(sid)) {
        console.log(`[Cleanup] Removendo container orfao ${c.Names[0]}`);
        const inst = docker.getContainer(c.Id);
        await inst.stop({ t: 2 }).catch(() => {});
        await inst.remove({ force: true }).catch(() => {});
      }
    }
  } catch (err) {
    console.warn('[Cleanup] Aviso ao listar containers Docker:', err.message);
  }
}

module.exports = {
  CONFIG,
  sessions,
  userSessions,
  createOrReuseSession,
  destroySession,
  touchSession,
  cleanupOrphanContainers,
};

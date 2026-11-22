const { CONFIG, sessions, destroySession, cleanupOrphanContainers } = require('./dockerManager');

function startIdleReaper() {
  // Limpa containers orfaos de restarts anteriores
  cleanupOrphanContainers();

  const interval = setInterval(async () => {
    const now = Date.now();
    for (const [sessionId, meta] of sessions.entries()) {
      const idleMs = now - meta.lastActivity;
      if (idleMs > CONFIG.idleTimeoutMs) {
        console.log(
          `[Reaper] Sessao ${sessionId} (user=${meta.username}) inativa ha ${Math.round(idleMs / 1000)}s -> encerrando.`
        );
        await destroySession(sessionId, 'idle_timeout_5m');
      }
    }
  }, 30 * 1000);

  interval.unref();
  return interval;
}

module.exports = { startIdleReaper };

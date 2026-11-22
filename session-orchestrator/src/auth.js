const jwt = require('jsonwebtoken');
const mysql = require('mysql2/promise');

const JWT_SECRET = process.env.JWT_SECRET || 'dev-secret-change-me';
const SUPABASE_JWT_SECRET = process.env.SUPABASE_JWT_SECRET || '';
const ALLOW_GUEST_DEMO = process.env.ALLOW_GUEST_DEMO === 'true';

let mysqlPool = null;

function getMysqlPool() {
  if (!mysqlPool && process.env.MYSQL_HOST && process.env.MYSQL_DATABASE) {
    mysqlPool = mysql.createPool({
      host: process.env.MYSQL_HOST,
      port: Number(process.env.MYSQL_PORT || 3306),
      user: process.env.MYSQL_USER || 'conquer',
      password: process.env.MYSQL_PASSWORD || '',
      database: process.env.MYSQL_DATABASE || 'conquer',
      waitForConnections: true,
      connectionLimit: 10,
      queueLimit: 0,
    });
  }
  return mysqlPool;
}

/**
 * Autentica contra a tabela `accounts` do Conquer Online 2.0 (MySQL/MariaDB)
 */
async function authenticateGameAccount(username, password) {
  if (!username || !password) return null;
  if (!/^[A-Za-z0-9_]{3,25}$/.test(username)) return null;

  const pool = getMysqlPool();
  if (!pool) {
    if (ALLOW_GUEST_DEMO) {
      return { username, source: 'demo' };
    }
    throw new Error('MySQL nao configurado no orquestrador.');
  }

  const [rows] = await pool.execute(
    'SELECT Username, Password, State FROM accounts WHERE Username = ? LIMIT 1',
    [username]
  );

  if (!rows || rows.length === 0) return null;
  const acc = rows[0];
  if (String(acc.Password).trim() !== String(password).trim()) return null;
  // State 0 ou 1 em algumas sources pode indicar banido (State 2 = Player, 4 = GM, 5 = PM)
  if (Number(acc.State) === 1) {
    throw new Error('Conta banida.');
  }

  return { username: String(acc.Username).trim(), state: Number(acc.State), source: 'mysql' };
}

function issueUserToken(payload) {
  return jwt.sign(payload, JWT_SECRET, { expiresIn: '12h' });
}

function issueStreamToken(sessionId, username) {
  return jwt.sign({ sid: sessionId, sub: username, typ: 'stream' }, JWT_SECRET, { expiresIn: '12h' });
}

/**
 * Valida Bearer token emitido pelo orquestrador OU opcionalmente um JWT do Supabase Auth
 */
function verifyAnyAuthToken(token) {
  if (!token) return null;
  try {
    const decoded = jwt.verify(token, JWT_SECRET);
    if (decoded && decoded.sub) {
      return { username: decoded.sub, provider: 'orchestrator' };
    }
  } catch (_) {}

  if (SUPABASE_JWT_SECRET) {
    try {
      const sb = jwt.verify(token, SUPABASE_JWT_SECRET);
      if (sb && sb.sub) {
        return {
          username: (sb.user_metadata && sb.user_metadata.username) || sb.email || sb.sub,
          provider: 'supabase',
        };
      }
    } catch (_) {}
  }
  return null;
}

function verifyStreamToken(token, expectedSessionId) {
  if (!token) return null;
  try {
    const decoded = jwt.verify(token, JWT_SECRET);
    if (decoded.typ === 'stream' && decoded.sid === expectedSessionId) {
      return decoded;
    }
  } catch (_) {}
  return null;
}

module.exports = {
  authenticateGameAccount,
  issueUserToken,
  issueStreamToken,
  verifyAnyAuthToken,
  verifyStreamToken,
};

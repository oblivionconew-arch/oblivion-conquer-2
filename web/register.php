<?php
header('Content-Type: application/json; charset=utf-8');

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['ok' => false, 'message' => 'Método não permitido.']);
    exit;
}

$username = trim($_POST['username'] ?? '');
$password = trim($_POST['password'] ?? '');
$email    = trim($_POST['email'] ?? '');

if (!preg_match('/^[A-Za-z0-9_]{4,16}$/', $username)) {
    echo json_encode(['ok' => false, 'message' => 'Usuário inválido: use 4 a 16 letras, números ou _.']);
    exit;
}
if (strlen($password) < 4 || strlen($password) > 16) {
    echo json_encode(['ok' => false, 'message' => 'Senha inválida: use entre 4 e 16 caracteres.']);
    exit;
}

$cfg = require __DIR__ . '/config.php';

try {
    $dsn = sprintf('mysql:host=%s;port=%s;dbname=%s;charset=utf8mb4', $cfg['db_host'], $cfg['db_port'], $cfg['db_name']);
    $pdo = new PDO($dsn, $cfg['db_user'], $cfg['db_pass'], [
        PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
        PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
    ]);

    // Verifica se o usuario ja existe
    $check = $pdo->prepare('SELECT Username FROM accounts WHERE Username = :u LIMIT 1');
    $check->execute([':u' => $username]);
    if ($check->fetch()) {
        echo json_encode(['ok' => false, 'message' => 'Este nome de usuário já está em uso!']);
        exit;
    }

    // Insere nova conta com State = 2 (Player padrao na source Oblivion)
    $stmt = $pdo->prepare(
        "INSERT INTO accounts (Username, Password, State, Email, IP, EntityID, lastvote) VALUES (:u, :p, 2, :e, :ip, 0, '')"
    );
    $stmt->execute([
        ':u'  => $username,
        ':p'  => $password,
        ':e'  => $email,
        ':ip' => $_SERVER['REMOTE_ADDR'] ?? '0.0.0.0',
    ]);

    echo json_encode(['ok' => true, 'message' => 'Conta criada com sucesso! Já pode entrar no jogo.']);
} catch (Throwable $ex) {
    http_response_code(500);
    echo json_encode(['ok' => false, 'message' => 'Erro ao conectar ao banco de dados do servidor.']);
}

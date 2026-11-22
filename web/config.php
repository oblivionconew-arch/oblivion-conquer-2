<?php
// Configuracao de conexao MySQL para o site do Oblivion Conquer 2.0
// Ajuste com os dados gerados pelo setup_vps_ubuntu.sh (ou XAMPP local)
return [
    'db_host' => getenv('CONQUER_DB_HOST') ?: '127.0.0.1',
    'db_port' => getenv('CONQUER_DB_PORT') ?: '3306',
    'db_name' => getenv('CONQUER_DB_NAME') ?: 'conquer',
    'db_user' => getenv('CONQUER_DB_USER') ?: 'conquer',
    'db_pass' => getenv('CONQUER_DB_PASS') ?: 'TROQUE_ESTA_SENHA',
];

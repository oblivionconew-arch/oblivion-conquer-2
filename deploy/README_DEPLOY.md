# Guia de Deploy 24/7 — Conquer Online 2.0 (Ubuntu Linux + Wine + systemd)

## Estrutura do Workspace (`conquer-production`)

- `server/src/Source/` — Código-fonte C# (.NET 4.0 x86) do servidor `Conquer_Online_Server` já limpo de modo Debug e pronto para execução headless.
- `dist/server/` — Binários **Release x86** já compilados (`Conquer_Online_Server.exe`), DLLs de runtime, pasta `database/` e `configuration.ini` configurável.
- `client-hook/` — Projeto C++17 (Win32 x86, `/MT` estático) para gerar `ConquerHook.dll` e `PlayLauncher.exe` (substituindo `CSV3Hook.dll` e `play.exe` em Debug).
- `tools/serverdat-gen/` — Gerador de `server.dat` em C# (`dist/tools/ServerDatGen.exe`) e Python (`serverdat_gen.py`).
- `deploy/` — Scripts de provisionamento Ubuntu (`setup_vps_ubuntu.sh`), inicialização headless (`start_server.sh`), unit `systemd` (`conquer-server.service`) e dump SQL (`sql/dbb.sql`).

---

## 1. Deploy do Servidor na VPS Ubuntu (22.04 / 24.04 LTS)

1. Envie a pasta `conquer-production` inteira para a VPS (ex.: `/tmp/conquer-production` via `scp`/`rsync`).
2. Execute o provisionamento automático informando seu IP/domínio e portas desejadas:

```bash
cd /tmp/conquer-production/deploy
sudo EXTERNAL_IP="SEU_IP_OU_DOMINIO" AUTH_PORT=9958 GAME_PORT=5816 SERVER_NAME="Oblivion" bash setup_vps_ubuntu.sh
```

3. Inicie e acompanhe o servidor 24/7:

```bash
sudo systemctl start conquer-server
sudo systemctl status conquer-server
sudo journalctl -u conquer-server -f
```

> **Nota**: O emulador real encontrado nos seus arquivos unifica Login (`9958`), Game (`5816`) e NPC no executável único `Conquer_Online_Server.exe`. Caso no futuro você utilize binários C++ TQ separados (`AccountServer`, `MsgServer`, `NpcServer`), consulte os templates ordenados de referência em `deploy/systemd/legacy-3process-reference/`.

---

## 2. Compilação da `ConquerHook.dll` e `PlayLauncher.exe` (Zero Dependência de Debug)

### Opção A — Diretamente na VPS Ubuntu (Cross-Compile Win32 Estático)
O `setup_vps_ubuntu.sh` já instala `g++-mingw-w64-i686`. Basta rodar na VPS:

```bash
cd /tmp/conquer-production/client-hook
bash build_mingw_linux.sh
```

Os binários finais sem dependências (`ConquerHook.dll`, `PlayLauncher.exe`, `conquer_hook.ini`) serão gerados em `dist/client/`.

### Opção B — No Windows com Visual Studio (2019/2022 + Desktop C++)
1. Abra `client-hook/ConquerHook.sln`.
2. Selecione **`Release | Win32`** (já pré-configurado com linkagem estática **`/MT`**).
3. Compile a solução — os binários saem em `dist/client/`.

### Instalação no Cliente Windows (`OblivionCoClienteFull`)
1. Copie `ConquerHook.dll`, `PlayLauncher.exe` e `conquer_hook.ini` para a pasta raiz do cliente (onde fica o `Conquer.exe`).
2. Remova os arquivos antigos em Debug (`play.exe` e `CSV3Hook.dll`).
3. Edite `conquer_hook.ini` com o IP/domínio da sua VPS e porta `9958`:
   ```ini
   [config]
   host=SEU_IP_OU_DOMINIO
   auth_port=9958
   game_port=5816
   multiclient=1
   bypass_antidebug=1
   block_autopatch=1
   ```
4. Abra o jogo pelo `PlayLauncher.exe` (duplo clique, sem Visual Studio ou debugger, com multi-client liberado).

---

## 3. Gerador de `server.dat`

- **Gerar `server.dat` legado criptografado (cifra TQ `0x2537`)**:
  ```powershell
  .\dist\tools\ServerDatGen.exe --generate --mode legacy-dat --name "Oblivion" --ip "SEU_IP_OU_DOMINIO" --port 9958 --out server.dat
  ```
- **Gerar `server.dat` Flash XML (v5517–6022+)**:
  ```powershell
  .\dist\tools\ServerDatGen.exe --generate --mode xml --name "Oblivion" --ip "SEU_IP_OU_DOMINIO" --port 9958 --out server.dat
  ```
- **Gerar `server.dat` criptografado em blocos RSA-1024 (128 bytes/bloco)**:
  ```powershell
  .\dist\tools\ServerDatGen.exe --generate --mode rsa1024 --name "Oblivion" --ip "SEU_IP_OU_DOMINIO" --port 9958 --out server.dat
  ```
- **No Linux (Python 3)**:
  ```bash
  python3 tools/serverdat-gen/serverdat_gen.py --mode xml --name "Oblivion" --ip "SEU_IP_OU_DOMINIO" --port 9958 --out server.dat
  ```

---

## 4. Operação e Backup Diário (Cron)

```bash
# Reiniciar servidor com salvamento gracioso
sudo systemctl restart conquer-server

# Backup diário do MySQL (adicionar em: sudo crontab -e)
0 4 * * * mysqldump -u root conquer | gzip > /var/backups/conquer_$(date +\%F).sql.gz
```

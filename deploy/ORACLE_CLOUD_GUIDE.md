# Guia Completo — Oracle Cloud Always Free (R$ 0 para Sempre)

A **Oracle Cloud Always Free** oferece os melhores recursos gratuitos permanentes para rodar o **Oblivion Conquer 2.0**:
- **2 VMs AMD x86_64 (`VM.Standard.E2.1.Micro`)** — 1 GB RAM (+ 4 GB Swap criado pelo nosso script) → **ideal para o Servidor Conquer (`Conquer_Online_Server.exe` + MariaDB + API de Cadastro)**.
- **Até 4 OCPUs + 24 GB RAM ARM (`VM.Standard.A1.Flex`)** + **200 GB disco** + **10 TB/mês de tráfego** → **ideal para o Streaming Docker (`KasmVNC` para Navegador e APK Android)**.

---

## Passo 1 — Criar a Conta Oracle Cloud Always Free
1. Acesse: **https://www.oracle.com/br/cloud/free/**
2. Cadastre-se com seu e-mail (`oblivionconew@gmail.com`).
3. **Home Region (muito importante — não pode mudar depois!):** escolha **`Brazil Southeast (Vinhedo)`** *(costuma ter mais disponibilidade de VMs grátis e ping ~15–20 ms no Brasil)* ou **`Brazil East (São Paulo)`**.
4. Valide o cartão de crédito (a Oracle faz apenas uma pré-autorização temporária estornada para evitar bots e **não cobra** se você mantiver a conta em *Always Free*).

---

## Passo 2 — Criar a VM #1 (Servidor Conquer + MySQL + Cadastro do Site)
1. No painel OCI → **Compute → Instances → Create Instance**:
   - **Name:** `conquer-game-server`
   - **Image:** **Canonical Ubuntu 22.04** *(não escolha Minimal)*
   - **Shape:** **AMD → `VM.Standard.E2.1.Micro` (Always Free-eligible)**
   - **SSH Keys:** clique em **Save Private Key** (guarde o arquivo `.key` no seu PC para conectar via SSH!).
2. Clique em **Create** e anote o **Public IP Address** (ex.: `144.22.xxx.xxx`).

---

## Passo 3 — Liberar as Portas no Firewall da Oracle Cloud (OBRIGATÓRIO!)
1. Na página da instância → clique no nome da **Subnet** (em *Primary VNIC*).
2. Clique em **Default Security List for ...** → **Add Ingress Rules**.
3. Adicione **1 regra** (ou 4 regras separadas) com:
   - **Source CIDR:** `0.0.0.0/0`
   - **IP Protocol:** `TCP`
   - **Destination Port Range:** `80,443,9958,5816` *(se a OCI pedir separado, crie uma regra para cada porta: `80`, `443`, `9958` e `5816`)*.

---

## Passo 4 — Criar os Registros DNS na Cloudflare (`oblivionco.com.br`)
Assim que `oblivionco.com.br` ficar **Active** na Cloudflare, vá em **DNS → Records** e adicione:
- **`game`** (`A`) → `<IP_PUBLICO_DA_VM_1>` → **DNS Only (Cinza — NÃO ativar laranja!)**
- **`stream`** (`A`) → `<IP_PUBLICO_DA_VM_1>` *(ou da VM #2 ARM se criar)* → **Proxied (Laranja)**

---

## Passo 5 — Instalar Tudo na VM com 3 Comandos (via SSH)

No PowerShell do Windows (substitua o caminho da chave `.key` e o IP da sua VM):

```powershell
# 1. Enviar os binarios Release do servidor e o dump dbb.sql para a VM
scp -i "C:\caminho\para\sua-chave.key" -r "C:\Users\User\.gemini\antigravity\scratch\conquer-production\dist\server" ubuntu@SEU_IP_OCI:/tmp/conquer-server
scp -i "C:\Users\User\.gemini\antigravity\scratch\conquer-production\deploy\sql\dbb.sql" ubuntu@SEU_IP_OCI:/tmp/dbb.sql
```

Depois conecte via SSH (`ssh -i "C:\caminho\para\sua-chave.key" ubuntu@SEU_IP_OCI`) e execute:

```bash
git clone https://github.com/oblivionconew-arch/oblivion-conquer-2.git /opt/conquer-production
mkdir -p /opt/conquer-production/dist/server /opt/conquer-production/deploy/sql
cp -a /tmp/conquer-server/. /opt/conquer-production/dist/server/
cp /tmp/dbb.sql /opt/conquer-production/deploy/sql/dbb.sql

cd /opt/conquer-production/deploy
sudo EXTERNAL_IP="game.oblivionco.com.br" AUTH_PORT=9958 GAME_PORT=5816 SERVER_NAME="Oblivion" bash setup_oracle_cloud.sh
```

Depois inicie o servidor Conquer e a API de cadastro/streaming:
```bash
sudo systemctl start conquer-server
sudo journalctl -u conquer-server -f
```

---

## Passo 6 (Opcional) — Criar VM #2 `Ampere A1.Flex` (4 OCPUs + 24 GB RAM Always Free) para Streaming Web/Mobile
Para suportar vários jogadores simultâneos jogando pelo navegador (`play.html`) e pelo `Conquer-Mobile.apk`:
1. Crie uma 2ª instância com **Shape: Ampere → `VM.Standard.A1.Flex` (4 OCPUs, 24 GB RAM, Always Free)** e **Ubuntu 22.04**.
2. Libere as portas `80` e `443` na Security List.
3. Aponte o registro DNS `stream.oblivionco.com.br` na Cloudflare para o IP dessa VM #2.
4. Clone o repositório e rode `sudo bash setup_oracle_cloud.sh` — o script detecta ARM64 automaticamente e prepara o Docker para o streaming!

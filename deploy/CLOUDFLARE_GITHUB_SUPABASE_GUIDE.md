# Guia de Produção: `oblivionco.com.br` (Registro.br + Cloudflare + GitHub + Supabase)

Este guia conecta **todas as suas contas já criadas** em menos de 15 minutos para colocar **Site + Jogo no Navegador + APK Android** no ar em **`https://oblivionco.com.br`**.

---

## Passo 1 — Delegar `oblivionco.com.br` do Registro.br para a Cloudflare

1. No painel da **Cloudflare** (`oblivionconew@gmail.com`), clique em **Adicionar site (Add a site)** e digite `oblivionco.com.br` → escolha o plano **Free (R$ 0)**.
2. A Cloudflare exibirá 2 **Nameservers** (ex.: `anna.ns.cloudflare.com` e `bob.ns.cloudflare.com`).
3. No **Registro.br** (onde você já está logado), abra `oblivionco.com.br` → **DNS** → **Alterar servidores DNS** → cole os 2 Nameservers da Cloudflare e salve.
4. Na Cloudflare → **SSL/TLS** → configure o modo **Full (strict)** e ative **Always Use HTTPS** e **WebSockets** (em *Network*).

---

## Passo 2 — Configurar Registros DNS na Cloudflare

Na aba **DNS → Records** do domínio `oblivionco.com.br`, crie exatamente estes registros:

| Tipo | Nome | Conteúdo | Status Proxy | Finalidade |
| :--- | :--- | :--- | :--- | :--- |
| `CNAME` | `@` | `<seu-projeto>.pages.dev` *(criado no Passo 3)* | **Proxied (Laranja)** | Site principal + `play.html` (`https://oblivionco.com.br`) |
| `CNAME` | `www` | `oblivionco.com.br` | **Proxied (Laranja)** | Redirecionamento `www` |
| `A` | `game` | `<IP_DA_VPS_CONQUER>` | **DNS Only (Cinza — NÃO ativar laranja!)** | Servidor Conquer TCP `9958` (Auth) e `5816` (Game) |
| `A` | `stream` | `<IP_DA_VPS_STREAMING>` | **Proxied (Laranja)** *(KasmVNC)* ou **DNS Only** *(WebRTC)* | Orquestrador Node.js + Containers Docker (`stream.oblivionco.com.br`) |

> **Importante**: O subdomínio `game.oblivionco.com.br` **precisa ficar cinza (DNS Only)** porque o proxy laranja da Cloudflare só trafega HTTP/HTTPS e bloquearia as portas TCP `9958` e `5816` do Conquer.

---

## Passo 3 — Subir o Repositório no GitHub + Publicar Site no Cloudflare Pages (Grátis)

1. Crie um repositório no GitHub (`oblivionconew@gmail.com`), por exemplo `oblivion-conquer-2`, e faça push da pasta `C:\Users\User\.gemini\antigravity\scratch\conquer-production`:
   ```bash
   git init
   git add .
   git commit -m "Oblivion Conquer 2.0: Servidor PT-BR, Web Streaming, APK Capacitor"
   git branch -M main
   git remote add origin https://github.com/<SEU_USUARIO>/oblivion-conquer-2.git
   git push -u origin main
   ```
2. Na **Cloudflare** → **Workers & Pages** → **Create** → **Pages** → **Connect to Git**:
   - Escolha o repositório `oblivion-conquer-2`
   - **Build command**: *(deixe vazio)*
   - **Build output directory**: `web`
3. Em **Custom domains**, adicione `oblivionco.com.br` e `www.oblivionco.com.br`. Em ~1 minuto seu site + `https://oblivionco.com.br/play.html` estarão online com HTTPS!

---

## Passo 4 — Gerar `Conquer-Mobile.apk` Automaticamente na Nuvem (GitHub Actions)

1. No seu repositório GitHub → aba **Actions** → selecione **Build Conquer-Mobile.apk (Android Capacitor)** → clique em **Run workflow** (ou crie uma tag `git tag v1.0.0 && git push origin v1.0.0`).
2. Em ~4 minutos o GitHub compila, assina e publica automaticamente o arquivo **`Conquer-Mobile.apk`** em **Releases**!
3. *(Opcional para assinatura permanente da Play Store/Updates)*: gere sua keystore uma vez com `keytool`, converta para base64 e salve em **Settings → Secrets and variables → Actions**:
   - `ANDROID_KEYSTORE_BASE64`
   - `KEYSTORE_PASSWORD`
   - `KEY_ALIAS`
   - `KEY_PASSWORD`
4. Atualize o botão de download do APK em `web/index.html` para:
   `https://github.com/<SEU_USUARIO>/oblivion-conquer-2/releases/latest/download/Conquer-Mobile.apk`

---

## Passo 5 — Subir o Streaming Docker na VPS Linux (`stream.oblivionco.com.br`)

1. Copie o cliente limpo (gerado pelo `FixClient.ps1`) para `/opt/conquer/client` na VPS.
2. Configure o `.env` e construa a imagem de streaming (ela já cross-compila a `ConquerHook.dll` e o `PlayLauncher.exe` automaticamente!):
   ```bash
   cd /opt/conquer-production/web-client
   cp .env.example .env
   nano .env   # preencha JWT_SECRET, MYSQL_PASSWORD e GAME_SERVER_HOST=game.oblivionco.com.br

   # Construir imagem do cliente Conquer (Wine32 + KasmVNC + ConquerHook.dll)
   docker compose --profile build-only build conquer-web-image

   # Subir Orquestrador Node.js + Proxy HTTPS Caddy
   docker compose up -d --build orchestrator caddy
   ```
3. Teste a saúde da API:
   ```bash
   curl https://stream.oblivionco.com.br/api/health
   ```

---

## Passo 6 — Integração Opcional com Supabase (`oblivionconew-arch`)

Se desejar autenticar jogadores também via **Supabase Auth** (além da tabela `accounts` do MySQL):
1. No painel do Supabase (`oblivionconew-arch`) → **Project Settings → API**, copie `Project URL` e `JWT Secret`.
2. Cole em `web-client/.env`:
   ```env
   SUPABASE_URL=https://<seu-projeto>.supabase.co
   SUPABASE_JWT_SECRET=<seu-jwt-secret>
   ```
3. Reinicie o orquestrador (`docker compose restart orchestrator`). Ele passará a aceitar tanto o login direto da tabela `accounts` do Conquer quanto JWTs do Supabase automaticamente.

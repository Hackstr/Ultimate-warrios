# Deployment Guide — Tactical Duelist

## Architecture Overview

```
┌─────────────────────┐     ┌─────────────────────┐
│   Telegram Client    │     │    Unity WebGL       │
│   (Mini App)         │────▶│    (nginx :8080)     │
└─────────────────────┘     └──────────┬──────────┘
                                       │ Socket.IO + HTTP
                                       ▼
                            ┌──────────────────────┐
                            │   NestJS Server       │
                            │   (:3000)             │
                            └─────┬──────────┬──────┘
                                  │          │
                          ┌───────▼──┐  ┌────▼──────┐
                          │ Postgres │  │   Redis   │
                          │ (:5432)  │  │  (:6379)  │
                          └──────────┘  └───────────┘
```

All services run inside Docker via `docker-compose.production.yml`.

---

## Prerequisites

- **VPS/Server** with Docker and Docker Compose installed (Ubuntu 22.04+ recommended)
- **Domain name** (e.g., `api.tactical-duelist.com`) with DNS pointing to your server
- **Telegram Bot** created via [@BotFather](https://t.me/BotFather)
- **Unity 6** for building the WebGL client

---

## Step 1: Create Telegram Bot

1. Open [@BotFather](https://t.me/BotFather) in Telegram
2. Send `/newbot` and follow prompts to create a bot
3. Copy the **bot token** (format: `123456:ABC-DEF...`)
4. Send `/newapp` to BotFather:
   - Select your bot
   - Enter app title: **Tactical Duelist**
   - Enter app description
   - Upload a 640x360 photo (or skip)
   - **Web App URL**: `https://YOUR_DOMAIN:8080` (or wherever nginx serves the WebGL build)
5. Save the bot token for the `.env.production` file

---

## Step 2: Prepare Server

```bash
# SSH into your server
ssh user@your-server-ip

# Clone or upload the project
git clone YOUR_REPO_URL tactical-duelist
cd tactical-duelist

# Create production environment file
cp .env.production.example .env.production

# Edit with real values
nano .env.production
```

Fill in `.env.production`:
```env
POSTGRES_PASSWORD=generate_a_strong_password
REDIS_PASSWORD=generate_another_password
JWT_SECRET=$(openssl rand -hex 32)
TELEGRAM_BOT_TOKEN=your_bot_token_from_botfather
PORT=3000
WEBGL_PORT=8080
```

---

## Step 3: Build Unity WebGL

In Unity Editor:

1. **File → Build Settings**
2. Select **WebGL** platform, click **Switch Platform**
3. **Player Settings:**
   - **Resolution and Presentation → WebGL Template**: Select `TelegramMiniApp`
   - **Publishing Settings → Compression Format**: `Gzip` (or `Brotli` for smaller builds)
   - **Publishing Settings → Decompression Fallback**: Enabled
4. Click **Build** and select output folder
5. Copy the build output to `WebGLBuild/` directory in the project root:

```bash
# From your local machine
scp -r /path/to/unity/build/* user@your-server-ip:~/tactical-duelist/WebGLBuild/
```

The `WebGLBuild/` folder should contain:
```
WebGLBuild/
├── index.html
├── style.css
├── Build/
│   ├── WebGL.data.gz
│   ├── WebGL.framework.js.gz
│   ├── WebGL.loader.js
│   └── WebGL.wasm.gz
└── TemplateData/ (if any)
```

---

## Step 4: Deploy

```bash
cd tactical-duelist

# Build and start all services
docker compose -f docker-compose.production.yml --env-file .env.production up -d --build

# Check logs
docker compose -f docker-compose.production.yml logs -f server
docker compose -f docker-compose.production.yml logs -f webgl

# Verify services are running
docker compose -f docker-compose.production.yml ps
```

---

## Step 5: Set Web App URL in BotFather

1. Open [@BotFather](https://t.me/BotFather)
2. Send `/myapps`
3. Select your app
4. Edit **Web App URL** to: `http://YOUR_SERVER_IP:8080`
   - Or `https://your-domain.com` if you set up SSL

---

## Step 6: Test

1. Open your Telegram bot
2. Tap the **Menu Button** or the Mini App button
3. The game should load in the Telegram WebView
4. Ask a friend to do the same — you should be able to matchmake and play!

---

## SSL/HTTPS (Recommended for Production)

For production, add a reverse proxy with SSL. Example with Caddy:

```bash
# Install Caddy on host (not in Docker)
sudo apt install -y caddy

# Edit Caddyfile
sudo nano /etc/caddy/Caddyfile
```

```
your-domain.com {
    reverse_proxy localhost:8080
}

api.your-domain.com {
    reverse_proxy localhost:3000
}
```

```bash
sudo systemctl restart caddy
```

Then update:
- BotFather Web App URL → `https://your-domain.com`
- Unity client server URL → `https://api.your-domain.com`

---

## Useful Commands

```bash
# View server logs
docker compose -f docker-compose.production.yml logs -f server

# Restart server after code changes
docker compose -f docker-compose.production.yml up -d --build server

# Access database
docker compose -f docker-compose.production.yml exec postgres psql -U duelist -d tactical_duelist

# Stop everything
docker compose -f docker-compose.production.yml down

# Full reset (destroys data!)
docker compose -f docker-compose.production.yml down -v
```

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| WebGL build shows blank page | Check browser console; ensure nginx serves `.gz` with correct headers |
| Socket.IO connection fails | Verify `server` URL query param or `window.location.origin` matches the NestJS port |
| Auth fails in Telegram | Verify `TELEGRAM_BOT_TOKEN` matches your bot; check server logs |
| Database connection error | Check `DATABASE_URL` in server env; verify postgres is healthy |
| Redis connection refused | Verify Redis password matches in both server and docker-compose |

---

## Quick Local Test (without VPS)

You can test the full flow locally:

```bash
# 1. Start all services
docker compose -f docker-compose.production.yml --env-file .env.production up -d --build

# 2. Build WebGL from Unity, output to WebGLBuild/

# 3. Open http://localhost:8080 in browser
#    (won't have Telegram context, but can test loading)

# 4. For Telegram test, use ngrok:
ngrok http 8080
# Copy the ngrok URL and set it as Web App URL in BotFather
```

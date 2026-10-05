# Exposing & Hosting Al-Dawah Pharma from Your Laptop (Without a Domain)

This guide walks you through exposing your locally running Al-Dawah Pharma application (`http://localhost:5091`) to the public internet so anyone can access it from their phone, PC, or remote device without needing to buy a domain or configure router port forwarding.

---

## 1. Quick Comparison of Free Solutions

When hosting from a laptop behind residential Wi-Fi (often blocked by CGNAT or dynamic IPs), **reverse tunnel services** are the safest and easiest solution. They provide a free public URL with automatic HTTPS encryption and route traffic directly to your laptop.

| Method | Setup Time | Account Needed? | Free HTTPS? | Stability & Limits | Best For |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Cloudflare Quick Tunnel** | 1 min | ❌ No | ✅ Yes | Random `*.trycloudflare.com` URL; unlimited bandwidth | **Recommended (Best overall)** |
| **Pinggy (SSH Tunnel)** | 10 sec | ❌ No (No install needed) | ✅ Yes | Free 60-min sessions; random URL | **Quick 1-minute test** |
| **ngrok** | 3 min | ✅ Yes (Free signup) | ✅ Yes | Interstitial warning page on free tier | Standard developer tool |
| **LocalTunnel (`npx`)** | 1 min | ❌ No | ✅ Yes | Occasional password prompt on visit | Node.js users |

---

## 2. Option A: Cloudflare Quick Tunnels (Recommended)

Cloudflare Tunnels provide high-speed, secure routing with full SSL/TLS certificates and no bandwidth caps.

### Step 1: Install `cloudflared` on Linux

On Arch / Arch-based distributions:
```bash
sudo pacman -S cloudflared
```

Or via direct binary download:
```bash
curl -L --output cloudflared.deb https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-amd64.deb
sudo dpkg -i cloudflared.deb
```
*(Or download the standalone binary into `/usr/local/bin/cloudflared`).*

### Step 2: Start the Tunnel

Run this single command while your Docker containers are running:

```bash
cloudflared tunnel --url http://localhost:5091
```

### Step 3: Share the URL

In the terminal output, look for a log entry like:
```text
+--------------------------------------------------------------------------------------------+
|  Your quick Tunnel has been created! Visit it at (it may take some time to be reachable):  |
|  https://random-words-1234.trycloudflare.com                                               |
+--------------------------------------------------------------------------------------------+
```

Copy that HTTPS URL and send it to anyone. They can now access your application from anywhere in the world.

> [!TIP]
> Keep that terminal window open. If you close the terminal or press `Ctrl + C`, the tunnel closes and the URL will stop working.

---

## 3. Option B: Pinggy (Zero Installation via SSH)

If you need to share a link right now without downloading or installing any new software:

```bash
ssh -p 443 -R0:localhost:5091 a.pinggy.io
```

- When prompted `Are you sure you want to continue connecting (yes/no/[fingerprint])?`, type `yes`.
- It will immediately output an HTTPS URL like `https://xxxx.a.pinggy.link`.
- Share that link with your users.

---

## 4. Option C: ngrok

ngrok is a classic choice for web developers.

### Step 1: Install & Authenticate
1. Sign up for a free account at [dashboard.ngrok.com](https://dashboard.ngrok.com/signup).
2. Install ngrok:
   ```bash
   # Arch Linux:
   sudo pacman -S ngrok
   ```
3. Add your auth token (found on your ngrok dashboard):
   ```bash
   ngrok config add-authtoken <YOUR_NGROK_AUTH_TOKEN>
   ```

### Step 2: Expose Port 5091
```bash
ngrok http 5091
```

ngrok will display a forwarding URL like `https://abcd-123-456.ngrok-free.app`.
*(Note: Visitors on the free tier will see an initial interstitial splash page clicking "Visit Site" before reaching the app).*

---

## 5. Running the Tunnel as a Background Service

If you want the public tunnel to stay active automatically whenever your laptop is on:

### Running `cloudflared` in the background with `tmux` or `systemd`

Using a detached `tmux` or `screen` session:
```bash
tmux new -s public-tunnel "cloudflared tunnel --url http://localhost:5091"
```
Detach using `Ctrl + B`, then press `D`. The tunnel will continue running in the background.

To view the URL or status again:
```bash
tmux attach -t public-tunnel
```

---

## 6. Critical Considerations When Hosting on a Laptop

> [!WARNING]
> **Laptop Sleep & Power Management**
> By default, closing your laptop lid or leaving it idle will cause it to sleep or suspend. When the laptop sleeps, your Docker containers and tunnels immediately disconnect.
> - Configure system settings to **"Do nothing when lid is closed"** while plugged in.
> - On Linux (systemd), edit `/etc/systemd/logind.conf` to set:
>   ```ini
>   HandleLidSwitch=ignore
>   HandleLidSwitchExternalPower=ignore
>   ```
>   Then run `sudo systemctl restart systemd-logind`.

> [!CAUTION]
> **Database Security**
> In your `docker-compose.yml`, port `1521` (Oracle) is exposed to `0.0.0.0:1521`.
> - The reverse tunnel tools only expose port `5091` (the web application), so Oracle is **not** exposed to the outside internet through the tunnel.
> - Keep it this way: **never** point a tunnel or router port forward directly to port `1521`. Only the web app should talk to Oracle via Docker's internal network.

> [!IMPORTANT]
> **Dynamic URLs vs. Fixed URLs**
> Quick tunnels generate a new random URL each time you restart the command. If you want a permanent custom subdomain (e.g., `aldawah-pharma.yourdomain.com` or a persistent Cloudflare name) without paying:
> - You can register a free domain on providers like DuckDNS, or
> - Use a persistent Cloudflare Named Tunnel (requires a free Cloudflare account).

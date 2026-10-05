# Complete Hosting & Deployment Guide

This guide covers all production deployment strategies for the **Al-Dawah Pharmacy Inventory & Sales Management System**, ranging from a 1-command Docker setup to cloud VPS hosting with SSL and local pharmacy intranet installations.

---

## 🏗️ Architecture Overview

The system consists of three architectural components:

```
+-----------------------------------------------------------------------------------+
|                            PRODUCTION DEPLOYMENT TOPOLOGY                         |
+-----------------------------------------------------------------------------------+
|                                                                                   |
|  [ CLIENTS / CASHIERS ]                                                           |
|  Counter 1, Counter 2, Tablets, Mobile                                            |
|        │                                                                          |
|        ▼ (HTTPS / HTTP Port 5091 or 443)                                          |
|  [ NGINX REVERSE PROXY / KESTREL ]                                                |
|  Serves Frontend SPA static files (index.html, CSS, JS)                           |
|        │                                                                          |
|        ▼ (Direct in-process request)                                              |
|  [ ASP.NET CORE 10 WEB API ]                                                      |
|  Processes business logic, authentication, JWT tokens                             |
|        │                                                                          |
|        ▼ (TCP Port 1521)                                                          |
|  [ ORACLE DATABASE 23c / 26ai ]                                                   |
|  Stores 12 tables, compound triggers, stored procedures, audit logs               |
+-----------------------------------------------------------------------------------+
```

---

## 🚀 Option 1: Docker Compose (Recommended - 1 Command)

The fastest and most portable way to host the entire system on any Linux, Windows, or macOS server.

### Prerequisites:
- Docker and Docker Compose installed.

### Steps:
1. Clone the repository onto your server:
   ```bash
   git clone https://github.com/arif-z04/al-dawah-pharmacy-dashboard.git
   cd al-dawah-pharmacy-dashboard
   ```

2. Start the entire application and database stack in detached mode:
   ```bash
   docker compose up -d --build
   ```

3. Docker Compose will:
   - Start the **Oracle Database** container with health checks and persistent storage volumes (`oracle-data`).
   - Wait until Oracle logs `DATABASE IS READY TO USE!`.
   - Build the **ASP.NET Core Web API** and bundle the **Vanilla JS Frontend** into `wwwroot`.
   - Launch the web application listening on port `5091`.

4. Access the application in your browser:
   👉 **`http://<SERVER_IP>:5091`**

5. To view live logs:
   ```bash
   docker compose logs -f web-app
   ```

6. To stop the application:
   ```bash
   docker compose down
   ```

---

## ☁️ Option 2: Linux Cloud VPS Hosting (Ubuntu 22.04 / 24.04 with Nginx & SSL)

Use this method when hosting on cloud providers such as **DigitalOcean**, **AWS EC2**, **Hetzner**, **Linode**, or **Azure**.

### Step 1: Update Server and Install Prerequisites
```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y git curl nginx certbot python3-certbot-nginx
```

Install .NET 10 SDK/Runtime:
```bash
# Register Microsoft package repository
sudo apt-get install -y dotnet-sdk-10.0
```

### Step 2: Run Oracle Database in Docker
```bash
sudo apt install -y docker.io
sudo systemctl enable --now docker

sudo docker run -d \
  --name oracle \
  --restart unless-stopped \
  -p 1521:1521 \
  -e ORACLE_PASSWORD=YourStrongPassword2026# \
  -v oracle-data:/opt/oracle/oradata \
  container-registry.oracle.com/database/free:latest
```

Initialize the database schema:
```bash
sudo docker cp sql oracle:/tmp/sql
sudo docker exec oracle bash -c "sqlplus -S system/YourStrongPassword2026#@localhost:1521/FREE @/tmp/sql/01_schema.sql"
sudo docker exec oracle bash -c "sqlplus -S system/YourStrongPassword2026#@localhost:1521/FREE @/tmp/sql/02_constraints_indexes.sql"
sudo docker exec oracle bash -c "sqlplus -S system/YourStrongPassword2026#@localhost:1521/FREE @/tmp/sql/03_views.sql"
sudo docker exec oracle bash -c "sqlplus -S system/YourStrongPassword2026#@localhost:1521/FREE @/tmp/sql/04_functions.sql"
sudo docker exec oracle bash -c "sqlplus -S system/YourStrongPassword2026#@localhost:1521/FREE @/tmp/sql/05_procedures.sql"
sudo docker exec oracle bash -c "sqlplus -S system/YourStrongPassword2026#@localhost:1521/FREE @/tmp/sql/06_triggers.sql"
sudo docker exec oracle bash -c "sqlplus -S system/YourStrongPassword2026#@localhost:1521/FREE @/tmp/sql/07_roles_privileges.sql"
sudo docker exec oracle bash -c "sqlplus -S system/YourStrongPassword2026#@localhost:1521/FREE @/tmp/sql/08_seed_data.sql"
```

### Step 3: Publish the .NET Application
```bash
git clone https://github.com/arif-z04/al-dawah-pharmacy-dashboard.git /tmp/aldawah
cd /tmp/aldawah
dotnet publish src/Backend/Api/Api.csproj -c Release -o /var/www/aldawah
```

### Step 4: Configure Systemd Background Service
Create a systemd unit file to ensure the web server restarts automatically on reboots:
```bash
sudo nano /etc/systemd/system/aldawah.service
```

Paste the following configuration:
```ini
[Unit]
Description=Al-Dawah Pharma Web API and SPA Service
After=network.target

[Service]
WorkingDirectory=/var/www/aldawah
ExecStart=/usr/bin/dotnet /var/www/aldawah/Api.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=aldawah-pharma
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5091
Environment=ConnectionStrings__OracleDb=User Id=C##PHARMACY_APP;Password=PharmacyApp2026#;Data Source=localhost:1521/FREE;
Environment=Jwt__Key=ChangeThisToYourCustomHighEntropySecretKey2026!#

[Install]
WantedBy=multi-user.target
```

Enable and start the service:
```bash
sudo systemctl daemon-reload
sudo systemctl enable --now aldawah.service
sudo systemctl status aldawah.service
```

### Step 5: Configure Nginx as Reverse Proxy
Create an Nginx server block:
```bash
sudo nano /etc/nginx/sites-available/aldawah
```

Paste the following:
```nginx
server {
    listen 80;
    server_name pharmacy.yourdomain.com; # Replace with your domain or server IP

    client_max_body_size 20M;

    location / {
        proxy_pass http://127.0.0.1:5091;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Enable the site and restart Nginx:
```bash
sudo ln -s /etc/nginx/sites-available/aldawah /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

### Step 6: Enable Free HTTPS (SSL) with Let's Encrypt
```bash
sudo certbot --nginx -d pharmacy.yourdomain.com
```
Certbot will automatically install the SSL certificates and configure HTTP-to-HTTPS redirection.

---

## 🏢 Option 3: Local Pharmacy Intranet (LAN) Hosting (Offline Retail)

Most retail pharmacies operate inside a physical storefront and require **100% offline uptime**, meaning cashiers must be able to sell medicines even if the internet service provider is down.

```
                  +-----------------------------------+
                  |  STORE ROUTER / WI-FI (192.168.1.1)|
                  +-----------------+-----------------+
                                    |
          +-------------------------+-------------------------+
          |                                                   |
          ▼                                                   ▼
+-----------------------+                           +-------------------+
| MANAGER'S PC / SERVER |                           | CASHIER COUNTER 1 |
| IP: 192.168.1.100     |                           | Chrome Browser:   |
| Runs Oracle + .NET    |                           | 192.168.1.100:5091|
+-----------------------+                           +-------------------+
          |                                                   |
          +-------------------------+-------------------------+
                                    |
                                    ▼
                            +-------------------+
                            | CASHIER COUNTER 2 |
                            | Chrome Browser:   |
                            | 192.168.1.100:5091|
                            +-------------------+
```

### Steps for Local Store Hosting:
1. **Designate a Main PC**: Choose the most reliable PC in the pharmacy (Manager's computer or dedicated mini-PC server).
2. **Assign a Static Local IP**: In Windows Network Settings or your router's DHCP reservation, assign this PC a static IP (e.g., `192.168.1.100`).
3. **Run the Application**: Follow the [Windows Setup Guide (`documentation/setup-guide.md`)](file:///home/noir/Work/AL-Dawah_Pharma/documentation/setup-guide.md) to start Oracle and the .NET application.
4. **Allow Port 5091 Through Windows Firewall**:
   Run PowerShell as Administrator:
   ```powershell
   New-NetFirewallRule -DisplayName "Al-Dawah Pharma Kiosk" -Direction Inbound -LocalPort 5091 -Protocol TCP -Action Allow
   ```
5. **Connect Cashier Registers**:
   On all other computers, tablets, or phones connected to the store Wi-Fi, open Google Chrome and navigate to:
   👉 **`http://192.168.1.100:5091`**
   All counters can now process sales simultaneously with real-time stock synchronization and zero internet dependency.

---

## 🔒 Production Hardening Checklist

Before going live with real patient and inventory data, verify these four essential security practices:

1. **Change Default Credentials**:
   - Change the `admin` password immediately after first login via the **Profile / Change Password** screen.
   - Do not use `admin123` or `PharmacyApp2026#` in production.
2. **Generate a Unique JWT Secret Key**:
   - In production, set `Jwt__Key` in environment variables to a cryptographically random 64-character string:
     ```bash
     openssl rand -base64 48
     ```
3. **Configure Daily Automated Oracle Backups**:
   Schedule an automated backup using `expdp` via cron:
   ```bash
   # Add to crontab (crontab -e) to run every night at 2:00 AM:
   0 2 * * * docker exec oracle expdp system/YourStrongPassword2026#@localhost:1521/FREE schemas=SYSTEM directory=DATA_PUMP_DIR dumpfile=backup_%date%.dmp
   ```
4. **Firewall Isolation**:
   - Only expose port `80` (HTTP) and `443` (HTTPS) to the public.
   - **Never expose Oracle Port 1521 to the public internet.** Ensure port 1521 is restricted to `localhost` or your private docker network.

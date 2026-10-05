# Comprehensive Windows Setup & Deployment Guide

> [!NOTE]
> This is the official Windows installation and deployment guide for the **Al-Dawah Pharmacy Inventory & Sales Management System**.
> For deep architectural details and beginners' concepts, refer to [documentation/README.md](file:///home/noir/Work/AL-Dawah_Pharma/documentation/README.md).

---

## 1. System Requirements & Prerequisites

Before starting, ensure your Windows PC meets the following minimum specifications:

| Component | Minimum Specification | Recommended Specification |
| :--- | :--- | :--- |
| **Operating System** | Windows 10 (64-bit) Home/Pro Build 19041+ or Windows 11 | Windows 11 Pro 64-bit |
| **CPU** | 64-bit Intel or AMD processor with Virtualization support | 4+ physical cores |
| **RAM** | 8 GB RAM | 16 GB RAM (ensures smooth Oracle DB execution) |
| **Free Disk Space** | 15 GB available SSD storage | 30 GB available SSD storage |
| **Permissions** | Local Administrator access on your Windows user account | Administrator |

---

## 2. Step 1: Enable Hardware Virtualization in Windows

Oracle Database runs inside an enterprise Linux container. To run containers on Windows, hardware virtualization must be enabled in your computer's BIOS/UEFI.

### How to Check if Virtualization Is Enabled:
1. Press `Ctrl + Shift + Esc` on your keyboard to open **Task Manager**.
2. Click on the **Performance** tab on the left sidebar.
3. Click on **CPU**.
4. In the bottom right corner, look for **Virtualization**:
   - If it says **Enabled**, you are ready!
   - If it says **Disabled**, restart your PC, enter your BIOS/UEFI settings (typically by pressing `F2`, `F10`, `F12`, or `Del` during boot), navigate to the **Advanced / CPU Configuration** menu, enable **Intel VT-x** or **AMD-V / SVM**, save settings, and reboot.

---

## 3. Step 2: Install Required Windows Tools

We will use Windows Package Manager (**winget**), which comes built into Windows 10 and 11.

Open **PowerShell as Administrator** (Right-click the Windows Start menu icon -> Select **Terminal (Admin)** or **Windows PowerShell (Admin)**) and execute the following commands:

### 1. Install Git for Windows
```powershell
winget install --id Git.Git -e --source winget
```
*Why*: Allows you to clone and manage the project source code.

### 2. Install .NET 10 (or .NET 8) SDK
```powershell
winget install Microsoft.DotNet.SDK.10 -e --source winget
```
*Verify*: Close and re-open PowerShell, then run:
```powershell
dotnet --version
```
You should see a version string starting with `10.0.` (or `8.0.`).

### 3. Install Docker Desktop for Windows
```powershell
winget install Docker.DockerDesktop -e --source winget
```
1. Once installation completes, **restart your computer**.
2. After rebooting, launch **Docker Desktop** from the Start menu.
3. If prompted to install the **WSL 2 Linux kernel update package**, click the download link provided by Docker or run in PowerShell:
   ```powershell
   wsl --update
   wsl --set-default-version 2
   ```
4. In Docker Desktop, navigate to **Settings (Gear Icon) -> General** and verify that **"Use the WSL 2 based engine"** is checked.
5. Wait until the status icon in the bottom-left corner of Docker Desktop turns **Green** (Engine running).

---

## 4. Step 3: Run Oracle Database Free on Windows

Oracle Database 23c / 26ai Free is the official database engine. We will launch it inside Docker with a single command.

Open PowerShell and run:
```powershell
docker run -d `
  --name oracle `
  -p 1521:1521 `
  -e ORACLE_PASSWORD=SecretPassword2026# `
  container-registry.oracle.com/database/free:latest
```

### Explaining the Flags:
- `-d`: Runs the database in the background (detached mode).
- `--name oracle`: Gives the container an easy-to-remember name.
- `-p 1521:1521`: Maps port 1521 inside the container to port 1521 on your Windows machine.
- `-e ORACLE_PASSWORD=SecretPassword2026#`: Sets the master password for the administrative user (`SYSTEM`).

### Monitoring Container Initialization
Oracle is an enterprise database that configures its data files during first boot. Run:
```powershell
docker logs -f oracle
```
Watch the output until you see:
```text
#########################
DATABASE IS READY TO USE!
#########################
```
*(This initial boot process typically takes 1 to 3 minutes on modern SSDs. Once you see this message, press `Ctrl + C` to exit the log viewer).*

---

## 5. Step 4: Clone the Codebase and Configure Line Endings

In PowerShell, clone the repository to your development directory:

```powershell
cd C:\Users\$env:USERNAME\Documents
git clone https://github.com/your-username/AL-Dawah_Pharma.git
cd AL-Dawah_Pharma
```

### Important: Windows Line Endings (CRLF vs LF)
Windows uses Carriage Return + Line Feed (`\r\n`), while Linux and Docker containers expect Line Feed (`\n`). To prevent script syntax errors when running SQL files inside the container, configure Git:
```powershell
git config core.autocrlf input
```

---

## 6. Step 5: Initialize the Oracle Database Schema

We will copy the SQL scripts from the `/sql` directory into the Oracle container and run them in their exact dependency order.

Run the following commands in PowerShell from the project root:

```powershell
# Copy SQL scripts into the running container
docker cp sql oracle:/tmp/sql

# Execute SQL scripts sequentially using sqlplus inside the container
docker exec -it oracle bash -c "sqlplus system/SecretPassword2026#@localhost:1521/FREE @/tmp/sql/01_schema.sql"
docker exec -it oracle bash -c "sqlplus system/SecretPassword2026#@localhost:1521/FREE @/tmp/sql/02_constraints_indexes.sql"
docker exec -it oracle bash -c "sqlplus system/SecretPassword2026#@localhost:1521/FREE @/tmp/sql/03_views.sql"
docker exec -it oracle bash -c "sqlplus system/SecretPassword2026#@localhost:1521/FREE @/tmp/sql/04_triggers.sql"
docker exec -it oracle bash -c "sqlplus system/SecretPassword2026#@localhost:1521/FREE @/tmp/sql/05_procedures.sql"
docker exec -it oracle bash -c "sqlplus system/SecretPassword2026#@localhost:1521/FREE @/tmp/sql/06_functions.sql"
docker exec -it oracle bash -c "sqlplus system/SecretPassword2026#@localhost:1521/FREE @/tmp/sql/07_roles_privileges.sql"
docker exec -it oracle bash -c "sqlplus system/SecretPassword2026#@localhost:1521/FREE @/tmp/sql/08_seed_data.sql"
docker exec -it oracle bash -c "sqlplus system/SecretPassword2026#@localhost:1521/FREE @/tmp/sql/10_verification.sql"
```

### Automated Single-File PowerShell Script: `setup-database.ps1`
You can save the following script as `setup-database.ps1` in the project root:

```powershell
Write-Host ">>> Transferring SQL migration scripts to Oracle container..." -ForegroundColor Cyan
docker cp sql oracle:/tmp/sql

$scripts = @(
    "01_schema.sql",
    "02_constraints_indexes.sql",
    "03_views.sql",
    "04_triggers.sql",
    "05_procedures.sql",
    "06_functions.sql",
    "07_roles_privileges.sql",
    "08_seed_data.sql",
    "10_verification.sql"
)

foreach ($script in $scripts) {
    Write-Host ">>> Executing $script..." -ForegroundColor Yellow
    docker exec oracle bash -c "sqlplus -S system/SecretPassword2026#@localhost:1521/FREE @/tmp/sql/$script"
}

Write-Host ">>> Database provisioning complete and verified!" -ForegroundColor Green
```
To run it:
```powershell
powershell -ExecutionPolicy Bypass -File .\setup-database.ps1
```

---

## 7. Step 6: Verify Backend Database Connection Settings

Open `src/Backend/Api/appsettings.json` in VS Code or Notepad:

```json
{
  "ConnectionStrings": {
    "OracleDb": "Data Source=localhost:1521/FREE;User Id=C##PHARMACY_APP;Password=PharmacyApp2026#;"
  },
  "Jwt": {
    "Key": "AlDawahPharmaSecretKeySuperSecure2026!#*",
    "Issuer": "AlDawahPharmaApi",
    "Audience": "AlDawahPharmaClient"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```
*Notice: The backend connects as `C##PHARMACY_APP` (the least-privilege application user configured in `07_roles_privileges.sql`), ensuring strict security.*

---

## 8. Step 7: Build and Run the Application

In your PowerShell window, navigate to the API project directory and run the server:

```powershell
cd src\Backend\Api
dotnet build
dotnet run --launch-profile http
```

You will see output similar to:
```text
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5091
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
```

---

## 9. Step 8: Access the Application

Open your favorite web browser (Google Chrome, Microsoft Edge, Firefox, Brave) and navigate to:

👉 **`http://localhost:5091`**

### Pre-Configured Demo Accounts:
| Role | Username | Password | Access Level |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin` | `admin123` | Full access: User management, reports, inventory adjustments, POS. |
| **Pharmacist / Staff** | `pharmacist` | `pharma123` | Operational access: Point of sale, purchase entries, stock view. |

---

## 10. Step 9: Running Automated Tests on Windows

To run the automated xUnit unit and integration test suite:

In a new PowerShell window:
```powershell
cd AL-Dawah_Pharma
dotnet test
```

Expected output:
```text
Passed!  - Failed: 0, Passed: 9, Skipped: 0, Total: 9 - AlDawahPharma.Tests.dll
```

---

## 11. Windows Troubleshooting & Common Gotchas

### Issue 1: PowerShell Script Execution Policy Error
**Symptom**: `File ... cannot be loaded because running scripts is disabled on this system.`
**Solution**:
Run this command in PowerShell to temporarily allow scripts in your current session:
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
```

### Issue 2: Port 1521 Collision (Address Already In Use)
**Symptom**: Docker fails to bind port 1521, or another service occupies port 1521.
**Diagnosis**:
In PowerShell, check what process is using port 1521:
```powershell
Get-NetTCPConnection -LocalPort 1521 -ErrorAction SilentlyContinue | Format-Table -AutoSize
```
If an existing native Oracle or SQL Developer service is running, either stop it from `services.msc` or map Docker to port `1522`:
```powershell
docker run -d --name oracle -p 1522:1521 -e ORACLE_PASSWORD=SecretPassword2026# container-registry.oracle.com/database/free:latest
```
*(If using port 1522, update `Data Source=localhost:1522/FREE` in `appsettings.json`).*

### Issue 3: Windows Defender Firewall Prompt
**Symptom**: A Windows Defender popup asks whether to allow `dotnet.exe` or `Docker Desktop` to communicate on Private and Public networks.
**Solution**: Check **"Private networks, such as my home or work network"** and click **Allow Access**.

### Issue 4: Docker Desktop WSL2 Memory Consumption
**Symptom**: Windows feels sluggish after running Docker for several hours.
**Solution**: Limit WSL2 memory allocation:
1. Press `Win + R`, type `%USERPROFILE%`, and press Enter.
2. Create or edit a file named `.wslconfig`:
   ```ini
   [wsl2]
   memory=4GB
   processors=2
   ```
3. Restart WSL in PowerShell: `wsl --shutdown`.

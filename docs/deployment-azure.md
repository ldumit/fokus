# Fokus — Azure Deployment

Single Azure App Service (Linux) hosting the .NET API + Vue SPA. No containers — uses the built-in DOTNETCORE runtime. SQLite persisted on `/home/data/`.

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) installed and logged in (`az login`)
- Node.js + npm (for frontend build)
- .NET 10 SDK
- Google OAuth credentials (Client ID + Client Secret) registered at [Google Cloud Console](https://console.cloud.google.com/)

## 1. Azure Login

```powershell
# Authenticate with Azure (use Cloud Shell if MFA blocks local CLI)
az login
```

## 2. Set Subscription (if multiple subscriptions)

```powershell
# List all subscriptions to find the right one
az account list --output table

# Set the active subscription
az account set --subscription "<subscription name or ID>"
```

## 3. Create Azure Resources

```powershell
# Create resource group in Poland Central region
az group create --name <your-resource-group> --location polandcentral

# Create App Service plan (F1 = free, B1 = ~$13/mo with Always On)
az appservice plan create --name <your-plan-name> --resource-group <your-resource-group> --sku F1 --is-linux

# Create web app with .NET 10 runtime (name must be globally unique)
az webapp create --name <your-app-name> --resource-group <your-resource-group> --plan <your-plan-name> --runtime "DOTNETCORE:10.0"
```

## 4. Configure the App

```powershell
# Enable WebSockets (required for SignalR)
az webapp config set --name <your-app-name> --resource-group <your-resource-group> --web-sockets-enabled true

# Set startup command to run the API DLL
az webapp config set --name <your-app-name> --resource-group <your-resource-group> --startup-file "dotnet Fokus.API.dll"

# Disable Kudu build (we publish pre-built artifacts)
az webapp config appsettings set --name <your-app-name> --resource-group <your-resource-group> --settings SCM_DO_BUILD_DURING_DEPLOYMENT=false
```

## 5. Set Connection String

```powershell
# SQLite on /home/data/ — persists across redeploys (Azure mounts /home/ as persistent storage)
az webapp config connection-string set --name <your-app-name> --resource-group <your-resource-group> --connection-string-type Custom --settings Database="Data Source=/home/data/fokus.db"
```

## 6. Set Secrets

```powershell
# Google OAuth + Jira credentials (ASP.NET Core reads __ as nested config separator)
az webapp config appsettings set --name <your-app-name> --resource-group <your-resource-group> --settings \
  Google__ClientId="<Google Cloud Console OAuth Client ID>" \
  Google__ClientSecret="<Google Cloud Console OAuth Client Secret>" \
  Jira__InstanceUrl="<https://your-org.atlassian.net>" \
  Jira__Email="<Jira account email>" \
  Jira__ApiToken="<Jira API token>"
```

## 7. Google OAuth Setup

In [Google Cloud Console](https://console.cloud.google.com/) → Credentials → your OAuth 2.0 Client ID, add:

- **Authorized JavaScript origins:** `https://<your-app-name>.azurewebsites.net`
- **Authorized redirect URIs:** `https://<your-app-name>.azurewebsites.net/auth/callback`

## 8. Build & Deploy

From repo root:

```powershell
.\deploy.ps1
```

This builds the frontend, publishes the API, zips the output, and deploys to Azure.

If `az login` doesn't work locally (MFA issues), build locally and deploy via Cloud Shell:

```powershell
# 1. Run deploy.ps1 locally — it will fail at az webapp deploy but produces the zip
.\deploy.ps1

# 2. Upload src\Services\Fokus\Fokus.API\deploy.zip to Cloud Shell (Manage files → Upload)

# 3. In Cloud Shell, deploy the zip
az webapp deploy --name <your-app-name> --resource-group <your-resource-group> --src-path deploy.zip --type zip --clean true
```

**Important:** Use `tar -a -cf deploy.zip *` (not PowerShell's `Compress-Archive`) — `Compress-Archive` creates entries with Windows backslashes that Azure Linux cannot resolve.

## 9. Verify

Open `https://<your-app-name>.azurewebsites.net` in a browser.

Check logs if something is wrong:

```powershell
# Enable filesystem logging
az webapp log config --name <your-app-name> --resource-group <your-resource-group> --docker-container-logging filesystem

# Stream live logs (Ctrl+C to stop)
az webapp log tail --name <your-app-name> --resource-group <your-resource-group>

# Filter to warnings/errors only
az webapp log config --name <your-app-name> --resource-group <your-resource-group> --application-logging filesystem --level warning
```

## Redeployment

Run `.\deploy.ps1` from repo root (or Cloud Shell upload if no local `az login`). No config changes needed — app settings and connection strings persist between deploys.

## Useful Commands

```powershell
# Restart the app
az webapp restart --name <your-app-name> --resource-group <your-resource-group>

# View app settings
az webapp config appsettings list --name <your-app-name> --resource-group <your-resource-group>

# View connection strings
az webapp config connection-string list --name <your-app-name> --resource-group <your-resource-group>

# Stream live logs
az webapp log tail --name <your-app-name> --resource-group <your-resource-group>
```

## Plan Switching

```powershell
# Upgrade to B1 (~$13/mo) for always-on, custom domains, SSL
az appservice plan update --name <your-plan-name> --resource-group <your-resource-group> --sku B1

# Downgrade back to free tier
az appservice plan update --name <your-plan-name> --resource-group <your-resource-group> --sku F1
```

B1 benefits: Always On (no cold starts), custom domains + SSL, no 60 CPU min/day limit.

**Tip:** Upgrade to B1 temporarily for a deploy or demo, then downgrade. At ~$0.018/hr, a few hours costs less than $0.10.

## Known Limitations (F1 Free Tier)

- **60 CPU min/day** — shared compute time, resets at midnight UTC
- **No Always On** — app sleeps after ~20 min idle, cold start takes 5-15s
- **No custom domain or SSL** — only `*.azurewebsites.net` with Microsoft cert
- **SQLite on `/home/`** — persists between restarts but treat as losable

## Architecture

- Vue SPA builds into `Fokus.API/wwwroot/` and is served as static files from the same origin
- SQLite DB at `/home/data/fokus.db` — survives redeploys, does not survive plan deletion
- WebSockets enabled for SignalR
- `SCM_DO_BUILD_DURING_DEPLOYMENT=false` — artifacts are pre-built locally, Kudu build skipped
- App ensures `/home/data/` directory exists at startup before running migrations

# WalkMap — Azure Deployment Guide

## Architecture

```
GitHub Actions (CI/CD)
    │
    ├── Backend  → Azure App Service (Linux, .NET 8)
    │                └── Azure SQL Database
    └── Frontend → Azure Static Web Apps (Blazor WASM)
```

---

## Step 1 — Create Azure Resources

Run these in Azure CLI (or use the Portal):

```bash
# Variables — change these
RESOURCE_GROUP="walkmap-rg"
LOCATION="eastus"
APP_SERVICE_PLAN="walkmap-plan"
BACKEND_APP="walkmap-api"           # must be globally unique
FRONTEND_APP="walkmap-frontend"     # must be globally unique
SQL_SERVER="walkmap-sql"            # must be globally unique
SQL_DB="walkmap-db"
SQL_ADMIN="walkmapAdmin"
SQL_PASSWORD="YourStr0ngP@ssword!"

# Resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# App Service Plan (Linux B1 = ~$13/month)
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku B1 --is-linux

# Backend App Service
az webapp create \
  --name $BACKEND_APP \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNETCORE:8.0"

# Azure SQL Server + Database
az sql server create \
  --name $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user $SQL_ADMIN \
  --admin-password "$SQL_PASSWORD"

az sql db create \
  --name $SQL_DB \
  --server $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --edition Basic

# Allow Azure services to reach SQL
az sql server firewall-rule create \
  --name AllowAzureServices \
  --server $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Static Web App for frontend
az staticwebapp create \
  --name $FRONTEND_APP \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION
```

---

## Step 2 — Configure Backend App Settings

Replace all `YOUR_*` placeholders then run:

```bash
CONN_STRING="Server=tcp:${SQL_SERVER}.database.windows.net,1433;\
Initial Catalog=${SQL_DB};Persist Security Info=False;\
User ID=${SQL_ADMIN};Password=${SQL_PASSWORD};\
Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

FRONTEND_URL="https://${FRONTEND_APP}.azurestaticapps.net"

az webapp config appsettings set \
  --name $BACKEND_APP \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "ConnectionStrings__DefaultConnection=${CONN_STRING}" \
    "Jwt__Key=REPLACE_WITH_A_RANDOM_64_CHAR_STRING" \
    "Jwt__Issuer=WalkMapApi" \
    "Jwt__Audience=WalkMapClients" \
    "OpenRouteService__ApiKey=YOUR_ORS_API_KEY" \
    "AllowedOrigins__0=${FRONTEND_URL}"
```

> **Tip:** Generate a strong JWT key with:
> ```bash
> openssl rand -base64 48
> ```

---

## Step 3 — Add GitHub Secrets

In your GitHub repo → Settings → Secrets and variables → Actions, add:

| Secret name | Value |
|---|---|
| `AZURE_BACKEND_PUBLISH_PROFILE` | Download from App Service → "Get publish profile" |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | From Static Web App → "Manage deployment token" |

---

## Step 4 — Update Workflow Variables

In `.github/workflows/azure-deploy.yml`, set the `env` block:

```yaml
env:
  BACKEND_APP_NAME: walkmap-api        # your App Service name
  FRONTEND_APP_NAME: walkmap-frontend  # your Static Web App name
```

---

## Step 5 — Push & Deploy

```bash
git add .
git commit -m "Add Azure deployment config"
git push origin main
```

GitHub Actions will build and deploy both apps automatically.

---

## Step 6 — Verify

| Check | URL |
|---|---|
| API health | `https://walkmap-api.azurewebsites.net/swagger` |
| Frontend | `https://walkmap-frontend.azurestaticapps.net` |

---

## Required Secrets / Keys

| Key | Where to get it |
|---|---|
| ORS API key | https://openrouteservice.org → Sign up (free tier) |
| JWT secret | Generate locally: `openssl rand -base64 48` |
| SQL password | You set this in Step 1 |

---

## Cost Estimate (Basic tier)

| Resource | Monthly cost |
|---|---|
| App Service B1 | ~$13 |
| Azure SQL Basic | ~$5 |
| Static Web Apps | Free |
| **Total** | **~$18/month** |

# =============================================================
#  Azure Provisioning Script — Gilde Van De Stroppendragers
#  Run this ONCE in Azure Cloud Shell or local Azure CLI
#  Prerequisites: az login  (already logged in)
# =============================================================

# ?? Configuration ?????????????????????????????????????????????
$RESOURCE_GROUP   = "rg-gilde-prod"
$LOCATION         = "westeurope"
$SQL_SERVER_NAME  = "sql-gilde-prod"
$SQL_DB_NAME      = "db-gilde"
$SQL_ADMIN_USER   = "gilde-sqladmin"
$SQL_ADMIN_PASS   = "Gx7#mPqL2!vRnZ4w"        # ? auto-generated, store safely
$APP_PLAN_NAME    = "asp-gilde-prod"
$WEBAPP_NAME      = "app-gilde-prod"
$DOTNET_VERSION   = "8.0"

# ?? 1. Resource Group ?????????????????????????????????????????
Write-Host "Creating Resource Group..." -ForegroundColor Cyan
az group create `
  --name $RESOURCE_GROUP `
  --location $LOCATION

# ?? 2. Azure SQL Server ???????????????????????????????????????
Write-Host "Creating SQL Server..." -ForegroundColor Cyan
az sql server create `
  --name $SQL_SERVER_NAME `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION `
  --admin-user $SQL_ADMIN_USER `
  --admin-password $SQL_ADMIN_PASS

# Allow Azure services to reach the SQL server
az sql server firewall-rule create `
  --resource-group $RESOURCE_GROUP `
  --server $SQL_SERVER_NAME `
  --name "AllowAzureServices" `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0

# ?? 3. Azure SQL Database (Basic = cheapest) ??????????????????
Write-Host "Creating SQL Database..." -ForegroundColor Cyan
az sql db create `
  --resource-group $RESOURCE_GROUP `
  --server $SQL_SERVER_NAME `
  --name $SQL_DB_NAME `
  --service-objective Basic `
  --backup-storage-redundancy Local

# ?? 4. App Service Plan (B1 = cheapest Always-On) ?????????????
Write-Host "Creating App Service Plan (B1)..." -ForegroundColor Cyan
az appservice plan create `
  --name $APP_PLAN_NAME `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION `
  --sku B1 `
  --is-linux

# ?? 5. Web App ????????????????????????????????????????????????
Write-Host "Creating Web App..." -ForegroundColor Cyan
az webapp create `
  --name $WEBAPP_NAME `
  --resource-group $RESOURCE_GROUP `
  --plan $APP_PLAN_NAME `
  --runtime "DOTNETCORE:8.0"

# ?? 6. App Settings (connection string + environment) ?????????
Write-Host "Configuring App Settings..." -ForegroundColor Cyan

$CONNECTION_STRING = "Server=tcp:$SQL_SERVER_NAME.database.windows.net,1433;" + `
  "Initial Catalog=$SQL_DB_NAME;" + `
  "Persist Security Info=False;" + `
  "User ID=$SQL_ADMIN_USER;" + `
  "Password=$SQL_ADMIN_PASS;" + `
  "MultipleActiveResultSets=False;" + `
  "Encrypt=True;" + `
  "TrustServerCertificate=False;" + `
  "Connection Timeout=30;"

az webapp config connection-string set `
  --name $WEBAPP_NAME `
  --resource-group $RESOURCE_GROUP `
  --settings DefaultConnection="$CONNECTION_STRING" `
  --connection-string-type SQLAzure

az webapp config appsettings set `
  --name $WEBAPP_NAME `
  --resource-group $RESOURCE_GROUP `
  --settings ASPNETCORE_ENVIRONMENT="Production"

# ?? 7. HTTPS Only ?????????????????????????????????????????????
Write-Host "Enforcing HTTPS..." -ForegroundColor Cyan
az webapp update `
  --name $WEBAPP_NAME `
  --resource-group $RESOURCE_GROUP `
  --https-only true

# ?? 8. Always On (keeps app warm, no cold-start session loss) ??
az webapp config set `
  --name $WEBAPP_NAME `
  --resource-group $RESOURCE_GROUP `
  --always-on true

# ?? 9. Print Publish Profile (paste into GitHub Secret) ???????
Write-Host ""
Write-Host "======================================================" -ForegroundColor Green
Write-Host " DONE! Next step: add GitHub Secret" -ForegroundColor Green
Write-Host "======================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Run this to get your publish profile XML:" -ForegroundColor Yellow
Write-Host "  az webapp deployment list-publishing-profiles --name $WEBAPP_NAME --resource-group $RESOURCE_GROUP --xml" -ForegroundColor White
Write-Host ""
Write-Host "Copy the output and add it as a GitHub Secret named:" -ForegroundColor Yellow
Write-Host "  AZURE_WEBAPP_PUBLISH_PROFILE" -ForegroundColor White
Write-Host ""
Write-Host "Your app will be live at:" -ForegroundColor Yellow
Write-Host "  https://$WEBAPP_NAME.azurewebsites.net" -ForegroundColor White

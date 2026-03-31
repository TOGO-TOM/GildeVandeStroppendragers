#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Provisions all Azure resources required to host the AdminMembers (GildeVanDeStroppendragers) application.

.DESCRIPTION
    Creates the following Azure resources:
      - Resource Group
      - App Service Plan (Linux, B1)
      - App Service (.NET 8) with System-Assigned Managed Identity
      - Azure SQL Server + Database
      - Azure Storage Account + Blob containers (backups, exports, logos)
      - Role assignment: Storage Blob Data Contributor for the App Service identity
      - App Service configuration (connection string + app settings)

    The script registers all required Azure resource providers before provisioning,
    checks every step for errors, and stops immediately if a step fails.

.PARAMETER ResourceGroup
    Name of the Azure Resource Group to create or reuse. Default: rg-gilde-prod

.PARAMETER Location
    Azure region. Default: westeurope

.PARAMETER AppName
    Azure App Service name (must be globally unique). Default: app-gilde-prod

.PARAMETER SqlAdminPassword
    Password for the Azure SQL administrator login. Required.

.EXAMPLE
    .\provision.ps1 -SqlAdminPassword "MyS3cureP@ss!"
    .\provision.ps1 -ResourceGroup rg-gilde-dev -AppName app-gilde-dev -SqlAdminPassword "MyS3cureP@ss!"
#>

param(
    [string]$ResourceGroup   = "rg-gilde-prod",
    [string]$Location        = "westeurope",
    [string]$AppName         = "app-gilde-prod",
    [string]$PlanName        = "plan-gilde-prod",
    [string]$SqlServerName   = "sql-gilde-prod",
    [string]$SqlDbName       = "AdminMembersDb",
    [string]$SqlAdminUser    = "sqladmin",
    [Parameter(Mandatory = $true)]
    [string]$SqlAdminPassword,
    [string]$StorageAccount  = "stgildeprod"
)

# Stop on any unhandled error
$ErrorActionPreference = "Stop"

# Helper: run an az command, capture stderr, and abort with a clear message on failure.
# Always returns a plain [string] so callers can safely call .Trim() on the result.
function Invoke-Az {
    param([string[]]$Arguments)
    $raw = & az @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        $msg = $raw | ForEach-Object { "$_" } | Out-String
        Write-Host ""
        Write-Host "  ERROR running: az $($Arguments -join ' ')" -ForegroundColor Red
        Write-Host $msg -ForegroundColor Red
        throw "Azure CLI command failed (exit $LASTEXITCODE). See error above."
    }
    # Join all output lines into a single string so .Trim() always works
    return ($raw | ForEach-Object { "$_" }) -join "`n"
}

# ?????????????????????????????????????????????
# 0. Verify Azure CLI is logged in + lock subscription
# ?????????????????????????????????????????????
Write-Host "`n[0/9] Checking Azure CLI login..." -ForegroundColor Cyan
$accountJson = Invoke-Az @("account", "show")
$account = $accountJson | ConvertFrom-Json
$subscriptionId   = $account.id
$subscriptionName = $account.name
Write-Host "      Subscription : $subscriptionName" -ForegroundColor Green
Write-Host "      ID           : $subscriptionId"   -ForegroundColor Green

# Pin every subsequent az call to this subscription so a context switch cannot break later steps
Invoke-Az @("account", "set", "--subscription", $subscriptionId) | Out-Null

# ?????????????????????????????????????????????
# 1. Register required resource providers
#    (new subscriptions often have these unregistered)
# ?????????????????????????????????????????????
Write-Host "`n[1/9] Registering required Azure resource providers..." -ForegroundColor Cyan
$providers = @("Microsoft.Sql", "Microsoft.Web", "Microsoft.Storage")
foreach ($provider in $providers) {
    Write-Host "      Registering $provider ..." -ForegroundColor Gray
    Invoke-Az @("provider", "register", "--namespace", $provider, "--wait") | Out-Null
    Write-Host "      $provider registered." -ForegroundColor Green
}

# ?????????????????????????????????????????????
# 2. Resource Group
# ?????????????????????????????????????????????
Write-Host "`n[2/9] Creating Resource Group '$ResourceGroup' in '$Location'..." -ForegroundColor Cyan
Invoke-Az @("group", "create", "--name", $ResourceGroup, "--location", $Location, "--output", "none") | Out-Null
Write-Host "      Done." -ForegroundColor Green

# ?????????????????????????????????????????????
# 3. App Service Plan + Web App
# ?????????????????????????????????????????????
Write-Host "`n[3/9] Creating App Service Plan '$PlanName' (Linux B1)..." -ForegroundColor Cyan
Invoke-Az @("appservice", "plan", "create",
    "--name", $PlanName,
    "--resource-group", $ResourceGroup,
    "--sku", "B1",
    "--is-linux",
    "--output", "none") | Out-Null
Write-Host "      Done." -ForegroundColor Green

Write-Host "      Creating Web App '$AppName' (.NET 8)..." -ForegroundColor Cyan
Invoke-Az @("webapp", "create",
    "--name", $AppName,
    "--resource-group", $ResourceGroup,
    "--plan", $PlanName,
    "--runtime", "DOTNETCORE:8.0",
    "--output", "none") | Out-Null
Write-Host "      Done." -ForegroundColor Green

# ?????????????????????????????????????????????
# 4. System-Assigned Managed Identity
# ?????????????????????????????????????????????
Write-Host "`n[4/9] Enabling System-Assigned Managed Identity on '$AppName'..." -ForegroundColor Cyan
$identityJson = Invoke-Az @("webapp", "identity", "assign",
    "--name", $AppName,
    "--resource-group", $ResourceGroup) | ConvertFrom-Json
$principalId = $identityJson.principalId
Write-Host "      Principal ID: $principalId" -ForegroundColor Green

# ?????????????????????????????????????????????
# 5. Azure SQL Server + Database
# ?????????????????????????????????????????????
Write-Host "`n[5/9] Creating Azure SQL Server '$SqlServerName'..." -ForegroundColor Cyan
Invoke-Az @("sql", "server", "create",
    "--name", $SqlServerName,
    "--resource-group", $ResourceGroup,
    "--location", $Location,
    "--admin-user", $SqlAdminUser,
    "--admin-password", $SqlAdminPassword,
    "--output", "none") | Out-Null
Write-Host "      SQL Server created." -ForegroundColor Green

Write-Host "      Opening firewall for Azure services..." -ForegroundColor Cyan
Invoke-Az @("sql", "server", "firewall-rule", "create",
    "--server", $SqlServerName,
    "--resource-group", $ResourceGroup,
    "--name", "AllowAzureServices",
    "--start-ip-address", "0.0.0.0",
    "--end-ip-address", "0.0.0.0",
    "--output", "none") | Out-Null
Write-Host "      Firewall rule created." -ForegroundColor Green

Write-Host "      Creating SQL Database '$SqlDbName' (Basic)..." -ForegroundColor Cyan
Invoke-Az @("sql", "db", "create",
    "--name", $SqlDbName,
    "--server", $SqlServerName,
    "--resource-group", $ResourceGroup,
    "--service-objective", "Basic",
    "--backup-storage-redundancy", "Local",
    "--output", "none") | Out-Null
Write-Host "      Database created." -ForegroundColor Green

# ?????????????????????????????????????????????
# 6. Azure Storage Account + Blob Containers
# ?????????????????????????????????????????????
Write-Host "`n[6/9] Creating Storage Account '$StorageAccount'..." -ForegroundColor Cyan
Invoke-Az @("storage", "account", "create",
    "--name", $StorageAccount,
    "--resource-group", $ResourceGroup,
    "--location", $Location,
    "--sku", "Standard_LRS",
    "--kind", "StorageV2",
    "--output", "none") | Out-Null
Write-Host "      Storage Account created." -ForegroundColor Green

$blobEndpoint = (Invoke-Az @("storage", "account", "show",
    "--name", $StorageAccount,
    "--resource-group", $ResourceGroup,
    "--query", "primaryEndpoints.blob",
    "--output", "tsv")).Trim()
Write-Host "      Blob endpoint: $blobEndpoint" -ForegroundColor Green

Write-Host "      Creating blob containers (backups, exports, logos)..." -ForegroundColor Cyan
$storageKey = (Invoke-Az @("storage", "account", "keys", "list",
    "--account-name", $StorageAccount,
    "--resource-group", $ResourceGroup,
    "--query", "[0].value",
    "--output", "tsv")).Trim()

foreach ($container in @("backups", "exports", "logos")) {
    Invoke-Az @("storage", "container", "create",
        "--name", $container,
        "--account-name", $StorageAccount,
        "--account-key", $storageKey,
        "--output", "none") | Out-Null
    Write-Host "        Container '$container' created." -ForegroundColor Green
}

# ?????????????????????????????????????????????
# 7. Grant Managed Identity: Storage Blob Data Contributor
# ?????????????????????????????????????????????
Write-Host "`n[7/9] Granting 'Storage Blob Data Contributor' to App Service identity..." -ForegroundColor Cyan
$storageId = (Invoke-Az @("storage", "account", "show",
    "--name", $StorageAccount,
    "--resource-group", $ResourceGroup,
    "--query", "id",
    "--output", "tsv")).Trim()

Invoke-Az @("role", "assignment", "create",
    "--assignee", $principalId,
    "--role", "Storage Blob Data Contributor",
    "--scope", $storageId,
    "--output", "none") | Out-Null
Write-Host "      Done." -ForegroundColor Green

# ?????????????????????????????????????????????
# 8. Configure App Service Settings
# ?????????????????????????????????????????????
Write-Host "`n[8/9] Configuring App Service settings..." -ForegroundColor Cyan

$connectionString = "Server=tcp:$SqlServerName.database.windows.net,1433;" +
    "Database=$SqlDbName;" +
    "User ID=$SqlAdminUser;" +
    "Password=$SqlAdminPassword;" +
    "Encrypt=True;" +
    "TrustServerCertificate=False;" +
    "Connection Timeout=30;"

Invoke-Az @("webapp", "config", "connection-string", "set",
    "--name", $AppName,
    "--resource-group", $ResourceGroup,
    "--connection-string-type", "SQLAzure",
    "--settings", "DefaultConnection=$connectionString",
    "--output", "none") | Out-Null
Write-Host "      Connection string set." -ForegroundColor Green

Invoke-Az @("webapp", "config", "appsettings", "set",
    "--name", $AppName,
    "--resource-group", $ResourceGroup,
    "--settings",
        "AzureStorageBlob__Endpoint=$blobEndpoint",
        "AzureStorageBlob__BackupContainerName=backups",
        "AzureStorageBlob__ExportContainerName=exports",
        "AzureStorageBlob__LogoContainerName=logos",
        "ASPNETCORE_ENVIRONMENT=Production",
    "--output", "none") | Out-Null
Write-Host "      App settings configured." -ForegroundColor Green

# HTTPS only + Always On
Invoke-Az @("webapp", "update",
    "--name", $AppName,
    "--resource-group", $ResourceGroup,
    "--https-only", "true",
    "--output", "none") | Out-Null

Invoke-Az @("webapp", "config", "set",
    "--name", $AppName,
    "--resource-group", $ResourceGroup,
    "--always-on", "true",
    "--output", "none") | Out-Null
Write-Host "      HTTPS-only and Always On enabled." -ForegroundColor Green

# ?????????????????????????????????????????????
# 9. Get Publish Profile XML (paste into GitHub Secret)
# ?????????????????????????????????????????????
Write-Host "`n[9/9] Fetching publish profile for GitHub Actions secret..." -ForegroundColor Cyan
$publishProfile = Invoke-Az @("webapp", "deployment", "list-publishing-profiles",
    "--name", $AppName,
    "--resource-group", $ResourceGroup,
    "--xml")

# Resolve output path: prefer the folder the script lives in, then HOME, then /tmp
$scriptDir = if ($PSScriptRoot -and (Test-Path $PSScriptRoot)) {
    $PSScriptRoot
} elseif ($env:HOME -and (Test-Path $env:HOME)) {
    $env:HOME
} else {
    [System.IO.Path]::GetTempPath()
}
$profilePath = Join-Path $scriptDir "publish-profile.xml"
$publishProfile | Out-File -FilePath $profilePath -Encoding utf8
Write-Host "      Saved to: $profilePath" -ForegroundColor Green
Write-Host "      Add this file's contents as GitHub secret 'AZURE_WEBAPP_PUBLISH_PROFILE'" -ForegroundColor Yellow

# ?????????????????????????????????????????????
# Summary
# ?????????????????????????????????????????????
Write-Host ""
Write-Host "????????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host "  Resource Group : $ResourceGroup"                        -ForegroundColor White
Write-Host "  Web App URL    : https://$AppName.azurewebsites.net"    -ForegroundColor White
Write-Host "  SQL Server     : $SqlServerName.database.windows.net"   -ForegroundColor White
Write-Host "  SQL Database   : $SqlDbName"                            -ForegroundColor White
Write-Host "  Storage Account: $StorageAccount"                       -ForegroundColor White
Write-Host "  Blob Endpoint  : $blobEndpoint"                         -ForegroundColor White
Write-Host "????????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host ""
Write-Host "NEXT STEPS:" -ForegroundColor Cyan
Write-Host "  1. Open $profilePath"                                    -ForegroundColor White
Write-Host "     Copy the entire file contents."                       -ForegroundColor White
Write-Host "  2. Go to GitHub ? Settings ? Secrets ? Actions:"        -ForegroundColor White
Write-Host "     https://github.com/Goderis-ToGo/GildeVanDeStroppendragers/settings/secrets/actions" -ForegroundColor White
Write-Host "     Create secret: AZURE_WEBAPP_PUBLISH_PROFILE"         -ForegroundColor White
Write-Host "  3. Push any commit to 'main' ? deploy runs automatically." -ForegroundColor White
Write-Host ""

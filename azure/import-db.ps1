#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Uploads the local .bacpac and imports it into Azure SQL.
#>

$StorageAccount = "stgildeprod"
$ResourceGroup  = "rg-gilde-prod"
$SqlServer      = "sql-gilde-prod"
$SqlDb          = "AdminMembersDb"
$SqlUser        = "sqladmin"
$SqlPassword    = $env:AZURE_SQL_PASSWORD  # Set via environment variable, never hardcode
$BacpacPath     = "/mnt/c/Temp/AdminMembersDb.bacpac"
$BlobContainer  = "backups"
$BlobName       = "AdminMembersDb.bacpac"

Write-Host "`n[1/3] Uploading .bacpac to blob storage..." -ForegroundColor Cyan
az storage blob upload --account-name $StorageAccount --container-name $BlobContainer --name $BlobName --file $BacpacPath --overwrite --output none
Write-Host "      Done." -ForegroundColor Green

Write-Host "`n[2/3] Getting storage key..." -ForegroundColor Cyan
$StorageKey = az storage account keys list --account-name $StorageAccount --resource-group $ResourceGroup --query "[0].value" --output tsv
Write-Host "      Done." -ForegroundColor Green

Write-Host "`n[3/3] Importing .bacpac into Azure SQL (this takes 2-5 minutes)..." -ForegroundColor Cyan
$StorageUri = "https://$StorageAccount.blob.core.windows.net/$BlobContainer/$BlobName"
az sql db import --server $SqlServer --resource-group $ResourceGroup --name $SqlDb --admin-user $SqlUser --admin-password $SqlPassword --storage-key-type StorageAccessKey --storage-key $StorageKey --storage-uri $StorageUri
Write-Host "      Done." -ForegroundColor Green

Write-Host "`nDatabase import complete! Your Azure SQL database now has all local data." -ForegroundColor Yellow

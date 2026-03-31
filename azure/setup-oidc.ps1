#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Creates an Azure AD App Registration with federated OIDC credentials for GitHub Actions.
    Run this once after provisioning. Output gives you the 3 GitHub secrets to add.
#>

$AppName        = "sp-gilde-github-actions"
$ResourceGroup  = "rg-gilde-prod"
$WebAppName     = "app-gilde-prod"
$GitHubOrg      = "Goderis-ToGo"
$GitHubRepo     = "GildeVanDeStroppendragers"
$GitHubBranch   = "main"

Write-Host "`n[1/4] Getting subscription info..." -ForegroundColor Cyan
$subJson       = az account show | ConvertFrom-Json
$subscriptionId = $subJson.id
$tenantId       = $subJson.tenantId
Write-Host "      Subscription : $($subJson.name)" -ForegroundColor Green
Write-Host "      Tenant ID    : $tenantId"         -ForegroundColor Green

Write-Host "`n[2/4] Creating App Registration '$AppName'..." -ForegroundColor Cyan
$appJson  = az ad app create --display-name $AppName | ConvertFrom-Json
$appId    = $appJson.appId
$objectId = $appJson.id
Write-Host "      App (client) ID : $appId"  -ForegroundColor Green

Write-Host "`n[3/4] Creating Service Principal and role assignment..." -ForegroundColor Cyan
az ad sp create --id $appId | Out-Null

# Scope to just the resource group
$scope = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroup"
az role assignment create --assignee $appId --role Contributor --scope $scope | Out-Null
Write-Host "      Contributor on $ResourceGroup assigned." -ForegroundColor Green

Write-Host "`n[4/4] Adding federated credential for GitHub Actions (branch: $GitHubBranch)..." -ForegroundColor Cyan
$credentialBody = @{
    name        = "github-actions-main"
    issuer      = "https://token.actions.githubusercontent.com"
    subject     = "repo:$GitHubOrg/${GitHubRepo}:ref:refs/heads/$GitHubBranch"
    description = "GitHub Actions OIDC for $GitHubRepo main branch"
    audiences   = @("api://AzureADTokenExchange")
} | ConvertTo-Json -Compress

az ad app federated-credential create --id $objectId --parameters $credentialBody | Out-Null
Write-Host "      Federated credential created." -ForegroundColor Green

Write-Host ""
Write-Host "????????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host "  Add these 3 secrets to GitHub:" -ForegroundColor Cyan
Write-Host "  https://github.com/$GitHubOrg/$GitHubRepo/settings/secrets/actions" -ForegroundColor White
Write-Host ""
Write-Host "  AZURE_CLIENT_ID       = $appId"       -ForegroundColor Green
Write-Host "  AZURE_TENANT_ID       = $tenantId"    -ForegroundColor Green
Write-Host "  AZURE_SUBSCRIPTION_ID = $subscriptionId" -ForegroundColor Green
Write-Host "????????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host ""
Write-Host "  You can now DELETE the old AZURE_WEBAPP_PUBLISH_PROFILE secret." -ForegroundColor Gray
Write-Host ""

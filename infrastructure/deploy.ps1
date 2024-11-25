[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$resourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$tenantId,
    
    [Parameter(Mandatory=$false)]
    [string]$location = "westeurope"
)

Write-Host "Script started..." -ForegroundColor Green

# Basic setup
$ErrorActionPreference = "Stop"
$VerbosePreference = "Continue"

Write-Host "Checking Azure PowerShell module..." -ForegroundColor Yellow
if (-not (Get-Module -ListAvailable -Name Az)) {
    Write-Host "Installing Az module..." -ForegroundColor Yellow
    Install-Module -Name Az -Scope CurrentUser -Repository PSGallery -Force -AllowClobber
}

Write-Host "Importing Az module..." -ForegroundColor Yellow
Import-Module Az

Write-Host "Connecting to Azure..." -ForegroundColor Yellow
Connect-AzAccount -TenantId $tenantId

Write-Host "Installing Bicep..." -ForegroundColor Yellow
az bicep install

Write-Host "Creating Resource Group if it doesn't exist..." -ForegroundColor Yellow
$rg = Get-AzResourceGroup -Name $resourceGroupName -ErrorAction SilentlyContinue
if (-not $rg) {
    Write-Host "Creating resource group '$resourceGroupName' in location '$location'..." -ForegroundColor Yellow
    New-AzResourceGroup -Name $resourceGroupName -Location $location
}

Write-Host "Deploying Bicep template..." -ForegroundColor Yellow
$deploymentName = "weatherstation-" + (Get-Date -Format "yyyyMMddHHmmss")
$templateFile = Join-Path $PSScriptRoot "main.bicep"

Write-Host "Template file path: $templateFile" -ForegroundColor Yellow
Write-Host "Deployment name: $deploymentName" -ForegroundColor Yellow

if (-not (Test-Path $templateFile)) {
    Write-Error "Bicep template file not found at: $templateFile"
    exit 1
}

Write-Host "Starting deployment..." -ForegroundColor Yellow
$deployment = New-AzResourceGroupDeployment `
    -Name $deploymentName `
    -ResourceGroupName $resourceGroupName `
    -TemplateFile $templateFile `
    -Verbose

if ($deployment.ProvisioningState -eq "Succeeded") {
    Write-Host "Deployment succeeded!" -ForegroundColor Green
    $functionAppName = $deployment.Outputs.functionAppName.Value
    Write-Host "Function App Name: $functionAppName" -ForegroundColor Cyan
} else {
    Write-Error "Deployment failed with state: $($deployment.ProvisioningState)"
    exit 1
}

Write-Host "Script completed!" -ForegroundColor Green
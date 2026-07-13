# Publish-QIN.ps1
# This script prepares the deployment package for the server.

Write-Host "--- QIN-Production: Publishing started ---" -ForegroundColor Cyan

# Make sure we are in the project directory
Set-Location -Path $PSScriptRoot

# 1. Remove old package if it exists
if (Test-Path ".\publish_output") {
    Remove-Item -Path ".\publish_output" -Recurse -Force
}

# 2. Publish the project for IIS deployment
Write-Host "Publishing files including the .NET runtime for the server..." -ForegroundColor Yellow
dotnet publish ".\QIN-Production-Web.csproj" -c Release -o ".\publish_output" -r win-x64 --self-contained true --nologo

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSUCCESS!" -ForegroundColor Green
    Write-Host "All server files are now available in this folder:"
    Write-Host (Get-Item ".\publish_output").FullName -ForegroundColor White
    Write-Host "`nNext steps:"
    Write-Host "1. Copy the CONTENTS of 'publish_output' to the server target folder (for example C:\inetpub\wwwroot\qin)." -ForegroundColor White
    Write-Host "2. Important: IIS must point to the deployed target folder that contains 'web.config' and 'QIN-Production-Web.exe'." -ForegroundColor Yellow
    Write-Host "3. Do not point IIS to the source project folder and do not copy 'publish_output' as a nested subfolder." -ForegroundColor Yellow
    Write-Host "4. Make sure the '.NET Hosting Bundle' is installed on the server." -ForegroundColor White
    Write-Host "5. Create or update the IIS website so its Physical Path matches that deployed target folder exactly." -ForegroundColor White
}
else {
    Write-Host "`nPublishing failed!" -ForegroundColor Red
}

pause

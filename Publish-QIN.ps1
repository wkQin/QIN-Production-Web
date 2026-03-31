# Publish-QIN.ps1
# Dieses Skript bereitet alles für den Server vor.

Write-Host "--- QIN-Production: Veröffentlichung wird gestartet ---" -ForegroundColor Cyan

# Sicherstellen, dass wir im richtigen Verzeichnis sind
Set-Location -Path $PSScriptRoot

# 1. Altes Paket löschen, falls vorhanden
if (Test-Path ".\publish_output") {
    Remove-Item -Path ".\publish_output" -Recurse -Force
}

# 2. Projekt kompilieren und exportieren (Release Mode + Self-Contained)
Write-Host "Kompiliere Dateien inkl. .NET 10 Runtime für den Server..." -ForegroundColor Yellow
dotnet publish ".\QIN-Production-Web.csproj" -c Release -o ".\publish_output" -r win-x64 --self-contained true --nologo

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nERFOLG!" -ForegroundColor Green
    Write-Host "Sämtliche Dateien für den Server liegen nun in diesem Ordner:"
    Write-Host (Get-Item ".\publish_output").FullName -ForegroundColor White
    Write-Host "`nNächste Schritte:"
    Write-Host "1. Kopiere den INHALT des 'publish_output' Ordners auf deinen Server (z.B. nach C:\inetpub\wwwroot\qin)."
    Write-Host "2. Stelle sicher, dass auf dem Server das '.NET Hosting Bundle' installiert ist."
    Write-Host "3. Erstelle im IIS Manager eine neue Website, die auf diesen Pfad zeigt."
}
else {
    Write-Host "`nFEHLER beim Erstellen des Pakets!" -ForegroundColor Red
}
pause

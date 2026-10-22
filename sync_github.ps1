# Zen - GitHub Senkronizasyon Scripti
$env:Path = "C:\Users\oturu\AppData\Local\Microsoft\WinGet\Packages\Git.MinGit_Microsoft.Winget.Source_8wekyb3d8bbwe\cmd;" + $env:Path

param (
    [string]$Message = "Guncelleme: " + (Get-Date -Format "yyyy-MM-dd HH:mm")
)

Write-Host "Zen GitHub Senkronizasyonu Baslatiliyor..." -ForegroundColor Cyan

git add .
$status = git status --porcelain
if (-not $status) {
    Write-Host "Degisiklik bulunamadi, repo guncel." -ForegroundColor Yellow
    exit 0
}

git commit -m $Message
git push origin main

Write-Host "Basariyla GitHub'a gonderildi!" -ForegroundColor Green

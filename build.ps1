# Build script for Zen Fence - Masaüstü Düzenleyici (Geliştirici: Serkan ŞENTÜRK)
$ErrorActionPreference = "Stop"

$currentDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $currentDir) { $currentDir = Get-Location }

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$wpfPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"
$outExe = Join-Path $currentDir "ZenFence.exe"
$sourceDir = Join-Path $currentDir "Source"

if (-not (Test-Path $csc)) {
    Write-Error "C# compiler not found at: $csc"
    exit 1
}

$sources = Get-ChildItem -Path $sourceDir -Filter "*.cs" | Select-Object -ExpandProperty FullName

$references = @(
    "System.dll",
    "System.Core.dll",
    "System.Data.dll",
    "System.Drawing.dll",
    "System.Windows.Forms.dll",
    "System.Web.Extensions.dll",
    "System.Xaml.dll",
    (Join-Path $wpfPath "WindowsBase.dll"),
    (Join-Path $wpfPath "PresentationCore.dll"),
    (Join-Path $wpfPath "PresentationFramework.dll")
)

$refArgs = $references | ForEach-Object { "/r:`"$_`"" }

$iconPath = Join-Path $currentDir "app.ico"
$iconArgs = if (Test-Path $iconPath) { @("/win32icon:`"$iconPath`"") } else { @() }

$compileArgs = @(
    "/target:winexe",
    "/optimize+",
    "/platform:anycpu",
    "/out:`"$outExe`""
) + $iconArgs + $refArgs + ($sources | ForEach-Object { "`"$_`"" })

Write-Host "Compiling Zen Fence - Masaüstü Düzenleyici (Geliştirici: Serkan ŞENTÜRK)..." -ForegroundColor Cyan

$process = Start-Process -FilePath $csc -ArgumentList $compileArgs -NoNewWindow -Wait -PassThru

if ($process.ExitCode -eq 0) {
    Write-Host "BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "Output: $outExe" -ForegroundColor Green
    
    # Also keep Zen.exe for compatibility
    Copy-Item $outExe (Join-Path $currentDir "Zen.exe") -Force

    Write-Host "Outputs ready: ZenFence.exe & Zen.exe" -ForegroundColor Green
} else {
    Write-Host "BUILD FAILED! ExitCode: $($process.ExitCode)" -ForegroundColor Red
    exit $process.ExitCode
}

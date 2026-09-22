# DailyGiftClaim - one-click build & package script
# Usage: run inside the DailyGiftClaim directory:
#   powershell -ExecutionPolicy Bypass -File .\publish.ps1
# Prereq: .NET 8 SDK (https://dotnet.microsoft.com/download/dotnet/8.0)
# Note: this script is kept pure ASCII on purpose (PS 5.1 reads no-BOM files as GBK,
#       which would garble Chinese text). The output folder name "发布" is built
#       from char codes below, so the folder is always created correctly.

param(
    [string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
Write-Host "==> Building (dotnet build -c $Configuration)..." -ForegroundColor Cyan
dotnet build "$root\DailyGiftClaim.csproj" -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Build failed. Please install .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0" }

$out = "$root\bin\$Configuration\net8.0-windows"
# Output folder name: 发(U+53D1) + 布(U+5E03) = "发布"
$pubDir = [string][char]0x53D1 + [char]0x5E03
# MOD package folder name (English; the display name comes from info.lps vupmod#, unrelated to this)
$modName = "DailyGift"
$mod = "$root\$pubDir\$modName"

Write-Host "==> Assembling MOD package: $mod" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path "$mod\plugin" | Out-Null

# 1. Manifest
Copy-Item "$root\info.lps" $mod -Force

# 1.5 Cover icon (workshop requires icon.png < 512KB in the mod root)
if (Test-Path "$root\icon.png") { Copy-Item "$root\icon.png" $mod -Force }

# 2. Plugin DLL and all runtime dependencies (same layout as official ModMaker's plugin dir)
Get-ChildItem $out -Filter *.dll | Copy-Item -Destination "$mod\plugin" -Force

# 3. Language files (lang/<culture>/*.lps)
if (Test-Path "$root\lang") { Copy-Item "$root\lang" "$mod\" -Recurse -Force }

Write-Host ""
Write-Host "DONE! MOD folder: $mod" -ForegroundColor Green
Write-Host ""
Write-Host "Local test: copy the whole folder to <game>\mod\$modName"
Write-Host "    enable the code plugin in Settings -> MOD Manager if prompted, then restart the game."
Write-Host ""
Write-Host "Workshop upload: Steam -> Library -> VPet-Simulator -> Workshop -> Upload new item"
Write-Host "    select folder: $mod"
Write-Host "    (confirm author in $mod\info.lps before uploading)"

# Ayaz HUD'u Release olarak derler ve kurulum dosyasini (cikti\AyazHUD-Kurulum-<surum>.exe) uretir.
# Gerekenler: .NET SDK 8+ (dotnet), Inno Setup 7 (ISCC.exe).
# Kullanim: powershell -ExecutionPolicy Bypass -File derle.ps1
param(
  [string]$Dotnet = '',
  [string]$Iscc = ''
)
$ErrorActionPreference = 'Stop'
$kok = $PSScriptRoot

function Bul([string]$verilen, [string[]]$adaylar, [string]$komut) {
  if ($verilen) { return $verilen }
  foreach ($a in $adaylar) { if (Test-Path $a) { return $a } }
  $c = Get-Command $komut -ErrorAction SilentlyContinue
  if ($c) { return $c.Source }
  throw "$komut bulunamadi. Yolunu parametreyle ver: -$($komut.Split('.')[0]) <yol>"
}

$Dotnet = Bul $Dotnet @("$env:USERPROFILE\.dotnet\dotnet.exe", "$env:ProgramFiles\dotnet\dotnet.exe") 'dotnet.exe'
$Iscc = Bul $Iscc @("$env:LOCALAPPDATA\Programs\Inno Setup 7\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe", "$env:ProgramFiles\Inno Setup 7\ISCC.exe") 'ISCC.exe'

$proje = Join-Path $kok 'src\AyazHUD\AyazHUD.csproj'
$surum = ([xml](Get-Content $proje -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
Write-Host "Surum: $surum"

$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
& $Dotnet build $proje -c Release -v minimal
if ($LASTEXITCODE -ne 0) { throw "Derleme basarisiz" }

& $Iscc "/DSurum=$surum" (Join-Path $kok 'kurulum\AyazHUD.iss')
if ($LASTEXITCODE -ne 0) { throw "Kurulum dosyasi uretilemedi" }

Get-Item (Join-Path $kok "cikti\AyazHUD-Kurulum-$surum.exe") | Select-Object FullName, Length

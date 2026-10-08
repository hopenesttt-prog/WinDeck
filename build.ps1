# WinDeck build script (uses the C# compiler that ships with Windows / .NET Framework 4.8)
# Usage: powershell -ExecutionPolicy Bypass -File build.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $csc)) { throw 'csc.exe (.NET Framework 4.x) not found.' }

$build = Join-Path $root 'build'
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $build, $dist | Out-Null

# 1) Generate the application icon from the same drawing code the app uses.
$iconGen = Join-Path $build 'IconGen.exe'
$ico = Join-Path $build 'app.ico'
& $csc /nologo /codepage:65001 /target:exe "/out:$iconGen" /r:System.Drawing.dll `
    (Join-Path $root 'tools\IconGen.cs') (Join-Path $root 'src\AppIcon.cs')
if ($LASTEXITCODE -ne 0) { throw 'IconGen compile failed.' }
& $iconGen $ico
if ($LASTEXITCODE -ne 0) { throw 'Icon generation failed.' }

# 2) Compile WinDeck.exe
$sources = Get-ChildItem (Join-Path $root 'src') -Filter *.cs | ForEach-Object { $_.FullName }
$exe = Join-Path $dist 'WinDeck.exe'
& $csc /nologo /codepage:65001 /target:winexe /optimize+ /platform:anycpu `
    "/out:$exe" "/win32icon:$ico" "/win32manifest:$(Join-Path $root 'app.manifest')" `
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Web.Extensions.dll `
    $sources
if ($LASTEXITCODE -ne 0) { throw 'WinDeck compile failed.' }

Write-Host "Build OK: $exe"

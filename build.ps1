param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$buildRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('nocturne-build-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $buildRoot | Out-Null
Copy-Item -Path (Join-Path $projectRoot '*') -Destination $buildRoot -Recurse
$destination = Join-Path (Split-Path $projectRoot -Parent) 'Nocturne-portable'
& $Dotnet publish (Join-Path $buildRoot 'Nocturne.csproj') -c Release -r win-x64 --self-contained true -p:Platform=x64 -p:DebugType=None -p:DebugSymbols=false -o $destination
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Output ('Portable application: ' + (Join-Path $destination 'Nocturne.exe'))

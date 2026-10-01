param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$buildRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('yume-build-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $buildRoot | Out-Null
Copy-Item -Path (Join-Path $projectRoot '*') -Destination $buildRoot -Recurse -Exclude '.git', 'bin', 'obj'
$destination = Join-Path (Split-Path $projectRoot -Parent) 'Yume-portable'
& $Dotnet publish (Join-Path $buildRoot 'Yume.csproj') -c Release -r win-x64 --self-contained true -p:Platform=x64 -p:DebugType=None -p:DebugSymbols=false -o $destination
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Output ('Portable application: ' + (Join-Path $destination 'Yume.exe'))

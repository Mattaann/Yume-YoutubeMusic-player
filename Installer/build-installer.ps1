param(
    [Parameter(Mandatory=$true)][string]$AppSource,
    [Parameter(Mandatory=$true)][string]$Bootstrapper,
    [string]$Compiler = 'ISCC.exe',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\..'),
    [string]$Version = '1.2.10'
)
$ErrorActionPreference = 'Stop'
$app = (Resolve-Path -LiteralPath $AppSource).Path
$runtime = (Resolve-Path -LiteralPath $Bootstrapper).Path
if (!(Test-Path -LiteralPath (Join-Path $app 'Yume.exe'))) { throw 'Publish the Windows x64 app first.' }
$signature = Get-AuthenticodeSignature -LiteralPath $runtime
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') { throw 'A valid Microsoft-signed WebView2 bootstrapper is required.' }
foreach ($name in @('WebView2','settings.json','background-probe.json','integration-check.json')) {
    if (Test-Path -LiteralPath (Join-Path $app $name)) { throw 'The app folder contains profile or diagnostic data.' }
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$output = (Resolve-Path -LiteralPath $OutputDirectory).Path
& $Compiler "/DAppVersion=$Version" "/DAppSource=$app" "/DBootstrapper=$runtime" "/DSetupOutput=$output" (Join-Path $PSScriptRoot 'Yume.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer build failed.' }

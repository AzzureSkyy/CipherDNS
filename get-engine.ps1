# Downloads the official dnscrypt-proxy Windows build into .\engine and verifies its SHA-256.
$version = '2.1.20'
$sha256 = '9e498cb9bdbae57756db19ce516362ac307e9340a6996214964cfcc7c489abc3'
$url = "https://github.com/DNSCrypt/dnscrypt-proxy/releases/download/$version/dnscrypt-proxy-win64-$version.zip"
$zip = Join-Path $env:TEMP "dnscrypt-proxy-$version.zip"
$ProgressPreference = 'SilentlyContinue'
Invoke-WebRequest $url -OutFile $zip
if ((Get-FileHash $zip -Algorithm SHA256).Hash.ToLower() -ne $sha256) { throw 'Checksum mismatch; not installing.' }
$tmp = Join-Path $env:TEMP "dnscrypt-proxy-$version"
Expand-Archive $zip $tmp -Force
New-Item -ItemType Directory -Force (Join-Path $PSScriptRoot 'engine') | Out-Null
Copy-Item (Join-Path $tmp 'win64\*') (Join-Path $PSScriptRoot 'engine') -Force
Write-Host "dnscrypt-proxy $version installed to engine\"

[CmdletBinding()]
param(
    [string]$Version = '1.0.37'
)

$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^(\d+)\.(\d+)\.(\d+)$') {
    throw "Surum X.Y.Z biciminde olmali: $Version"
}

$versionCode = ([int]$Matches[1] * 10000) + ([int]$Matches[2] * 1000) + [int]$Matches[3]
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'MKFiloServis.Client\MKFiloServis.Client.csproj'
$output = Join-Path $PSScriptRoot "output\v$Version"
$desktopPayload = Join-Path $PSScriptRoot 'payload\Desktop'
$androidApk = Join-Path $root 'MKFiloServis.Client\bin\Release\net10.0-android\publish\com.mkfiloservis.client-Signed.apk'

$expectedDesktopPayload = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'payload\Desktop'))
if (-not [System.IO.Path]::GetFullPath($desktopPayload).Equals($expectedDesktopPayload, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Masaustu payload yolu setup klasoru disinda: $desktopPayload"
}

$iscc = @(
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe',
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 ISCC.exe bulunamadi.' }

if (Test-Path -LiteralPath $desktopPayload) {
    Remove-Item -LiteralPath $desktopPayload -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $output, $desktopPayload | Out-Null

dotnet restore $project -p:TargetFramework=net10.0-android --nologo
if ($LASTEXITCODE -ne 0) { throw 'Android restore basarisiz.' }
dotnet publish $project -f net10.0-android -c Release -p:AndroidPackageFormats=apk `
    -p:ApplicationDisplayVersion=$Version -p:ApplicationVersion=$versionCode --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'Android APK build basarisiz.' }
if (-not (Test-Path -LiteralPath $androidApk)) { throw "Imzali APK bulunamadi: $androidApk" }
Copy-Item -LiteralPath $androidApk -Destination (Join-Path $output "MKFiloServisAndroid-$Version.apk") -Force

dotnet restore $project -p:TargetFramework=net10.0-windows10.0.19041.0 -p:RuntimeIdentifier=win-x64 --nologo
if ($LASTEXITCODE -ne 0) { throw 'Windows restore basarisiz.' }
dotnet publish $project -f net10.0-windows10.0.19041.0 -r win-x64 -c Release `
    -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -p:SelfContained=true `
    -p:PublishReadyToRun=false -p:ApplicationDisplayVersion=$Version `
    -o $desktopPayload --no-restore --nologo
if ($LASTEXITCODE -ne 0) { throw 'Windows istemci publish basarisiz.' }

& $iscc "/DMyAppVersion=$Version" "/DOutputDir=$output" (Join-Path $PSScriptRoot 'DesktopSetup.iss')
if ($LASTEXITCODE -ne 0) { throw 'Masaustu Inno Setup basarisiz.' }

$packages = Get-ChildItem -LiteralPath $output -File |
    Where-Object { $_.Extension -in '.exe', '.apk' } |
    Sort-Object Name
$checksums = foreach ($package in $packages) {
    '{0}  {1}' -f (Get-FileHash -LiteralPath $package.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $package.Name
}
$checksums | Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt') -Encoding UTF8
$Version | Set-Content -LiteralPath (Join-Path $output 'version.txt') -Encoding UTF8

@"
# MK Filo Servis v$Version

- MKFiloServisKurulum-$Version.exe: Web sunucusu, lisans araci ve veri aktarimi.
- MKFiloServisGuncelle-$Version.exe: Mevcut Windows kurulumu icin guncelleme.
- MKFiloServisKurulumMusteri-$Version.exe: Lisans araci haric musteri kurulumu.
- MKLisansArac-$Version.exe: Bagimsiz lisans araci.
- MKFiloServisMasaustu-$Version.exe: Windows masaustu istemcisi.
- MKFiloServisAndroid-$Version.apk: Android istemcisi, gelistirme anahtariyla imzali.

Masaustu ve Android istemcileri web sunucusuna baglanir; sunucuyu kendi iclerinde barindirmaz.
Windows istemcisinin ilk adresi http://localhost:5050. Android'de sunucunun ag IP'si
veya HTTPS adresi girilir. APK'nin yuklenmesi icin cihazda dis kaynaklardan yuklemeye izin verin.
Dosya SHA256 degerleri SHA256SUMS.txt icindedir.
"@ | Set-Content -LiteralPath (Join-Path $output 'README.md') -Encoding UTF8

$packages | Select-Object Name, Length | Format-Table -AutoSize

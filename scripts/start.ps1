param(
    [string]$AppPath = (Split-Path -Parent $PSScriptRoot),
    [ValidateRange(1, 65535)][int]$Port = 5050,
    [switch]$NoBrowser,
    [switch]$CheckOnly
)

$ErrorActionPreference = 'Stop'
$process = $null
try {
    $AppPath = (Resolve-Path $AppPath).Path
    $project = Join-Path $AppPath 'MKFiloServis.Web\MKFiloServis.Web.csproj'
    $dll = Join-Path $AppPath 'MKFiloServis.Web.dll'
    $exe = Join-Path $AppPath 'MKFiloServis.Web.exe'
    $runtimeConfig = Join-Path $AppPath 'MKFiloServis.Web.runtimeconfig.json'
    $source = Test-Path $project
    $selfContained = $false
    if (!$source -and (Test-Path $runtimeConfig)) {
        $runtime = Get-Content -Raw $runtimeConfig | ConvertFrom-Json
        $selfContained = $null -ne $runtime.runtimeOptions.includedFrameworks
    }
    if ($selfContained -and !(Test-Path $exe)) {
        throw 'Self-contained publish icin MKFiloServis.Web.exe bulunamadi. Paketi yeniden yayinlayin.'
    }
    if (!$source -and !(Test-Path $dll) -and !(Test-Path $exe)) {
        throw 'Web projesi veya publish ciktisi bulunamadi. -AppPath ile uygulama klasorunu belirtin.'
    }

    $dotnet = $null
    if ($source -or !$selfContained) {
        $dotnet = (Get-Command dotnet -ErrorAction Stop).Source
        if ($source) {
            $sdks = & $dotnet --list-sdks
            if ($LASTEXITCODE -ne 0 -or !($sdks -match '^(10|11)\.')) {
                throw 'Kaynak projeyi calistirmak icin .NET 10 veya uyumlu .NET 11 SDK gerekir.'
            }
        }
        $runtimes = & $dotnet --list-runtimes
        if ($LASTEXITCODE -ne 0 -or !($runtimes -match '^Microsoft.AspNetCore.App 10\.') -or !($runtimes -match '^Microsoft.NETCore.App 10\.')) {
            throw '.NET 10 ASP.NET Core Runtime gerekli. Yalniz SDK kontrolu yeterli degildir.'
        }
    }

    $environment = if ($source) { 'Development' } else { 'Production' }
    if (!$source -and [string]::IsNullOrWhiteSpace($env:Jwt__Secret)) {
        throw 'Production icin korumali Jwt__Secret ortam degiskenini saglayin. IIS kurulumu icin configure-iis-startup.ps1 kullanin; sirri JSON veya komut satirina yazmayin.'
    }

    $selectedPort = $null
    $lastPort = [Math]::Min($Port + 19, 65535)
    foreach ($candidate in $Port..$lastPort) {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $candidate)
        try {
            $listener.Start()
            $selectedPort = $candidate
            break
        }
        catch [System.Net.Sockets.SocketException] {
            Write-Host "Port $candidate kullanimda; siradaki port deneniyor."
        }
        finally { $listener.Stop() }
    }
    if ($null -eq $selectedPort) { throw 'Uygun port bulunamadi. -Port ile farkli bir baslangic portu secin.' }
    $url = "http://localhost:$selectedPort"
    Write-Host "Ortam: $environment; Web adresi: $url"
    if ($CheckOnly) {
        Write-Host 'Baslangic on kontrolleri basarili. Uygulama ve veritabani islemleri baslatilmadi.'
        exit 0
    }

    $info = [System.Diagnostics.ProcessStartInfo]::new()
    $info.UseShellExecute = $false
    $info.WorkingDirectory = if ($source) { Split-Path -Parent $project } else { $AppPath }
    $info.EnvironmentVariables['ASPNETCORE_ENVIRONMENT'] = $environment
    $info.EnvironmentVariables['DOTNET_ENVIRONMENT'] = $environment
    if (!$source -and (Test-Path $exe)) {
        $info.FileName = $exe
        $info.Arguments = "--urls $url"
    }
    elseif ($source) {
        & $dotnet build $project -c Debug
        if ($LASTEXITCODE -ne 0) { throw 'Web projesi derlenemedi; yukaridaki derleme hatalarini kontrol edin.' }
        $sourceDll = Join-Path (Split-Path -Parent $project) 'bin\Debug\net10.0\MKFiloServis.Web.dll'
        $info.FileName = $dotnet
        $info.Arguments = "`"$sourceDll`" --urls $url"
    }
    else {
        $info.FileName = $dotnet
        $info.Arguments = "`"$dll`" --urls $url"
    }
    $process = [System.Diagnostics.Process]::Start($info)
    $ready = $false
    $deadline = [DateTime]::UtcNow.AddMinutes(5)
    while (!$process.HasExited -and [DateTime]::UtcNow -lt $deadline) {
        try {
            $response = Invoke-WebRequest "$url/healthz" -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) { $ready = $true; break }
        }
        catch [System.Net.WebException] {
            Start-Sleep -Seconds 1
        }
    }
    if (!$ready) { throw 'Uygulama hazir olmadi. Yukaridaki baslangic/veritabani hata kayitlarini kontrol edin.' }
    Write-Host 'Uygulama hazir.'
    if (!$NoBrowser) { Start-Process $url }
    $process.WaitForExit()
    exit $process.ExitCode
}
catch {
    Write-Error $_ -ErrorAction Continue
    exit 1
}
finally {
    if ($null -ne $process) {
        if (!$process.HasExited) { $process.Kill() }
        $process.Dispose()
    }
}

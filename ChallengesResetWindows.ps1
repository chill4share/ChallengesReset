# Script xoa token thu thach LMHT

try {
    $LeagueProcess = Get-CimInstance -ClassName Win32_Process -Filter "Name = 'LeagueClientUx.exe'" -ErrorAction Stop
}
catch {
    Write-Host "Loi: Khong tim thay tien trinh LeagueClientUx.exe" -ForegroundColor Red
    Start-Sleep -Seconds 5
    exit
}

$Port = $LeagueProcess.CommandLine | Select-String -Pattern '--app-port=(\d+)' | ForEach-Object { $_.Matches.Groups[1].Value }
$AuthToken = $LeagueProcess.CommandLine | Select-String -Pattern '--remoting-auth-token=([^\s"]+)' | ForEach-Object { $_.Matches.Groups[1].Value }

if (-not $Port -or -not $AuthToken) {
    Write-Host "Loi: Khong the lay port hoac token" -ForegroundColor Red
    Start-Sleep -Seconds 5
    exit
}

$URL = "https://127.0.0.1:$Port/lol-challenges/v1/update-player-preferences/"
$EncodedAuth = [Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes("riot:$AuthToken"))
$headers = @{
    "Authorization" = "Basic $EncodedAuth"
    "Accept"        = "application/json"
}
$body = @{ "challengeIds" = @() } | ConvertTo-Json -Compress

Write-Host "Dang gui yeu cau xoa token..."

try {
    if ($PSVersionTable.PSVersion.Major -lt 6) {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
        Invoke-RestMethod -Uri $URL -Headers $headers -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop
    }
    else {
        Invoke-RestMethod -Uri $URL -Headers $headers -Method Post -Body $body -ContentType "application/json" -SkipCertificateCheck -ErrorAction Stop
    }
    Write-Host "Thanh cong! Token thu thach da duoc xoa." -ForegroundColor Green
}
catch {
    Write-Host "Loi khi gui yeu cau: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "Dong sau 5 giay..."
Start-Sleep -Seconds 5

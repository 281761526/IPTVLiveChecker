param(
    [string]$ConfigPath = "App.config"
)

$ErrorActionPreference = "Stop"

Write-Host "Step 0/3: Detecting EXE path..."
$exePaths = @(
    "bin\x64\Debug\IPTVLiveChecker.exe",
    "bin\Debug\IPTVLiveChecker.exe",
    "bin\x64\Release\IPTVLiveChecker.exe",
    "bin\Release\IPTVLiveChecker.exe"
)

$candidateExes = @()
foreach ($path in $exePaths) {
    if (Test-Path $path) {
        $candidateExes += Get-Item $path
    }
}

if ($candidateExes.Count -eq 0) {
    Write-Host "Error: IPTVLiveChecker.exe not found in any known path" -ForegroundColor Red
    Write-Host "Searched paths:" -ForegroundColor Yellow
    foreach ($path in $exePaths) {
        Write-Host "  $path"
    }
    Read-Host "Press Enter to exit"
    exit 1
}

$ExePath = ($candidateExes | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
Write-Host "Found $(($candidateExes | Select-Object FullName).FullName -join ', ')"
Write-Host "Using latest: $ExePath" -ForegroundColor Green

Write-Host "Step 1/3: Calculating EXE MD5..."
$md5 = New-Object System.Security.Cryptography.MD5CryptoServiceProvider
$stream = [System.IO.File]::OpenRead($ExePath)
$hash = $md5.ComputeHash($stream)
$stream.Close()
$md5Str = ([System.BitConverter]::ToString($hash) -replace "-", "").ToUpper()
Write-Host "MD5: $md5Str"

Write-Host "Step 2/3: AES Encrypting..."
$AesKey = [byte[]]@(0x4D, 0x6F, 0x72, 0x65, 0x53, 0x65, 0x63, 0x72, 0x65, 0x74, 0x4B, 0x65, 0x79, 0x21, 0x40, 0x23, 0x58, 0x59, 0x7A, 0x61, 0x31, 0x32, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B)
$AesIV = [byte[]]@(0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46)
$aes = [System.Security.Cryptography.Aes]::Create()
$aes.Key = $AesKey
$aes.IV = $AesIV
$aes.Mode = [System.Security.Cryptography.CipherMode]::CBC
$aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7
$plainBytes = [System.Text.Encoding]::UTF8.GetBytes($md5Str)
$encryptor = $aes.CreateEncryptor()
$ms = New-Object System.IO.MemoryStream
$cs = New-Object System.Security.Cryptography.CryptoStream($ms, $encryptor, [System.Security.Cryptography.CryptoStreamMode]::Write)
$cs.Write($plainBytes, 0, $plainBytes.Length)
$cs.FlushFinalBlock()
$encryptedBase64 = [System.Convert]::ToBase64String($ms.ToArray())
$cs.Close()
$ms.Close()
Write-Host "Encrypted: $encryptedBase64"

Write-Host "Step 3/3: Updating configuration files..."
$config = [System.IO.File]::ReadAllText($ConfigPath, [System.Text.Encoding]::UTF8)
$oldTag = '<add key="ExpectedExeMd5" value="'
$oldEnd = '" />'
$oldValue = $config.Substring($config.IndexOf($oldTag) + $oldTag.Length)
$oldValue = $oldValue.Substring(0, $oldValue.IndexOf($oldEnd))
$newConfig = $config.Replace($oldTag + $oldValue + $oldEnd, $oldTag + $encryptedBase64 + $oldEnd)

[System.IO.File]::WriteAllText($ConfigPath, $newConfig, [System.Text.Encoding]::UTF8)
Write-Host "App.config updated!"

$outputConfigPath = Join-Path (Split-Path $ExePath) "IPTVLiveChecker.exe.config"
if (Test-Path $outputConfigPath) {
    [System.IO.File]::WriteAllText($outputConfigPath, $newConfig, [System.Text.Encoding]::UTF8)
    Write-Host "$outputConfigPath updated!"
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
Write-Host "EXE Path:    $ExePath"
Write-Host "EXE MD5:     $md5Str"
Write-Host "Encrypted:   $encryptedBase64"
Write-Host ""
Read-Host "Press Enter to exit"
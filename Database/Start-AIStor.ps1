$ErrorActionPreference = "Stop"

$serverPath = if ($env:MINIO_SERVER_EXE) {
    $env:MINIO_SERVER_EXE
} else {
    Join-Path $env:USERPROFILE "Downloads\minio.exe"
}
$stateRoot = Join-Path $env:LOCALAPPDATA "TemplateManagementSystem"
$licensePath = Join-Path $stateRoot "minio.license"
$dataPath = Join-Path $stateRoot "minio-data"
$secretsPath = Join-Path $env:APPDATA "Microsoft\UserSecrets\template-management-local\secrets.json"

if (-not (Test-Path -LiteralPath $serverPath)) {
    throw "AIStor server binary not found at '$serverPath'. Set MINIO_SERVER_EXE to its full path."
}
if (-not (Test-Path -LiteralPath $licensePath)) {
    throw "AIStor license not found at '$licensePath'."
}
if (-not (Test-Path -LiteralPath $secretsPath)) {
    throw "Application User Secrets not found. Configure ObjectStorage:AccessKey and ObjectStorage:SecretKey first."
}

$secrets = Get-Content -LiteralPath $secretsPath -Raw | ConvertFrom-Json
$env:MINIO_ROOT_USER = $secrets.'ObjectStorage:AccessKey'
$env:MINIO_ROOT_PASSWORD = $secrets.'ObjectStorage:SecretKey'
if (-not $env:MINIO_ROOT_USER -or -not $env:MINIO_ROOT_PASSWORD) {
    throw "Configure ObjectStorage:AccessKey and ObjectStorage:SecretKey before starting AIStor."
}

New-Item -ItemType Directory -Force -Path $dataPath | Out-Null
& $serverPath server --license $licensePath --address "127.0.0.1:9000" --console-address "127.0.0.1:9001" $dataPath
if ($LASTEXITCODE -ne 0) {
    throw "AIStor exited with code $LASTEXITCODE."
}

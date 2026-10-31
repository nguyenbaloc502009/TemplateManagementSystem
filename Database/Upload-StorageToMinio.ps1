$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    & dotnet run --project "Database\StorageMigration\StorageMigration.csproj"
    if ($LASTEXITCODE -ne 0) {
        throw "Storage migration failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

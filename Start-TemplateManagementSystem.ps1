$ErrorActionPreference = "Stop"

$applications = @(
    @{
        Name = "DocumentTemplateSystem"
        Project = Join-Path $PSScriptRoot "DocumentTemplateSystem\DocumentTemplateSystem.csproj"
        Port = 5001
    },
    @{
        Name = "PresentationTemplateSystem"
        Project = Join-Path $PSScriptRoot "PresentationTemplateSystem\PresentationTemplateSystem.csproj"
        Port = 5002
    }
)

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "The .NET SDK was not found. Install the .NET 8 SDK before starting the applications."
}

foreach ($application in $applications) {
    $projectDirectory = Split-Path -Parent $application.Project
    $listeners = @(Get-NetTCPConnection -State Listen -LocalPort $application.Port -ErrorAction SilentlyContinue)
    $processIds = @($listeners | Select-Object -ExpandProperty OwningProcess -Unique)
    foreach ($processId in $processIds) {
        $runningApplication = Get-CimInstance Win32_Process -Filter "ProcessId = $processId"
        if (-not $runningApplication.ExecutablePath -or
            -not $runningApplication.ExecutablePath.StartsWith($projectDirectory, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Port $($application.Port) is already used by another process ($($runningApplication.Name), PID $processId); it was not stopped."
        }

        Stop-Process -Id $processId -ErrorAction Stop
        Write-Output "Stopped the previous $($application.Name) instance (PID $processId)."
    }

    $deadline = (Get-Date).AddSeconds(15)
    do {
        Start-Sleep -Milliseconds 500
        $listeners = @(Get-NetTCPConnection -State Listen -LocalPort $application.Port -ErrorAction SilentlyContinue)
    } while ($listeners.Count -gt 0 -and (Get-Date) -lt $deadline)

    if ($listeners.Count -gt 0) {
        throw "Port $($application.Port) did not become available after stopping $($application.Name)."
    }

    $arguments = 'run --project "{0}" --urls "http://localhost:{1}"' -f $application.Project, $application.Port
    $process = Start-Process -FilePath "dotnet" -ArgumentList $arguments -WorkingDirectory $PSScriptRoot -PassThru
    $deadline = (Get-Date).AddSeconds(30)

    do {
        Start-Sleep -Seconds 1
        $listener = Get-NetTCPConnection -State Listen -LocalPort $application.Port -ErrorAction SilentlyContinue
        if ($listener) {
            break
        }
    } while ((Get-Date) -lt $deadline)

    if (-not $listener) {
        throw "$($application.Name) did not open port $($application.Port) within 30 seconds."
    }

    Write-Output "$($application.Name) started at http://localhost:$($application.Port)"
}

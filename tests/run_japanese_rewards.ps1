# Windows only. Run from any directory; builds and executes the actual WFInfo pipeline.
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
try {
    dotnet build WFInfo.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
    $map = Join-Path $PSScriptRoot 'japanese-rewards.json'
    $spec = Get-Content $map -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach ($scenario in $spec.scenarios) {
        foreach ($extension in @('.json', '.png')) {
            if (!(Test-Path (Join-Path $PSScriptRoot ($scenario + $extension)))) {
                throw "Missing fixture: $scenario$extension"
            }
        }
    }
    $output = Join-Path $env:TEMP ("wfinfo-japanese-" + [guid]::NewGuid().ToString('N') + '.json')
    $exe = Join-Path $repo 'WFInfo\bin\Release\net48\WFInfo.exe'
    # WinExe needs an explicit wait. A unique result path prevents stale-result success.
    $process = Start-Process -FilePath $exe -ArgumentList @('--test', ('"' + $map + '"'), ('"' + $output + '"')) -Wait -PassThru
    if (!(Test-Path $output)) { throw "No results produced; exit code $($process.ExitCode). Check %APPDATA%\WFInfo\debug.log." }
    $results = Get-Content $output -Raw -Encoding UTF8 | ConvertFrom-Json
    $results | Select-Object TotalTests, PassedTests, FailedTests, ErrorTests, OverallAccuracy | Format-List
    Write-Host "Results: $output"
    # Export only debug images from this run, not unrelated screenshots or the entire user log.
    $diagnostics = [IO.Path]::ChangeExtension($output, $null) + '-diagnostics'
    New-Item -ItemType Directory -Path $diagnostics | Out-Null
    Copy-Item -LiteralPath $output -Destination (Join-Path $diagnostics 'results.json')
    $debugRoot = Join-Path $env:APPDATA 'WFInfo\Debug'
    foreach ($case in $results.TestResults) {
        if ($case.DebugImageId -and (Test-Path $debugRoot)) {
            Get-ChildItem -LiteralPath $debugRoot -File -Filter '*.png' |
                Where-Object { $_.BaseName.EndsWith('_' + $case.DebugImageId) } |
                Copy-Item -Destination $diagnostics
        }
    }
    $archive = $diagnostics + '.zip'
    Compress-Archive -Path (Join-Path $diagnostics '*') -DestinationPath $archive
    Write-Host "Diagnostics ZIP: $archive"

    if ($process.ExitCode -ne 0 -or $results.TotalTests -ne 6 -or $results.PassedTests -ne 6 -or
        $results.FailedTests -ne 0 -or $results.ErrorTests -ne 0 -or $results.ErrorMessage) {
        throw 'Japanese reward OCR regression failed. Inspect the result JSON and debug.log.'
    }
} finally { Pop-Location }

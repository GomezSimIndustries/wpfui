<#
.SYNOPSIS
    Packs the fork's core packable projects into an isolated output directory and reports
    each package's declared readme/icon/license, identity, provenance and framework/
    dependency metadata. Never pushes to any registry.

.DESCRIPTION
    Runs dotnet pack for each requested project with its own output directory, exits
    nonzero the moment a pack step fails, and otherwise inspects every produced .nupkg for
    the files PackageReadmeFile/PackageIcon declare. Artifact creation only; publishing a
    package to a feed is out of scope and this script never calls nuget or dotnet nuget
    push.

.PARAMETER OutputDirectory
    Directory the packages are written to. Created if missing. Defaults to a directory
    that is unique to this invocation so concurrent runs never collide.

.PARAMETER PackageVersionOverride
    Version string forced onto every produced package. Defaults to the repository's
    declared Version suffixed with the current commit, so a package produced by this
    script never reads as an official upstream release. Pass an empty string to pack with
    the checked-in Version unchanged.

.EXAMPLE
    pwsh -File pack-and-inspect.ps1
    pwsh -File pack-and-inspect.ps1 -OutputDirectory C:\temp\wpfui-pack
#>
[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path ([System.IO.Path]::GetTempPath()) "wpfui-pack-inspect-$([Guid]::NewGuid().ToString('N'))"),

    [string]$PackageVersionOverride = $(
        $shortSha = & git -C $PSScriptRoot rev-parse --short HEAD 2>$null
        $propsPath = Join-Path $PSScriptRoot 'Directory.Build.props'
        if ($LASTEXITCODE -eq 0 -and $shortSha -and (Test-Path -LiteralPath $propsPath)) {
            $propsContent = Get-Content -LiteralPath $propsPath -Raw
            if ($propsContent -match '<Version>([^<]+)</Version>') {
                "$($Matches[1])-private.$shortSha"
            }
        }
    )
)

Import-Module (Join-Path $PSScriptRoot 'build\PackAndInspect.psm1') -Force

if (-not (Test-Path -LiteralPath $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}

Write-Host "Packing into $OutputDirectory"
Write-Host "Package version override: $(if ($PackageVersionOverride) { $PackageVersionOverride } else { '<none, using the checked-in Version>' })"

$outcome = Invoke-WpfUiPackAndInspect -RepositoryRoot $PSScriptRoot -OutputDirectory $OutputDirectory -PackageVersionOverride $PackageVersionOverride

foreach ($projectResult in $outcome.Results) {
    Write-Host ''
    Write-Host "== $($projectResult.Project) =="

    if ($projectResult.PackExitCode -ne 0) {
        Write-Host "dotnet pack failed with exit code $($projectResult.PackExitCode)."
        continue
    }

    $metadata = $projectResult.Metadata

    if (-not $metadata) {
        Write-Host 'No .nupkg was produced.'
        continue
    }

    Write-Host "Id: $($metadata.Id)"
    Write-Host "Version: $($metadata.Version)"
    Write-Host "Authors: $($metadata.Authors)"
    Write-Host "License: $($metadata.License) ($($metadata.LicenseType))"
    Write-Host "Repository: $($metadata.RepositoryUrl) @ $($metadata.RepositoryCommit)"

    foreach ($group in $metadata.Dependencies) {
        Write-Host "Dependencies [$($group.TargetFramework)]: $($group.Packages -join ', ')"
    }

    Write-Host "Package set: $($metadata.Entries -join ', ')"

    if ($projectResult.Missing.Count -gt 0) {
        Write-Host "Missing declared files: $($projectResult.Missing -join ', ')"
    } else {
        Write-Host 'All declared files present.'
    }
}

Write-Host ''
Write-Host "Exit code: $($outcome.ExitCode)"

exit $outcome.ExitCode

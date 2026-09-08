<#
.SYNOPSIS
    Packs the fork's packable core projects into an isolated output directory and inspects
    each produced .nupkg for its declared readme, icon, license, identity and
    framework/dependency metadata, with exit codes propagated and no registry ever called.
#>

function Invoke-WpfUiPack {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ProjectPath,

        [Parameter(Mandatory)]
        [string]$OutputDirectory,

        [string]$PackageVersionOverride,

        [scriptblock]$Invoke = {
            param($packArguments)
            & dotnet @packArguments | Out-Host
            return $LASTEXITCODE
        }
    )

    $packArguments = @('pack', $ProjectPath, '-c', 'Release', '-o', $OutputDirectory, '--nologo')

    if ($PackageVersionOverride) {
        $packArguments += "-p:Version=$PackageVersionOverride"
    }

    return & $Invoke $packArguments
}

function Get-PackedNuspec {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$PackagePath
    )

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        $nuspecEntry = $archive.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
        if (-not $nuspecEntry) {
            throw "No .nuspec entry found in $PackagePath."
        }

        $reader = New-Object System.IO.StreamReader($nuspecEntry.Open())
        try {
            $nuspecXml = [xml]$reader.ReadToEnd()
        } finally {
            $reader.Dispose()
        }

        $metadataNode = $nuspecXml.package.metadata
        $dependencyGroups = @($metadataNode.dependencies.group | Where-Object { $_ } | ForEach-Object {
            [PSCustomObject]@{
                TargetFramework = $_.targetFramework
                Packages        = @($_.dependency | Where-Object { $_ } | ForEach-Object { "$($_.id) $($_.version)" })
            }
        })

        [PSCustomObject]@{
            Id                = $metadataNode.id
            Version           = $metadataNode.version
            Authors           = $metadataNode.authors
            License           = $metadataNode.license.'#text'
            LicenseType       = $metadataNode.license.type
            RepositoryUrl     = $metadataNode.repository.url
            RepositoryType    = $metadataNode.repository.type
            RepositoryCommit  = $metadataNode.repository.commit
            Dependencies      = $dependencyGroups
            Entries           = @($archive.Entries | ForEach-Object { $_.FullName })
        }
    } finally {
        $archive.Dispose()
    }
}

function Test-PackageDeclaresFile {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string[]]$Entries,

        [Parameter(Mandatory)]
        [string]$FileName
    )

    return $Entries -contains $FileName
}

function Invoke-WpfUiPackAndInspect {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$RepositoryRoot,

        [Parameter(Mandatory)]
        [string]$OutputDirectory,

        [string[]]$ProjectPaths = @(
            'src\Wpf.Ui\Wpf.Ui.csproj',
            'src\Wpf.Ui.Abstractions\Wpf.Ui.Abstractions.csproj',
            'src\Wpf.Ui.DependencyInjection\Wpf.Ui.DependencyInjection.csproj',
            'src\Wpf.Ui.Tray\Wpf.Ui.Tray.csproj'
        ),

        [string[]]$RequiredEntries = @('README.md', 'Icon.png'),

        [string]$PackageVersionOverride
    )

    $results = New-Object System.Collections.Generic.List[PSCustomObject]
    $exitCode = 0

    foreach ($relativeProjectPath in $ProjectPaths) {
        $projectPath = Join-Path $RepositoryRoot $relativeProjectPath
        $packExitCode = Invoke-WpfUiPack -ProjectPath $projectPath -OutputDirectory $OutputDirectory -PackageVersionOverride $PackageVersionOverride

        if ($packExitCode -ne 0) {
            $results.Add([PSCustomObject]@{
                Project      = $relativeProjectPath
                PackExitCode = $packExitCode
                Missing      = @()
                Metadata     = $null
            })
            $exitCode = $packExitCode
            continue
        }

        # The .nupkg file name follows PackageId, not the project file name.
        $package = Get-ChildItem -LiteralPath $OutputDirectory -Filter '*.nupkg' |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1

        if (-not $package) {
            $results.Add([PSCustomObject]@{
                Project      = $relativeProjectPath
                PackExitCode = 0
                Missing      = @('<no .nupkg produced>')
                Metadata     = $null
            })
            $exitCode = 1
            continue
        }

        $metadata = Get-PackedNuspec -PackagePath $package.FullName
        $missing = @($RequiredEntries | Where-Object { -not (Test-PackageDeclaresFile -Entries $metadata.Entries -FileName $_) })

        if ($missing.Count -gt 0) {
            $exitCode = 1
        }

        $results.Add([PSCustomObject]@{
            Project      = $relativeProjectPath
            PackExitCode = 0
            Missing      = $missing
            Metadata     = $metadata
        })
    }

    [PSCustomObject]@{
        ExitCode = $exitCode
        Results  = $results
    }
}

Export-ModuleMember -Function Invoke-WpfUiPack, Get-PackedNuspec, Test-PackageDeclaresFile, Invoke-WpfUiPackAndInspect

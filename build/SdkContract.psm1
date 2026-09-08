<#
.SYNOPSIS
    Reads the .NET SDK contract from global.json and drives a restore-then-build with
    exit codes propagated and SDK installation kept opt-in.
#>

function Get-RequiredDotnetSdk {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "SDK contract file not found at $Path."
    }

    $contract = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $version = $contract.sdk.version

    if (-not $version) {
        throw "$Path does not declare sdk.version."
    }

    [PSCustomObject]@{
        Version = $version
        Major   = [int]($version.Split('.')[0])
    }
}

function Get-InstalledDotnetSdkVersions {
    [CmdletBinding()]
    param(
        [scriptblock]$Invoke = {
            $sdkListLines = & dotnet --list-sdks 2>$null
            [PSCustomObject]@{ Lines = $sdkListLines; ExitCode = $LASTEXITCODE }
        }
    )

    $result = & $Invoke

    if ($result.ExitCode -ne 0 -or -not $result.Lines) {
        return @()
    }

    $result.Lines | ForEach-Object {
        if ($_ -match '^(\S+)\s+\[') { $Matches[1] }
    }
}

function Test-DotnetSdkPresent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [int]$RequiredMajor,

        [string[]]$InstalledVersions = @()
    )

    foreach ($installed in $InstalledVersions) {
        $major = ($installed -split '\.')[0] -as [int]
        if ($major -eq $RequiredMajor) { return $true }
    }

    return $false
}

function Invoke-DotnetStep {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [scriptblock]$Invoke = {
            param($stepArguments)
            & dotnet @stepArguments
            return $LASTEXITCODE
        }
    )

    return & $Invoke $Arguments
}

function Install-MissingSdk {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Version
    )

    $major = ($Version -split '\.')[0]
    & winget install "Microsoft.DotNet.SDK.$major" --accept-package-agreements --accept-source-agreements
    return $LASTEXITCODE
}

function Invoke-WpfUiBuild {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$GlobalJsonPath,

        [string]$SolutionPath = 'Wpf.Ui.sln',

        [string]$GalleryProjectPath = 'src\Wpf.Ui.Gallery\Wpf.Ui.Gallery.csproj',

        [switch]$InstallMissingSdk
    )

    $required = Get-RequiredDotnetSdk -Path $GlobalJsonPath
    $installed = Get-InstalledDotnetSdkVersions

    if (-not (Test-DotnetSdkPresent -RequiredMajor $required.Major -InstalledVersions $installed)) {
        if ($InstallMissingSdk) {
            Install-MissingSdk -Version $required.Version | Out-Null
            $installed = Get-InstalledDotnetSdkVersions
        }

        if (-not (Test-DotnetSdkPresent -RequiredMajor $required.Major -InstalledVersions $installed)) {
            Write-Error "No .NET SDK $($required.Major).x is installed (contract $GlobalJsonPath requires $($required.Version)). Installed: $($installed -join ', '). Pass -InstallMissingSdk to install it, or install it manually."
            return 1
        }
    }

    $restoreExit = Invoke-DotnetStep -Arguments @('restore', $SolutionPath, '/tl')
    if ($restoreExit -ne 0) {
        Write-Error "dotnet restore failed with exit code $restoreExit."
        return $restoreExit
    }

    $buildExit = Invoke-DotnetStep -Arguments @('build', $GalleryProjectPath, '--configuration', 'Release', '--no-restore', '--verbosity', 'quiet', '/tl')
    if ($buildExit -ne 0) {
        Write-Error "dotnet build failed with exit code $buildExit."
        return $buildExit
    }

    return 0
}

Export-ModuleMember -Function Get-RequiredDotnetSdk, Get-InstalledDotnetSdkVersions, Test-DotnetSdkPresent, Invoke-DotnetStep, Install-MissingSdk, Invoke-WpfUiBuild

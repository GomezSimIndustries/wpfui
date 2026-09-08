<#
.SYNOPSIS
    Verifies the .NET SDK contract in global.json, then restores and builds the gallery
    project.

.DESCRIPTION
    Checks the installed .NET SDKs against global.json and exits nonzero without building
    when the required major version is missing, a restore fails, or a build fails.
    Installing the SDK is opt-in via -InstallMissingSdk; it is never triggered
    automatically.

.PARAMETER InstallMissingSdk
    Installs the required .NET SDK with winget when the prerequisite check fails.

.EXAMPLE
    pwsh -File build.ps1
    pwsh -File build.ps1 -InstallMissingSdk
#>
[CmdletBinding()]
param(
    [switch]$InstallMissingSdk
)

Import-Module (Join-Path $PSScriptRoot 'build\SdkContract.psm1') -Force

if ($env:PROCESSOR_ARCHITECTURE -ne 'AMD64') {
    Write-Host 'Not in the x64 desktop environment.'
    exit 0
}

$exitCode = Invoke-WpfUiBuild -GlobalJsonPath (Join-Path $PSScriptRoot 'global.json') -InstallMissingSdk:$InstallMissingSdk
exit $exitCode

$moduleRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $moduleRoot 'SdkContract.psm1') -Force

Describe 'Get-RequiredDotnetSdk' {
    It 'reads the major version out of a global.json contract' {
        $path = Join-Path $TestDrive 'global.json'
        Set-Content -LiteralPath $path -Value '{"sdk":{"version":"10.0.100","rollForward":"latestFeature"}}'

        $result = Get-RequiredDotnetSdk -Path $path

        $result.Major | Should Be 10
        $result.Version | Should Be '10.0.100'
    }

    It 'throws when the contract file is missing' {
        $threw = $false
        try {
            Get-RequiredDotnetSdk -Path (Join-Path $TestDrive 'missing.json')
        } catch {
            $threw = $true
        }

        $threw | Should Be $true
    }
}

Describe 'Test-DotnetSdkPresent' {
    It 'returns true when an installed SDK matches the required major version' {
        Test-DotnetSdkPresent -RequiredMajor 10 -InstalledVersions @('9.0.317', '10.0.303') | Should Be $true
    }

    It 'returns false when no installed SDK matches the required major version' {
        Test-DotnetSdkPresent -RequiredMajor 10 -InstalledVersions @('8.0.404', '9.0.317') | Should Be $false
    }

    It 'returns false when no SDKs are installed' {
        Test-DotnetSdkPresent -RequiredMajor 10 -InstalledVersions @() | Should Be $false
    }
}

Describe 'Get-InstalledDotnetSdkVersions' {
    It 'parses version numbers out of dotnet --list-sdks output' {
        $fakeInvoke = {
            [PSCustomObject]@{
                Lines    = @('9.0.317 [C:\Program Files\dotnet\sdk]', '10.0.303 [C:\Program Files\dotnet\sdk]')
                ExitCode = 0
            }
        }

        $versions = @(Get-InstalledDotnetSdkVersions -Invoke $fakeInvoke)

        $versions.Count | Should Be 2
        ($versions -contains '10.0.303') | Should Be $true
    }

    It 'returns an empty list when dotnet exits nonzero' {
        $fakeInvoke = { [PSCustomObject]@{ Lines = @(); ExitCode = 1 } }

        $versions = @(Get-InstalledDotnetSdkVersions -Invoke $fakeInvoke)

        $versions.Count | Should Be 0
    }
}

Describe 'Invoke-DotnetStep' {
    It 'returns the exit code produced by the step' {
        $fakeInvoke = { param($stepArguments) 1 }

        $exitCode = Invoke-DotnetStep -Arguments @('restore', 'Wpf.Ui.sln') -Invoke $fakeInvoke

        $exitCode | Should Be 1
    }

    It 'returns zero when the step succeeds' {
        $fakeInvoke = { param($stepArguments) 0 }

        $exitCode = Invoke-DotnetStep -Arguments @('restore', 'Wpf.Ui.sln') -Invoke $fakeInvoke

        $exitCode | Should Be 0
    }
}

Describe 'Invoke-WpfUiBuild' {
    It 'selects a supported SDK, restores and builds without installing anything' {
        Mock Get-RequiredDotnetSdk -ModuleName SdkContract { [PSCustomObject]@{ Version = '10.0.100'; Major = 10 } }
        Mock Get-InstalledDotnetSdkVersions -ModuleName SdkContract { @('9.0.317', '10.0.303') }
        Mock Invoke-DotnetStep -ModuleName SdkContract { 0 }
        Mock Install-MissingSdk -ModuleName SdkContract { 0 }

        $result = Invoke-WpfUiBuild -GlobalJsonPath 'unused.json'

        $result | Should Be 0
        Assert-MockCalled Invoke-DotnetStep -ModuleName SdkContract -Times 2 -Exactly -Scope It
        Assert-MockCalled Install-MissingSdk -ModuleName SdkContract -Times 0 -Exactly -Scope It
    }

    It 'fails clearly without installing when the required SDK is missing' {
        Mock Get-RequiredDotnetSdk -ModuleName SdkContract { [PSCustomObject]@{ Version = '10.0.100'; Major = 10 } }
        Mock Get-InstalledDotnetSdkVersions -ModuleName SdkContract { @('8.0.404') }
        Mock Invoke-DotnetStep -ModuleName SdkContract { 0 }
        Mock Install-MissingSdk -ModuleName SdkContract { 0 }

        $result = Invoke-WpfUiBuild -GlobalJsonPath 'unused.json' -ErrorAction SilentlyContinue

        $result | Should Be 1
        Assert-MockCalled Install-MissingSdk -ModuleName SdkContract -Times 0 -Exactly -Scope It
        Assert-MockCalled Invoke-DotnetStep -ModuleName SdkContract -Times 0 -Exactly -Scope It
    }

    It 'installs the SDK only when explicitly requested' {
        Mock Get-RequiredDotnetSdk -ModuleName SdkContract { [PSCustomObject]@{ Version = '10.0.100'; Major = 10 } }
        Mock Get-InstalledDotnetSdkVersions -ModuleName SdkContract { @('8.0.404') }
        Mock Install-MissingSdk -ModuleName SdkContract { 0 }
        Mock Invoke-DotnetStep -ModuleName SdkContract { 0 }

        $result = Invoke-WpfUiBuild -GlobalJsonPath 'unused.json' -InstallMissingSdk -ErrorAction SilentlyContinue

        Assert-MockCalled Install-MissingSdk -ModuleName SdkContract -Times 1 -Exactly -Scope It
    }

    It 'returns a failing restore exit code without attempting a build' {
        Mock Get-RequiredDotnetSdk -ModuleName SdkContract { [PSCustomObject]@{ Version = '10.0.100'; Major = 10 } }
        Mock Get-InstalledDotnetSdkVersions -ModuleName SdkContract { @('10.0.303') }
        Mock Invoke-DotnetStep -ModuleName SdkContract -ParameterFilter { $Arguments[0] -eq 'restore' } { 1 }
        Mock Invoke-DotnetStep -ModuleName SdkContract -ParameterFilter { $Arguments[0] -eq 'build' } { 0 }

        $result = Invoke-WpfUiBuild -GlobalJsonPath 'unused.json' -ErrorAction SilentlyContinue

        $result | Should Be 1
        Assert-MockCalled Invoke-DotnetStep -ModuleName SdkContract -ParameterFilter { $Arguments[0] -eq 'build' } -Times 0 -Exactly -Scope It
    }

    It 'returns a failing build exit code' {
        Mock Get-RequiredDotnetSdk -ModuleName SdkContract { [PSCustomObject]@{ Version = '10.0.100'; Major = 10 } }
        Mock Get-InstalledDotnetSdkVersions -ModuleName SdkContract { @('10.0.303') }
        Mock Invoke-DotnetStep -ModuleName SdkContract -ParameterFilter { $Arguments[0] -eq 'restore' } { 0 }
        Mock Invoke-DotnetStep -ModuleName SdkContract -ParameterFilter { $Arguments[0] -eq 'build' } { 2 }

        $result = Invoke-WpfUiBuild -GlobalJsonPath 'unused.json' -ErrorAction SilentlyContinue

        $result | Should Be 2
    }
}

Describe 'SDK contract alignment' {
    $repoRoot = Split-Path -Parent $moduleRoot
    $globalJsonPath = Join-Path $repoRoot 'global.json'
    $devcontainerPath = Join-Path $repoRoot '.devcontainer\devcontainer.json'

    It 'devcontainer.json targets the same major SDK as global.json' {
        $required = Get-RequiredDotnetSdk -Path $globalJsonPath
        $devcontainer = Get-Content -LiteralPath $devcontainerPath -Raw | ConvertFrom-Json

        $devcontainer.image | Should Match "sdk:$($required.Major)\."
    }
}

$moduleRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $moduleRoot 'PackAndInspect.psm1') -Force

function New-FixturePackage {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [string]$Id = 'WPF-UI',
        [string]$Version = '4.2.0',
        [string[]]$ExtraEntries = @('README.md', 'Icon.png')
    )

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Force
    }

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $nuspec = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    <id>$Id</id>
    <version>$Version</version>
    <authors>lepo.co</authors>
    <license type="expression">MIT</license>
    <repository type="git" url="https://example.invalid/fork" commit="deadbeef" />
    <dependencies>
      <group targetFramework="net10.0-windows">
        <dependency id="System.Memory" version="4.5.5" exclude="Build,Analyzers" />
      </group>
    </dependencies>
  </metadata>
</package>
"@

    $archive = [System.IO.Compression.ZipFile]::Open($Path, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        $nuspecEntry = $archive.CreateEntry("$Id.nuspec")
        $writer = New-Object System.IO.StreamWriter($nuspecEntry.Open())
        try { $writer.Write($nuspec) } finally { $writer.Dispose() }

        foreach ($entryName in $ExtraEntries) {
            $entry = $archive.CreateEntry($entryName)
            $writer = New-Object System.IO.StreamWriter($entry.Open())
            try { $writer.Write('fixture') } finally { $writer.Dispose() }
        }
    } finally {
        $archive.Dispose()
    }
}

Describe 'Get-PackedNuspec' {
    It 'reads identity, provenance and dependency metadata out of a real .nupkg' {
        $packagePath = Join-Path $TestDrive 'WPF-UI.4.2.0.nupkg'
        New-FixturePackage -Path $packagePath

        $metadata = Get-PackedNuspec -PackagePath $packagePath

        $metadata.Id | Should Be 'WPF-UI'
        $metadata.Version | Should Be '4.2.0'
        $metadata.Authors | Should Be 'lepo.co'
        $metadata.License | Should Be 'MIT'
        $metadata.RepositoryUrl | Should Be 'https://example.invalid/fork'
        $metadata.RepositoryCommit | Should Be 'deadbeef'
        $metadata.Dependencies.Count | Should Be 1
        $metadata.Dependencies[0].TargetFramework | Should Be 'net10.0-windows'
        ($metadata.Dependencies[0].Packages -contains 'System.Memory 4.5.5') | Should Be $true
        ($metadata.Entries -contains 'README.md') | Should Be $true
        ($metadata.Entries -contains 'Icon.png') | Should Be $true
    }

    It 'reports the intended package set as the full entry list' {
        $packagePath = Join-Path $TestDrive 'WPF-UI.Abstractions.4.2.0.nupkg'
        New-FixturePackage -Path $packagePath -Id 'WPF-UI.Abstractions' -ExtraEntries @('README.md', 'Icon.png', 'lib\net10.0\WPF-UI.Abstractions.dll')

        $metadata = Get-PackedNuspec -PackagePath $packagePath

        ($metadata.Entries -contains 'lib\net10.0\WPF-UI.Abstractions.dll') | Should Be $true
    }
}

Describe 'Invoke-WpfUiPack' {
    It 'returns a single exit code even when the packed command writes multiple output lines' {
        $exitCode = Invoke-WpfUiPack -ProjectPath (Join-Path $TestDrive 'missing.csproj') -OutputDirectory $TestDrive

        $exitCode.GetType().IsArray | Should Be $false
        $exitCode | Should BeOfType ([int])
        $exitCode | Should Not Be 0
    }

    It 'overrides the package version so a local package can never read as an official release' {
        $script:capturedArguments = $null
        $fakeInvoke = { param($packArguments) $script:capturedArguments = $packArguments; return 0 }

        Invoke-WpfUiPack -ProjectPath 'C:\repo\src\Wpf.Ui\Wpf.Ui.csproj' -OutputDirectory $TestDrive -PackageVersionOverride '4.2.0-private.abc1234' -Invoke $fakeInvoke

        ($script:capturedArguments -contains '-p:Version=4.2.0-private.abc1234') | Should Be $true
    }

    It 'omits the version override argument when none is given' {
        $script:capturedArguments = $null
        $fakeInvoke = { param($packArguments) $script:capturedArguments = $packArguments; return 0 }

        Invoke-WpfUiPack -ProjectPath 'C:\repo\src\Wpf.Ui\Wpf.Ui.csproj' -OutputDirectory $TestDrive -Invoke $fakeInvoke

        ($script:capturedArguments | Where-Object { $_ -like '-p:Version=*' }).Count | Should Be 0
    }
}

Describe 'Test-PackageDeclaresFile' {
    It 'returns true when the file is present among the package entries' {
        Test-PackageDeclaresFile -Entries @('README.md', 'Icon.png') -FileName 'README.md' | Should Be $true
    }

    It 'returns false when the file is missing from the package entries' {
        Test-PackageDeclaresFile -Entries @('Icon.png') -FileName 'README.md' | Should Be $false
    }
}

Describe 'Invoke-WpfUiPackAndInspect' {
    It 'packs every requested project and reports zero missing files when packing succeeds' {
        Mock Invoke-WpfUiPack -ModuleName PackAndInspect { 0 }
        Mock Get-PackedNuspec -ModuleName PackAndInspect {
            [PSCustomObject]@{
                Id = 'WPF-UI'; Version = '4.2.0'; Entries = @('README.md', 'Icon.png')
            }
        }
        Mock Get-ChildItem -ModuleName PackAndInspect { [PSCustomObject]@{ FullName = 'fake.nupkg' } }

        $result = Invoke-WpfUiPackAndInspect -RepositoryRoot $TestDrive -OutputDirectory $TestDrive -ProjectPaths @('src\Wpf.Ui\Wpf.Ui.csproj')

        $result.ExitCode | Should Be 0
        $result.Results.Count | Should Be 1
        $result.Results[0].Missing.Count | Should Be 0
    }

    It 'propagates a nonzero pack exit code without treating it as success' {
        Mock Invoke-WpfUiPack -ModuleName PackAndInspect { 1 }
        Mock Get-PackedNuspec -ModuleName PackAndInspect { throw 'must not inspect a package that was never produced' }

        $result = Invoke-WpfUiPackAndInspect -RepositoryRoot $TestDrive -OutputDirectory $TestDrive -ProjectPaths @('src\Wpf.Ui\Wpf.Ui.csproj')

        $result.ExitCode | Should Be 1
        $result.Results[0].PackExitCode | Should Be 1
    }

    It 'fails the run when a declared file is missing from the produced package' {
        Mock Invoke-WpfUiPack -ModuleName PackAndInspect { 0 }
        Mock Get-PackedNuspec -ModuleName PackAndInspect {
            [PSCustomObject]@{ Id = 'WPF-UI'; Version = '4.2.0'; Entries = @('Icon.png') }
        }
        Mock Get-ChildItem -ModuleName PackAndInspect { [PSCustomObject]@{ FullName = 'fake.nupkg' } }

        $result = Invoke-WpfUiPackAndInspect -RepositoryRoot $TestDrive -OutputDirectory $TestDrive -ProjectPaths @('src\Wpf.Ui\Wpf.Ui.csproj') -RequiredEntries @('README.md', 'Icon.png')

        $result.ExitCode | Should Be 1
        ($result.Results[0].Missing -contains 'README.md') | Should Be $true
    }

    It 'forwards the version override to every pack step' {
        Mock Invoke-WpfUiPack -ModuleName PackAndInspect { 0 }
        Mock Get-PackedNuspec -ModuleName PackAndInspect {
            [PSCustomObject]@{ Id = 'WPF-UI'; Version = '4.2.0-private.abc1234'; Entries = @('README.md', 'Icon.png') }
        }
        Mock Get-ChildItem -ModuleName PackAndInspect { [PSCustomObject]@{ FullName = 'fake.nupkg' } }

        Invoke-WpfUiPackAndInspect -RepositoryRoot $TestDrive -OutputDirectory $TestDrive -ProjectPaths @('src\Wpf.Ui\Wpf.Ui.csproj') -PackageVersionOverride '4.2.0-private.abc1234' | Out-Null

        Assert-MockCalled Invoke-WpfUiPack -ModuleName PackAndInspect -ParameterFilter { $PackageVersionOverride -eq '4.2.0-private.abc1234' } -Times 1 -Exactly -Scope It
    }
}

Describe 'PackAndInspect module source' {
    It 'never calls a registry push' {
        $moduleSource = Get-Content -LiteralPath (Join-Path $moduleRoot 'PackAndInspect.psm1') -Raw

        $moduleSource | Should Not Match 'nuget push|dotnet nuget push'
    }
}

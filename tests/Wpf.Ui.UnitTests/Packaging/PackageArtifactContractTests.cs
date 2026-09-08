// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Wpf.Ui.UnitTests.Packaging;

public sealed class PackageArtifactContractTests
{
    private static readonly TimeSpan PackHangGuard = TimeSpan.FromMinutes(5);

    [Fact]
    public void Pack_ProducesPackageContainingDeclaredReadmeAndIcon()
    {
        var repositoryRoot = FindRepositoryRoot();
        var outputDirectory = Path.Combine(
            Path.GetTempPath(),
            $"wpfui-pack-{nameof(Pack_ProducesPackageContainingDeclaredReadmeAndIcon)}-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(outputDirectory);

        try
        {
            var packResult = RunDotnetPack(repositoryRoot, outputDirectory);

            Assert.True(packResult.ExitCode == 0, $"dotnet pack exited {packResult.ExitCode}:\n{packResult.Output}");

            var package = Assert.Single(Directory.GetFiles(outputDirectory, "*.nupkg"));

            using var archive = ZipFile.OpenRead(package);
            var entryNames = archive.Entries.Select(entry => entry.FullName).ToArray();

            Assert.Contains("README.md", entryNames);
            Assert.Contains("Icon.png", entryNames);
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }

    private static (int ExitCode, string Output) RunDotnetPack(string repositoryRoot, string outputDirectory)
    {
        var projectPath = Path.Combine(repositoryRoot, "src", "Wpf.Ui", "Wpf.Ui.csproj");

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("pack");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(outputDirectory);
        startInfo.ArgumentList.Add("--nologo");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("dotnet pack did not start.");

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit((int)PackHangGuard.TotalMilliseconds))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"dotnet pack did not exit within {PackHangGuard}.");
        }

        var output = stdOutTask.GetAwaiter().GetResult() + stdErrTask.GetAwaiter().GetResult();

        return (process.ExitCode, output);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Wpf.Ui.sln")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException(
                "Could not locate the repository root above the test output directory.");
        }

        return directory.FullName;
    }
}

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Wpf.Ui.FontMapper;

namespace Wpf.Ui.UnitTests.FontMapper;

public sealed class FontMapperGeneratorTests : IDisposable
{
    private readonly string _testRoot;
    private readonly string _fontsRoot;
    private readonly string _outputDirectory;
    private readonly string _regularJsonPath;
    private readonly string _filledJsonPath;

    public FontMapperGeneratorTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "FontMapperGeneratorTests_" + Guid.NewGuid().ToString("N"));
        _fontsRoot = Path.Combine(_testRoot, "fonts");
        _outputDirectory = Path.Combine(_testRoot, "generated");
        Directory.CreateDirectory(_fontsRoot);
        Directory.CreateDirectory(_outputDirectory);

        File.WriteAllBytes(Path.Combine(_fontsRoot, "FluentSystemIcons-Regular.ttf"), new byte[] { 0 });
        File.WriteAllBytes(Path.Combine(_fontsRoot, "FluentSystemIcons-Filled.ttf"), new byte[] { 0 });

        _regularJsonPath = Path.Combine(_testRoot, "Regular.json");
        _filledJsonPath = Path.Combine(_testRoot, "Filled.json");

        File.WriteAllText(
            _regularJsonPath,
            """
            {
              "ic_fluent_add_circle_regular": 61441,
              "ic_fluent_alert_regular": 61443,
              "ic_fluent_only_in_regular_regular": 61444
            }
            """);

        File.WriteAllText(
            _filledJsonPath,
            """
            {
              "ic_fluent_add_circle_filled": 61441,
              "ic_fluent_alert_filled": 61443,
              "ic_fluent_only_in_filled_filled": 61445
            }
            """);
    }

    public void Dispose()
    {
        Directory.Delete(_testRoot, recursive: true);
    }

    [Fact]
    public async Task GenerateAsync_SamePinnedFixtureRunTwice_ProducesByteIdenticalOutput()
    {
        using var neverCalledClient = new HttpClient(new ThrowingHandler());

        var first = await FontMapperGenerator.GenerateAsync(BuildLocalOptions(), neverCalledClient);
        var firstRegular = await File.ReadAllBytesAsync(first.RegularFilePath);
        var firstFilled = await File.ReadAllBytesAsync(first.FilledFilePath);

        var second = await FontMapperGenerator.GenerateAsync(BuildLocalOptions(), neverCalledClient);
        var secondRegular = await File.ReadAllBytesAsync(second.RegularFilePath);
        var secondFilled = await File.ReadAllBytesAsync(second.FilledFilePath);

        Assert.Equal(firstRegular, secondRegular);
        Assert.Equal(firstFilled, secondFilled);
    }

    [Fact]
    public async Task GenerateAsync_LocalFixture_NeverCallsNetwork()
    {
        using var throwingClient = new HttpClient(new ThrowingHandler());

        var result = await FontMapperGenerator.GenerateAsync(BuildLocalOptions(), throwingClient);

        Assert.True(File.Exists(result.RegularFilePath));
        Assert.True(File.Exists(result.FilledFilePath));
    }

    [Fact]
    public async Task GenerateAsync_BothSymbolSetsKeepOnlySharedIconsMappedToBundledFonts()
    {
        using var throwingClient = new HttpClient(new ThrowingHandler());

        var result = await FontMapperGenerator.GenerateAsync(BuildLocalOptions(), throwingClient);
        var regularSource = await File.ReadAllTextAsync(result.RegularFilePath);
        var filledSource = await File.ReadAllTextAsync(result.FilledFilePath);

        Assert.Contains("AddCircle = 0xF001,", regularSource);
        Assert.Contains("AddCircle = 0xF001,", filledSource);
        Assert.DoesNotContain("OnlyInRegular", regularSource);
        Assert.DoesNotContain("OnlyInFilled", filledSource);
    }

    [Fact]
    public async Task GenerateAsync_MissingPackagedFontForSymbolSet_FailsWithClearMessage()
    {
        File.Delete(Path.Combine(_fontsRoot, "FluentSystemIcons-Filled.ttf"));
        using var throwingClient = new HttpClient(new ThrowingHandler());

        var exception = await Assert.ThrowsAsync<FontMapperInputException>(
            () => FontMapperGenerator.GenerateAsync(BuildLocalOptions(), throwingClient));

        Assert.Contains("FluentSystemIcons-Filled.ttf", exception.Message);
    }

    [Fact]
    public async Task GenerateAsync_MovingUpstreamRevision_FailsWithClearMessage()
    {
        var options = new FontMapperOptions
        {
            Version = "1.2.3-test",
            Revision = "main",
            FontsRootDirectory = _fontsRoot,
            OutputDirectory = _outputDirectory,
        };
        using var throwingClient = new HttpClient(new ThrowingHandler());

        var exception = await Assert.ThrowsAsync<FontMapperInputException>(
            () => FontMapperGenerator.GenerateAsync(options, throwingClient));

        Assert.Contains("pinned commit revision", exception.Message);
    }

    [Fact]
    public async Task GenerateAsync_NeitherLocalInputsNorRevision_FailsWithClearMessage()
    {
        var options = new FontMapperOptions
        {
            Version = "1.2.3-test",
            FontsRootDirectory = _fontsRoot,
            OutputDirectory = _outputDirectory,
        };
        using var throwingClient = new HttpClient(new ThrowingHandler());

        var exception = await Assert.ThrowsAsync<FontMapperInputException>(
            () => FontMapperGenerator.GenerateAsync(options, throwingClient));

        Assert.Contains("local JSON paths", exception.Message);
    }

    [Fact]
    public async Task GenerateAsync_MissingVersion_FailsWithClearMessage()
    {
        var options = new FontMapperOptions
        {
            Version = string.Empty,
            RegularJsonPath = _regularJsonPath,
            FilledJsonPath = _filledJsonPath,
            FontsRootDirectory = _fontsRoot,
            OutputDirectory = _outputDirectory,
        };
        using var throwingClient = new HttpClient(new ThrowingHandler());

        var exception = await Assert.ThrowsAsync<FontMapperInputException>(
            () => FontMapperGenerator.GenerateAsync(options, throwingClient));

        Assert.Contains("version", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private FontMapperOptions BuildLocalOptions() =>
        new()
        {
            Version = "1.2.3-test",
            RegularJsonPath = _regularJsonPath,
            FilledJsonPath = _filledJsonPath,
            FontsRootDirectory = _fontsRoot,
            OutputDirectory = _outputDirectory,
        };

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The network must not be reached for a local fixture run.");
    }
}

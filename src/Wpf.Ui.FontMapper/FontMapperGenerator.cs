// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.FontMapper;

/// <summary>
/// Generates the regular and filled icon enums from one consistent set of inputs.
/// </summary>
public static class FontMapperGenerator
{
    /// <summary>
    /// Validates <paramref name="options"/>, loads both icon sets, and writes the generated enums.
    /// </summary>
    /// <param name="options">The generation inputs.</param>
    /// <param name="httpClient">The client used for an upstream fetch. Not called when local JSON paths are supplied. When omitted, a client is created and disposed internally.</param>
    /// <param name="cancellationToken">A token to cancel generation.</param>
    /// <returns>The generated file paths and the icon names excluded for lacking a counterpart in the other set.</returns>
    public static async Task<FontMapperResult> GenerateAsync(
        FontMapperOptions options,
        HttpClient? httpClient = null,
        CancellationToken cancellationToken = default)
    {
        FontMapperInputValidator.Validate(options);

        using var ownedClient = httpClient is null ? new HttpClient() : null;
        var client = httpClient ?? ownedClient!;

        var regularContents = await IconMetadataLoader
            .LoadAsync(options.RegularJsonPath, options.Revision, IconFontDefinitions.Regular.UpstreamFileName, client, cancellationToken)
            .ConfigureAwait(false);
        var filledContents = await IconMetadataLoader
            .LoadAsync(options.FilledJsonPath, options.Revision, IconFontDefinitions.Filled.UpstreamFileName, client, cancellationToken)
            .ConfigureAwait(false);

        var removedKeys = regularContents.Keys
            .Except(filledContents.Keys)
            .Concat(filledContents.Keys.Except(regularContents.Keys))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        var prunedRegular = OrderForOutput(regularContents, removedKeys);
        var prunedFilled = OrderForOutput(filledContents, removedKeys);

        var regularSource = IconEnumSourceBuilder.Build(
            IconFontDefinitions.Regular.EnumName,
            IconFontDefinitions.Regular.DescriptionTemplate.Replace("{{FLUENT_SYSTEM_ICONS_VERSION}}", options.Version),
            prunedRegular);
        var filledSource = IconEnumSourceBuilder.Build(
            IconFontDefinitions.Filled.EnumName,
            IconFontDefinitions.Filled.DescriptionTemplate.Replace("{{FLUENT_SYSTEM_ICONS_VERSION}}", options.Version),
            prunedFilled);

        var regularPath = Path.Combine(options.OutputDirectory, IconFontDefinitions.Regular.DestinationRelativePath);
        var filledPath = Path.Combine(options.OutputDirectory, IconFontDefinitions.Filled.DestinationRelativePath);

        await WriteFileAsync(regularPath, regularSource, cancellationToken).ConfigureAwait(false);
        await WriteFileAsync(filledPath, filledSource, cancellationToken).ConfigureAwait(false);

        return new FontMapperResult(regularPath, filledPath, options.Version, removedKeys);
    }

    private static IReadOnlyList<KeyValuePair<string, long>> OrderForOutput(
        IReadOnlyDictionary<string, long> contents,
        IReadOnlyCollection<string> removedKeys) =>
        contents
            .Where(pair => !removedKeys.Contains(pair.Key))
            .OrderBy(pair => pair.Value)
            .ToArray();

    private static async Task WriteFileAsync(string path, string content, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, content, cancellationToken).ConfigureAwait(false);
    }
}

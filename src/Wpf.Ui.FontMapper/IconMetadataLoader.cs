// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.FontMapper;

/// <summary>
/// Loads icon metadata from a local file or a pinned upstream revision.
/// </summary>
public static class IconMetadataLoader
{
    private const string UpstreamRepositoryRawRoot =
        "https://raw.githubusercontent.com/microsoft/fluentui-system-icons";

    /// <summary>
    /// Loads and formats icon metadata, ordered by codepoint.
    /// </summary>
    /// <param name="localPath">A local JSON file path, or <see langword="null"/> to fetch from <paramref name="upstreamRevision"/>.</param>
    /// <param name="upstreamRevision">The pinned upstream commit revision, required when <paramref name="localPath"/> is <see langword="null"/>.</param>
    /// <param name="upstreamFileName">The upstream JSON file name for this icon set.</param>
    /// <param name="httpClient">The client used to fetch upstream metadata. Not called when <paramref name="localPath"/> is supplied.</param>
    /// <param name="cancellationToken">A token to cancel the load.</param>
    /// <returns>The formatted icon names mapped to their codepoints, ordered by codepoint.</returns>
    public static async Task<IReadOnlyDictionary<string, long>> LoadAsync(
        string? localPath,
        string? upstreamRevision,
        string upstreamFileName,
        HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        var json = localPath is not null
            ? await File.ReadAllTextAsync(localPath, cancellationToken).ConfigureAwait(false)
            : await FetchUpstreamAsync(upstreamRevision!, upstreamFileName, httpClient, cancellationToken)
                .ConfigureAwait(false);

        Dictionary<string, long>? raw;

        try
        {
            raw = JsonSerializer.Deserialize<Dictionary<string, long>>(json);
        }
        catch (JsonException ex)
        {
            throw new FontMapperInputException($"Unable to parse icon metadata for \"{upstreamFileName}\": {ex.Message}");
        }

        if (raw is null || raw.Count is 0)
        {
            throw new FontMapperInputException($"Icon metadata for \"{upstreamFileName}\" contained no entries.");
        }

        return raw
            .OrderBy(pair => pair.Value)
            .ToDictionary(pair => IconNameFormatter.Format(pair.Key), pair => pair.Value);
    }

    private static async Task<string> FetchUpstreamAsync(
        string revision,
        string fileName,
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        var url = $"{UpstreamRepositoryRawRoot}/{revision}/fonts/{fileName}";

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");

        using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}

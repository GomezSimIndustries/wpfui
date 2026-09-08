// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.FontMapper;

/// <summary>
/// Inputs for one generation run, covering both the regular and filled icon sets.
/// </summary>
public sealed class FontMapperOptions
{
    /// <summary>
    /// Gets the Fluent System Icons release version recorded in the generated enum documentation.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets the pinned upstream commit revision to fetch icon metadata from, or <see langword="null"/> when local paths are supplied instead.
    /// </summary>
    public string? Revision { get; init; }

    /// <summary>
    /// Gets the local path to the regular icon metadata JSON, or <see langword="null"/> to fetch it from <see cref="Revision"/>.
    /// </summary>
    public string? RegularJsonPath { get; init; }

    /// <summary>
    /// Gets the local path to the filled icon metadata JSON, or <see langword="null"/> to fetch it from <see cref="Revision"/>.
    /// </summary>
    public string? FilledJsonPath { get; init; }

    /// <summary>
    /// Gets the directory containing the packaged regular and filled TTF fonts.
    /// </summary>
    public required string FontsRootDirectory { get; init; }

    /// <summary>
    /// Gets the directory the generated enum files are written under.
    /// </summary>
    public required string OutputDirectory { get; init; }
}

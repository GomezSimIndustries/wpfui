// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.FontMapper;

/// <summary>
/// Describes one generated icon enum and the packaged font it is bundled with.
/// </summary>
/// <param name="EnumName">The generated enum type name.</param>
/// <param name="DescriptionTemplate">The XML doc summary template, containing a version placeholder.</param>
/// <param name="DestinationRelativePath">The generated file path, relative to the output directory.</param>
/// <param name="PackagedFontFileName">The packaged TTF file name this enum's glyphs map to.</param>
/// <param name="UpstreamFileName">The upstream JSON file name for this icon set.</param>
public sealed record IconFontDefinition(
    string EnumName,
    string DescriptionTemplate,
    string DestinationRelativePath,
    string PackagedFontFileName,
    string UpstreamFileName);

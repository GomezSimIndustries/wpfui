// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.FontMapper;

/// <summary>
/// Holds the fixed definitions for the regular and filled Fluent System Icons enums.
/// </summary>
public static class IconFontDefinitions
{
    /// <summary>
    /// Gets the definition for the regular icon set.
    /// </summary>
    public static IconFontDefinition Regular { get; } =
        new(
            "SymbolRegular",
            "Represents a list of regular Fluent System Icons <c>v.{{FLUENT_SYSTEM_ICONS_VERSION}}</c>.\n<para>May be converted to <see langword=\"char\"/> using <c>GetGlyph()</c> or to <see langword=\"string\"/> using <c>GetString()</c></para>",
            "generated\\SymbolRegular.cs",
            "FluentSystemIcons-Regular.ttf",
            "FluentSystemIcons-Regular.json");

    /// <summary>
    /// Gets the definition for the filled icon set.
    /// </summary>
    public static IconFontDefinition Filled { get; } =
        new(
            "SymbolFilled",
            "Represents a list of filled Fluent System Icons <c>v.{{FLUENT_SYSTEM_ICONS_VERSION}}</c>.\n<para>May be converted to <see langword=\"char\"/> using <c>GetGlyph()</c> or to <see langword=\"string\"/> using <c>GetString()</c></para>",
            "generated\\SymbolFilled.cs",
            "FluentSystemIcons-Filled.ttf",
            "FluentSystemIcons-Filled.json");
}

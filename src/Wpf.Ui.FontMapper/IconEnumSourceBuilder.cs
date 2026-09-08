// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.FontMapper;

/// <summary>
/// Builds the generated enum source text for one icon set.
/// </summary>
public static class IconEnumSourceBuilder
{
    /// <summary>
    /// Builds the enum source text for the given icons, in the order supplied.
    /// </summary>
    /// <param name="enumName">The enum type name.</param>
    /// <param name="description">The resolved XML doc summary, with the version placeholder already substituted.</param>
    /// <param name="orderedContents">The icon names and codepoints, in the exact order they are written.</param>
    /// <returns>The generated C# source text.</returns>
    public static string Build(string enumName, string description, IReadOnlyList<KeyValuePair<string, long>> orderedContents)
    {
        var enumMapStringBuilder = new StringBuilder();

        _ = enumMapStringBuilder
            .AppendLine("// This Source Code Form is subject to the terms of the MIT License.")
            .AppendLine(
                "// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.")
            .AppendLine("// Copyright (C) Leszek Pomianowski and WPF UI Contributors.")
            .AppendLine("// All Rights Reserved.")
            .AppendLine()
            .AppendLine("namespace Wpf.Ui.Controls;")
            .AppendLine()
            .AppendLine("/// <summary>")
            .AppendLine($"/// {description.Replace("\n", "\n/// ")}")
            .AppendLine("/// </summary>")
            .AppendLine("#pragma warning disable CS1591")
            .AppendLine("public enum " + enumName)
            .AppendLine("{")
            .AppendLine("    /// <summary>")
            .AppendLine("    /// Actually, this icon is not empty, but makes it easier to navigate.")
            .AppendLine("    /// </summary>")
            .AppendLine("    Empty = 0x0,")
            .AppendLine()
            .AppendLine("    // Automatically generated, may contain bugs.")
            .AppendLine();

        foreach (var icon in orderedContents)
        {
            if (icon.Value < 32)
            {
                _ = enumMapStringBuilder
                    .AppendLine()
                    .AppendLine("    /// <summary>")
                    .AppendLine("    /// Blank icon.")
                    .AppendLine("    /// </summary>");
            }

            _ = enumMapStringBuilder.AppendLine($"    {icon.Key} = 0x{icon.Value:X},");
        }

        _ = enumMapStringBuilder
            .AppendLine("}")
            .AppendLine()
            .AppendLine("#pragma warning restore CS1591")
            .Append("\r\n");

        return enumMapStringBuilder.ToString();
    }
}

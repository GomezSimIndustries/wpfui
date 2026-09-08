// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.FontMapper;

/// <summary>
/// Converts raw upstream icon names into enum member names.
/// </summary>
public static class IconNameFormatter
{
    /// <summary>
    /// Converts a raw <c>ic_fluent_*_regular</c> or <c>ic_fluent_*_filled</c> name into a PascalCase enum member name.
    /// </summary>
    /// <param name="rawIconName">The raw upstream icon name.</param>
    /// <returns>The formatted enum member name.</returns>
    public static string Format(string rawIconName)
    {
        rawIconName = rawIconName
            .Replace("ic_fluent_", string.Empty)
            .Replace("_regular", string.Empty)
            .Replace("_filled", string.Empty);

        var iconName = string.Empty;

        foreach (var part in rawIconName.Split('_'))
        {
            var characters = part.ToCharArray();
            characters[0] = char.ToUpper(characters[0]);

            iconName += new string(characters);
        }

        return iconName;
    }
}

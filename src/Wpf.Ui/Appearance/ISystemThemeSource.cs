// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Microsoft.Win32;

namespace Wpf.Ui.Appearance;

/// <summary>
/// Supplies the raw Windows theme signals that <see cref="SystemThemeManager"/> classifies.
/// </summary>
internal interface ISystemThemeSource
{
    /// <summary>
    /// Gets the registry theme filename, or an empty string when it cannot be read.
    /// </summary>
    string CurrentThemeFileName { get; }

    /// <summary>
    /// Gets a value indicating whether Windows high contrast is currently active.
    /// </summary>
    bool IsHighContrastActive { get; }

    /// <summary>
    /// Gets the raw AppsUseLightTheme registry value.
    /// </summary>
    object? RawAppsUseLightTheme { get; }

    /// <summary>
    /// Gets the raw SystemUsesLightTheme registry value.
    /// </summary>
    object? RawSystemUsesLightTheme { get; }
}

/// <summary>
/// Reads Windows theme signals from the registry and <see cref="SystemParameters"/>.
/// </summary>
internal sealed class RegistrySystemThemeSource : ISystemThemeSource
{
    /// <inheritdoc />
    public string CurrentThemeFileName =>
        Registry.GetValue(
            "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes",
            "CurrentTheme",
            "aero.theme"
        ) as string
        ?? string.Empty;

    /// <inheritdoc />
    public bool IsHighContrastActive => SystemParameters.HighContrast;

    /// <inheritdoc />
    public object? RawAppsUseLightTheme =>
        Registry.GetValue(
            "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize",
            "AppsUseLightTheme",
            1
        );

    /// <inheritdoc />
    public object? RawSystemUsesLightTheme =>
        Registry.GetValue(
            "HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize",
            "SystemUsesLightTheme",
            1
        );
}

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Appearance;

/// <summary>
/// Provides information about Windows system themes.
/// </summary>
/// <example>
/// <code lang="csharp">
/// var currentWindowTheme = SystemThemeManager.GetCachedSystemTheme();
/// </code>
/// <code lang="csharp">
/// SystemThemeManager.UpdateSystemThemeCache();
/// var currentWindowTheme = SystemThemeManager.GetCachedSystemTheme();
/// </code>
/// </example>
public static class SystemThemeManager
{
    private static SystemTheme _cachedTheme = SystemTheme.Unknown;

    /// <summary>
    /// Gets or sets the source of the live Windows theme signals used to classify the system theme.
    /// </summary>
    internal static ISystemThemeSource Source { get; set; } = new RegistrySystemThemeSource();

    /// <summary>
    /// Gets the Windows glass color.
    /// </summary>
    public static Color GlassColor => SystemParameters.WindowGlassColor;

    /// <summary>
    /// Gets a value indicating whether the system is currently using the high contrast theme.
    /// </summary>
    public static bool HighContrast => SystemParameters.HighContrast;

    /// <summary>
    /// Returns the Windows theme retrieved from the registry. If it has not been cached before, invokes the <see cref="UpdateSystemThemeCache"/> and then returns the currently obtained theme.
    /// </summary>
    /// <returns>Currently cached Windows theme.</returns>
    public static SystemTheme GetCachedSystemTheme()
    {
        if (_cachedTheme != SystemTheme.Unknown)
        {
            return _cachedTheme;
        }

        UpdateSystemThemeCache();

        return _cachedTheme;
    }

    /// <summary>
    /// Refreshes the currently saved system theme.
    /// </summary>
    public static void UpdateSystemThemeCache()
    {
        _cachedTheme = GetCurrentSystemTheme();
    }

    /// <summary>
    /// Classifies the Windows theme from its registry filename, the live high-contrast flag, and the app/system light-mode registry values.
    /// </summary>
    /// <remarks>High contrast takes precedence: a filename that does not match one of the four known high-contrast themes falls back to <see cref="SystemTheme.Custom"/> rather than a light or dark match.</remarks>
    internal static SystemTheme ClassifySystemTheme(
        string currentThemeFileName,
        bool isHighContrastActive,
        object? rawAppsUseLightTheme,
        object? rawSystemUsesLightTheme
    )
    {
        SystemTheme? matchedTheme = MatchThemeFileName(currentThemeFileName);

        if (isHighContrastActive)
        {
            return matchedTheme
                is SystemTheme.HC1
                    or SystemTheme.HC2
                    or SystemTheme.HCBlack
                    or SystemTheme.HCWhite
                ? matchedTheme.Value
                : SystemTheme.Custom;
        }

        if (matchedTheme.HasValue)
        {
            return matchedTheme.Value;
        }

        if (rawAppsUseLightTheme is 0)
        {
            return SystemTheme.Dark;
        }

        if (rawAppsUseLightTheme is 1)
        {
            return SystemTheme.Light;
        }

        return rawSystemUsesLightTheme is 0 ? SystemTheme.Dark : SystemTheme.Light;
    }

    private static SystemTheme? MatchThemeFileName(string currentThemeFileName)
    {
        if (string.IsNullOrEmpty(currentThemeFileName))
        {
            return null;
        }

        // This may be changed in the next versions, check the Insider previews
        if (currentThemeFileName.Contains("basic.theme", StringComparison.OrdinalIgnoreCase))
        {
            return SystemTheme.Light;
        }

        if (currentThemeFileName.Contains("aero.theme", StringComparison.OrdinalIgnoreCase))
        {
            return SystemTheme.Light;
        }

        if (currentThemeFileName.Contains("dark.theme", StringComparison.OrdinalIgnoreCase))
        {
            return SystemTheme.Dark;
        }

        if (currentThemeFileName.Contains("hcblack.theme", StringComparison.OrdinalIgnoreCase))
        {
            return SystemTheme.HCBlack;
        }

        if (currentThemeFileName.Contains("hcwhite.theme", StringComparison.OrdinalIgnoreCase))
        {
            return SystemTheme.HCWhite;
        }

        if (currentThemeFileName.Contains("hc1.theme", StringComparison.OrdinalIgnoreCase))
        {
            return SystemTheme.HC1;
        }

        if (currentThemeFileName.Contains("hc2.theme", StringComparison.OrdinalIgnoreCase))
        {
            return SystemTheme.HC2;
        }

        if (currentThemeFileName.Contains("themea.theme", StringComparison.OrdinalIgnoreCase))
        {
            return SystemTheme.Glow;
        }

        if (currentThemeFileName.Contains("themeb.theme", StringComparison.OrdinalIgnoreCase))
        {
            return SystemTheme.CapturedMotion;
        }

        if (currentThemeFileName.Contains("themec.theme", StringComparison.OrdinalIgnoreCase))
        {
            return SystemTheme.Sunrise;
        }

        if (currentThemeFileName.Contains("themed.theme", StringComparison.OrdinalIgnoreCase))
        {
            return SystemTheme.Flow;
        }

        return null;
    }

    private static SystemTheme GetCurrentSystemTheme()
    {
        ISystemThemeSource source = Source;

        return ClassifySystemTheme(
            source.CurrentThemeFileName,
            source.IsHighContrastActive,
            source.RawAppsUseLightTheme,
            source.RawSystemUsesLightTheme
        );
    }
}

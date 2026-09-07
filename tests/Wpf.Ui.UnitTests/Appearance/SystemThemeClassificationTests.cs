// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Appearance;

namespace Wpf.Ui.UnitTests.Appearance;

public class SystemThemeClassificationTests
{
    [Theory]
    [InlineData("hc1.theme", SystemTheme.HC1)]
    [InlineData("hc2.theme", SystemTheme.HC2)]
    [InlineData("hcblack.theme", SystemTheme.HCBlack)]
    [InlineData("hcwhite.theme", SystemTheme.HCWhite)]
    public void ClassifySystemTheme_HighContrastActive_RecognizedFileName_ReturnsMatchedVariant(
        string themeFileName,
        SystemTheme expected
    )
    {
        SystemTheme result = SystemThemeManager.ClassifySystemTheme(
            themeFileName,
            isHighContrastActive: true,
            rawAppsUseLightTheme: 1,
            rawSystemUsesLightTheme: 1
        );

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("mycompany.theme")]
    [InlineData("aero.theme")]
    [InlineData("dark.theme")]
    [InlineData("")]
    public void ClassifySystemTheme_HighContrastActive_UnrecognizedFileName_ReturnsCustom(string themeFileName)
    {
        SystemTheme result = SystemThemeManager.ClassifySystemTheme(
            themeFileName,
            isHighContrastActive: true,
            rawAppsUseLightTheme: 1,
            rawSystemUsesLightTheme: 1
        );

        Assert.Equal(SystemTheme.Custom, result);
    }

    [Theory]
    [InlineData("aero.theme", SystemTheme.Light)]
    [InlineData("basic.theme", SystemTheme.Light)]
    [InlineData("dark.theme", SystemTheme.Dark)]
    [InlineData("themea.theme", SystemTheme.Glow)]
    [InlineData("themeb.theme", SystemTheme.CapturedMotion)]
    [InlineData("themec.theme", SystemTheme.Sunrise)]
    [InlineData("themed.theme", SystemTheme.Flow)]
    public void ClassifySystemTheme_HighContrastInactive_RecognizedFileName_ReturnsMatchedVariant(
        string themeFileName,
        SystemTheme expected
    )
    {
        SystemTheme result = SystemThemeManager.ClassifySystemTheme(
            themeFileName,
            isHighContrastActive: false,
            rawAppsUseLightTheme: 1,
            rawSystemUsesLightTheme: 1
        );

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, SystemTheme.Dark)]
    [InlineData(1, SystemTheme.Light)]
    public void ClassifySystemTheme_HighContrastInactive_UnrecognizedFileName_FallsBackToAppsUseLightTheme(
        int rawAppsUseLightTheme,
        SystemTheme expected
    )
    {
        SystemTheme result = SystemThemeManager.ClassifySystemTheme(
            string.Empty,
            isHighContrastActive: false,
            rawAppsUseLightTheme: rawAppsUseLightTheme,
            rawSystemUsesLightTheme: 1
        );

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, SystemTheme.Dark)]
    [InlineData(1, SystemTheme.Light)]
    public void ClassifySystemTheme_HighContrastInactive_NoAppsUseLightTheme_FallsBackToSystemUsesLightTheme(
        int rawSystemUsesLightTheme,
        SystemTheme expected
    )
    {
        SystemTheme result = SystemThemeManager.ClassifySystemTheme(
            string.Empty,
            isHighContrastActive: false,
            rawAppsUseLightTheme: null,
            rawSystemUsesLightTheme: rawSystemUsesLightTheme
        );

        Assert.Equal(expected, result);
    }
}

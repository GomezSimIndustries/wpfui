// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Appearance;

namespace Wpf.Ui.UnitTests.Appearance;

public class ApplicationThemeMappingTests
{
    [Theory]
    [InlineData("hc1.theme", true)]
    [InlineData("hc2.theme", true)]
    [InlineData("hcblack.theme", true)]
    [InlineData("hcwhite.theme", true)]
    [InlineData("mycompany.theme", true)]
    public void MapSystemThemeToApplicationTheme_HighContrastClassification_ReturnsHighContrast(
        string themeFileName,
        bool isHighContrastActive
    )
    {
        SystemTheme classified = SystemThemeManager.ClassifySystemTheme(
            themeFileName,
            isHighContrastActive,
            rawAppsUseLightTheme: 1,
            rawSystemUsesLightTheme: 1
        );

        ApplicationTheme mapped = ApplicationThemeManager.MapSystemThemeToApplicationTheme(classified);

        Assert.Equal(ApplicationTheme.HighContrast, mapped);
    }

    [Theory]
    [InlineData("dark.theme", ApplicationTheme.Dark)]
    [InlineData("themea.theme", ApplicationTheme.Dark)]
    [InlineData("themeb.theme", ApplicationTheme.Dark)]
    [InlineData("aero.theme", ApplicationTheme.Light)]
    [InlineData("themec.theme", ApplicationTheme.Light)]
    [InlineData("themed.theme", ApplicationTheme.Light)]
    public void MapSystemThemeToApplicationTheme_NonHighContrastClassification_ReturnsMatchingApplicationTheme(
        string themeFileName,
        ApplicationTheme expected
    )
    {
        SystemTheme classified = SystemThemeManager.ClassifySystemTheme(
            themeFileName,
            isHighContrastActive: false,
            rawAppsUseLightTheme: 1,
            rawSystemUsesLightTheme: 1
        );

        ApplicationTheme mapped = ApplicationThemeManager.MapSystemThemeToApplicationTheme(classified);

        Assert.Equal(expected, mapped);
    }
}

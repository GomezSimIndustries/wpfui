// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Appearance;

namespace Wpf.Ui.UnitTests;

[Collection("ThemeState")]
public sealed class ThemeServiceTests : IDisposable
{
    private readonly ISystemThemeSource _originalSource = SystemThemeManager.Source;

    public void Dispose()
    {
        SystemThemeManager.Source = _originalSource;
        SystemThemeManager.UpdateSystemThemeCache();
    }

    [Fact]
    public void GetSystemTheme_NativeSystemThemeCustom_ReturnsHighContrast()
    {
        ISystemThemeSource source = Substitute.For<ISystemThemeSource>();
        source.CurrentThemeFileName.Returns("mycompany.theme");
        source.IsHighContrastActive.Returns(true);
        source.RawAppsUseLightTheme.Returns((object?)1);
        source.RawSystemUsesLightTheme.Returns((object?)1);
        SystemThemeManager.Source = source;
        SystemThemeManager.UpdateSystemThemeCache();

        var themeService = new ThemeService();

        ApplicationTheme mapped = themeService.GetSystemTheme();

        Assert.Equal(ApplicationTheme.HighContrast, mapped);
    }
}

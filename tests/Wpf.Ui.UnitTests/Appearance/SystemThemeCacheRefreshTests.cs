// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Appearance;

namespace Wpf.Ui.UnitTests.Appearance;

[Collection("ThemeState")]
public sealed class SystemThemeCacheRefreshTests : IDisposable
{
    private readonly ISystemThemeSource _originalSource = SystemThemeManager.Source;

    public void Dispose()
    {
        SystemThemeManager.Source = _originalSource;
        SystemThemeManager.UpdateSystemThemeCache();
    }

    [Fact]
    public void GetCachedSystemTheme_ReflectsSourceOnlyAfterExplicitRefresh()
    {
        ISystemThemeSource initialSource = Substitute.For<ISystemThemeSource>();
        initialSource.CurrentThemeFileName.Returns("dark.theme");
        initialSource.IsHighContrastActive.Returns(false);
        initialSource.RawAppsUseLightTheme.Returns((object?)1);
        initialSource.RawSystemUsesLightTheme.Returns((object?)1);
        SystemThemeManager.Source = initialSource;
        SystemThemeManager.UpdateSystemThemeCache();

        Assert.Equal(SystemTheme.Dark, SystemThemeManager.GetCachedSystemTheme());

        ISystemThemeSource highContrastSource = Substitute.For<ISystemThemeSource>();
        highContrastSource.CurrentThemeFileName.Returns("mycompany.theme");
        highContrastSource.IsHighContrastActive.Returns(true);
        highContrastSource.RawAppsUseLightTheme.Returns((object?)1);
        highContrastSource.RawSystemUsesLightTheme.Returns((object?)1);
        SystemThemeManager.Source = highContrastSource;

        Assert.Equal(SystemTheme.Dark, SystemThemeManager.GetCachedSystemTheme());

        SystemThemeManager.UpdateSystemThemeCache();

        Assert.Equal(SystemTheme.Custom, SystemThemeManager.GetCachedSystemTheme());
    }
}

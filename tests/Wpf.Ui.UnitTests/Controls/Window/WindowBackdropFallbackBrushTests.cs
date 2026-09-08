// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

[Collection("ThemeState")]
public sealed class WindowBackdropFallbackBrushTests : IDisposable
{
    private readonly ISystemThemeSource _originalSource = SystemThemeManager.Source;

    public void Dispose()
    {
        SystemThemeManager.Source = _originalSource;
        SystemThemeManager.UpdateSystemThemeCache();
        ApplicationThemeManager.ResetCachedApplicationTheme();
    }

    [Fact]
    public void GetFallbackBackgroundBrush_HighContrastSystemThemeCustom_ReturnsHCWhiteBrush()
    {
        SetSystemTheme("mycompany.theme", isHighContrastActive: true);

        WpfTestHost.Invoke(() =>
        {
            var appDictionaries = new ResourceDictionaryManager(ApplicationThemeManager.LibraryNamespace);
            ResourceDictionary? original = appDictionaries.GetDictionary("theme");
            Assert.NotNull(original);
            Uri originalSource = original!.Source;

            try
            {
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, updateAccent: false);

                SolidColorBrush brush = WindowBackdrop.GetFallbackBackgroundBrush();

                Assert.Equal(Color.FromArgb(0xFF, 0x20, 0x20, 0x20), brush.Color);
            }
            finally
            {
                appDictionaries.UpdateDictionary("theme", originalSource);
                ApplicationThemeManager.ResetCachedApplicationTheme();
            }
        });
    }

    private static void SetSystemTheme(string themeFileName, bool isHighContrastActive)
    {
        ISystemThemeSource source = Substitute.For<ISystemThemeSource>();
        source.CurrentThemeFileName.Returns(themeFileName);
        source.IsHighContrastActive.Returns(isHighContrastActive);
        source.RawAppsUseLightTheme.Returns((object?)1);
        source.RawSystemUsesLightTheme.Returns((object?)1);
        SystemThemeManager.Source = source;
        SystemThemeManager.UpdateSystemThemeCache();
    }
}

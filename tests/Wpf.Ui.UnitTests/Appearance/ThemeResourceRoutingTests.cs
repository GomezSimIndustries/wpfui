// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Appearance;
using Wpf.Ui.Markup;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Appearance;

[CollectionDefinition("ThemeState")]
public sealed class ThemeStateCollectionDefinition;

[Collection("ThemeState")]
public sealed class ThemeResourceRoutingTests : IDisposable
{
    private readonly ISystemThemeSource _originalSource = SystemThemeManager.Source;

    public void Dispose()
    {
        SystemThemeManager.Source = _originalSource;
        SystemThemeManager.UpdateSystemThemeCache();
        ApplicationThemeManager.ResetCachedApplicationTheme();
    }

    [Theory]
    [InlineData(ApplicationTheme.Light, "Light.xaml")]
    [InlineData(ApplicationTheme.Dark, "Dark.xaml")]
    public void ThemesDictionary_LightAndDark_ResolveToPackagedResource(
        ApplicationTheme theme,
        string expectedFileName
    )
    {
        WpfTestHost.Invoke(() =>
        {
            var dictionary = new ThemesDictionary { Theme = theme };

            Assert.EndsWith(expectedFileName, dictionary.Source.ToString(), StringComparison.Ordinal);
            Assert.True(dictionary.Count > 0);
        });
    }

    [Theory]
    [InlineData("hc1.theme", "HC1.xaml")]
    [InlineData("hc2.theme", "HC2.xaml")]
    [InlineData("hcblack.theme", "HCBlack.xaml")]
    [InlineData("hcwhite.theme", "HCWhite.xaml")]
    public void ThemesDictionary_HighContrastVariant_ResolvesToPackagedResource(
        string themeFileName,
        string expectedFileName
    )
    {
        SetSystemTheme(themeFileName, isHighContrastActive: true);

        WpfTestHost.Invoke(() =>
        {
            var dictionary = new ThemesDictionary { Theme = ApplicationTheme.HighContrast };

            Assert.EndsWith(expectedFileName, dictionary.Source.ToString(), StringComparison.Ordinal);
            Assert.True(dictionary.Count > 0);
        });
    }

    [Fact]
    public void ThemesDictionary_HighContrastRequested_SystemNotCurrentlyHighContrast_FallsBackToHCWhite()
    {
        SetSystemTheme("mycompany.theme", isHighContrastActive: false);

        WpfTestHost.Invoke(() =>
        {
            var dictionary = new ThemesDictionary { Theme = ApplicationTheme.HighContrast };

            Assert.EndsWith("HCWhite.xaml", dictionary.Source.ToString(), StringComparison.Ordinal);
            Assert.True(dictionary.Count > 0);
        });
    }

    [Theory]
    [InlineData("HC1.xaml")]
    [InlineData("HC2.xaml")]
    [InlineData("HCBlack.xaml")]
    [InlineData("HCWhite.xaml")]
    public void GetAppTheme_EmptyCache_DiscoversHighContrastVariantFromResources(string packagedFileName)
    {
        SetSystemTheme("hc1.theme", isHighContrastActive: true);

        WpfTestHost.Invoke(() =>
        {
            var appDictionaries = new ResourceDictionaryManager(ApplicationThemeManager.LibraryNamespace);
            ResourceDictionary? original = appDictionaries.GetDictionary("theme");
            Assert.NotNull(original);
            Uri originalSource = original!.Source;

            try
            {
                bool updated = appDictionaries.UpdateDictionary(
                    "theme",
                    new Uri(ApplicationThemeManager.ThemesDictionaryPath + packagedFileName, UriKind.Absolute)
                );
                Assert.True(updated);

                ApplicationThemeManager.ResetCachedApplicationTheme();

                ApplicationTheme discovered = ApplicationThemeManager.GetAppTheme();

                Assert.Equal(ApplicationTheme.HighContrast, discovered);
            }
            finally
            {
                appDictionaries.UpdateDictionary("theme", originalSource);
                ApplicationThemeManager.ResetCachedApplicationTheme();
            }
        });
    }

    [Theory]
    [InlineData("hc1.theme", "HC1.xaml")]
    [InlineData("hc2.theme", "HC2.xaml")]
    [InlineData("hcblack.theme", "HCBlack.xaml")]
    [InlineData("hcwhite.theme", "HCWhite.xaml")]
    public void Apply_HighContrast_SelectsSameVariantAsThemesDictionary(string themeFileName, string expectedFileName)
    {
        SetSystemTheme(themeFileName, isHighContrastActive: true);

        WpfTestHost.Invoke(() =>
        {
            var appDictionaries = new ResourceDictionaryManager(ApplicationThemeManager.LibraryNamespace);
            ResourceDictionary? original = appDictionaries.GetDictionary("theme");
            Assert.NotNull(original);
            Uri originalSource = original!.Source;

            try
            {
                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, updateAccent: false);

                ResourceDictionary? updated = appDictionaries.GetDictionary("theme");
                Assert.NotNull(updated);
                Assert.EndsWith(expectedFileName, updated!.Source.ToString(), StringComparison.Ordinal);

                var standalone = new ThemesDictionary { Theme = ApplicationTheme.HighContrast };
                Assert.Equal(updated.Source, standalone.Source);
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

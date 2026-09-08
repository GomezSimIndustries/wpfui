// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Appearance;

[Collection("ThemeState")]
public sealed class ThemeApplyResultTests : IDisposable
{
    public void Dispose()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
        });
    }

    [Fact]
    public void Apply_UnknownTheme_ReturnsFalse()
    {
        WpfTestHost.Invoke(() =>
        {
            bool applied = ApplicationThemeManager.Apply(ApplicationTheme.Unknown, updateAccent: false);

            Assert.False(applied);
        });
    }

    [Fact]
    public void Apply_UnknownTheme_UpdateAccentTrue_StillUpdatesAccentResource()
    {
        WpfTestHost.Invoke(() =>
        {
            var sentinel = Color.FromArgb(0xFF, 0x01, 0x02, 0x03);
            UiApplication.Current.Resources["SystemAccentColor"] = sentinel;

            bool applied = ApplicationThemeManager.Apply(ApplicationTheme.Unknown, updateAccent: true);

            Assert.False(applied);
            Assert.NotEqual(sentinel, (Color)UiApplication.Current.Resources["SystemAccentColor"]);
        });
    }

    [Fact]
    public void Apply_UnknownTheme_UpdateAccentFalse_LeavesAccentResourceUnchanged()
    {
        WpfTestHost.Invoke(() =>
        {
            var sentinel = Color.FromArgb(0xFF, 0x01, 0x02, 0x03);
            UiApplication.Current.Resources["SystemAccentColor"] = sentinel;

            bool applied = ApplicationThemeManager.Apply(ApplicationTheme.Unknown, updateAccent: false);

            Assert.False(applied);
            Assert.Equal(sentinel, (Color)UiApplication.Current.Resources["SystemAccentColor"]);
        });
    }

    [Fact]
    public void Apply_MatchingThemeDictionaryPresent_ReturnsTrue()
    {
        WpfTestHost.Invoke(() =>
        {
            bool applied = ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);

            Assert.True(applied);
        });
    }

    [Fact]
    public void Apply_NoMatchingThemeDictionary_ReturnsFalse()
    {
        WpfTestHost.Invoke(() =>
        {
            var appDictionaries = new ResourceDictionaryManager(ApplicationThemeManager.LibraryNamespace);
            ResourceDictionary? original = appDictionaries.GetDictionary("theme");
            Assert.NotNull(original);

            Collection<ResourceDictionary> top = UiApplication.Current.Resources.MergedDictionaries;
            int index = top.IndexOf(original!);
            Assert.True(index >= 0);

            top.RemoveAt(index);

            try
            {
                bool applied = ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);

                Assert.False(applied);
            }
            finally
            {
                top.Insert(index, original!);
                ApplicationThemeManager.ResetCachedApplicationTheme();
            }
        });
    }

    [Fact]
    public void SetTheme_SameThemeRequested_ReturnsFalseAndDoesNotReplaceDictionary()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);

            var appDictionaries = new ResourceDictionaryManager(ApplicationThemeManager.LibraryNamespace);
            ResourceDictionary? before = appDictionaries.GetDictionary("theme");

            var themeService = new ThemeService();
            bool changed = themeService.SetTheme(ApplicationTheme.Dark);

            ResourceDictionary? after = appDictionaries.GetDictionary("theme");

            Assert.False(changed);
            Assert.Same(before, after);
        });
    }

    [Fact]
    public void SetTheme_DifferentThemeRequested_ReturnsTrueAndAppliesIt()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);

            var themeService = new ThemeService();

            bool changed = themeService.SetTheme(ApplicationTheme.Light);

            Assert.True(changed);
            Assert.Equal(ApplicationTheme.Light, ApplicationThemeManager.GetAppTheme());
        });
    }

    [Fact]
    public void SetTheme_UnknownThemeRequested_ReturnsFalse()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);

            var themeService = new ThemeService();

            bool changed = themeService.SetTheme(ApplicationTheme.Unknown);

            Assert.False(changed);
        });
    }
}

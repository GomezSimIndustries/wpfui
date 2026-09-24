// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

[Collection("ThemeState")]
public sealed class TitleBarThemeAttachmentLifetimeTests : IDisposable
{
    public void Dispose()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
        });
    }

    [Fact]
    public void ThemeChange_AfterReload_UpdatesTheApplicationThemeExactlyOnce()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);

            Window window = WpfTestHost.CreateOffscreenOwnerWindow();
            var bar = new CountingTitleBar();
            window.Content = bar;
            window.Show();
            bar.UpdateLayout();

            try
            {
                bar.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
                bar.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                int baseline = bar.ThemeChangedCount;

                ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);

                Assert.Equal(baseline + 1, bar.ThemeChangedCount);
                Assert.Equal(ApplicationTheme.Light, bar.ApplicationTheme);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ThemeChange_WhileUnloaded_DoesNotUpdate()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);

            Window window = WpfTestHost.CreateOffscreenOwnerWindow();
            var bar = new CountingTitleBar();
            window.Content = bar;
            window.Show();
            bar.UpdateLayout();

            try
            {
                bar.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

                int baseline = bar.ThemeChangedCount;

                ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);

                Assert.Equal(baseline, bar.ThemeChangedCount);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ThemeChange_BeforeFirstLoad_DoesNotUpdate()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);

            var bar = new CountingTitleBar();
            bar.BeginInit();
            bar.EndInit();

            ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);

            Assert.Equal(0, bar.ThemeChangedCount);
        });
    }

    [Fact]
    public void FirstLoad_AfterAThemeChange_TakesTheCurrentApplicationTheme()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);

            var bar = new CountingTitleBar();
            bar.BeginInit();
            bar.EndInit();

            ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);

            Window window = WpfTestHost.CreateOffscreenOwnerWindow();
            window.Content = bar;
            window.Show();
            bar.UpdateLayout();

            try
            {
                Assert.Equal(ApplicationTheme.Light, bar.ApplicationTheme);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private sealed class CountingTitleBar : TitleBar
    {
        public int ThemeChangedCount { get; private set; }

        protected override void OnThemeChanged(ApplicationTheme currentApplicationTheme, Color systemAccent)
        {
            ThemeChangedCount++;
            base.OnThemeChanged(currentApplicationTheme, systemAccent);
        }
    }
}

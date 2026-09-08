// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.UnitTests.TestHelpers;
using ClientAreaBorderControl = Wpf.Ui.Controls.ClientAreaBorder;

namespace Wpf.Ui.UnitTests.Controls;

[Collection("ThemeState")]
public sealed class ClientAreaBorderThemeLifetimeTests : IDisposable
{
    public void Dispose()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
        });
    }

    [Fact]
    public void ClosingCanceled_ThemeChangeStillUpdatesApplicationTheme()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);

            Window window = WpfTestHost.CreateOffscreenOwnerWindow();
            var grid = (Grid)window.Content;
            var border = new ClientAreaBorderControl();
            grid.Children.Add(border);

            CancelEventHandler cancelClosing = (_, e) => e.Cancel = true;
            window.Closing += cancelClosing;

            window.Show();
            window.UpdateLayout();

            window.Close();

            Assert.True(window.IsVisible);

            ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);

            Assert.Equal(ApplicationTheme.Light, border.ApplicationTheme);

            window.Closing -= cancelClosing;
            window.Close();
        });
    }

    [Fact]
    public void RealClose_ThemeChangeNoLongerUpdatesApplicationTheme()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);

            Window window = WpfTestHost.CreateOffscreenOwnerWindow();
            var grid = (Grid)window.Content;
            var border = new ClientAreaBorderControl();
            grid.Children.Add(border);

            window.Show();
            window.UpdateLayout();

            window.Close();

            ApplicationTheme themeBeforeChange = border.ApplicationTheme;

            ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);

            Assert.Equal(themeBeforeChange, border.ApplicationTheme);
            Assert.NotEqual(ApplicationTheme.Light, border.ApplicationTheme);
        });
    }

    [Fact]
    public void ThemeChange_WhileUnloaded_DoesNotUpdateApplicationTheme()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
            var border = new ClientAreaBorderControl();
            border.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);
            Assert.Equal(ApplicationTheme.Light, border.ApplicationTheme);

            border.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
            Assert.Equal(ApplicationTheme.Light, border.ApplicationTheme);
        });
    }

    [Fact]
    public void Reload_ResyncsApplicationThemeAndResumesLiveUpdates()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
            var border = new ClientAreaBorderControl();
            border.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            border.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

            ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);

            border.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Assert.Equal(ApplicationTheme.Light, border.ApplicationTheme);

            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
            Assert.Equal(ApplicationTheme.Dark, border.ApplicationTheme);
        });
    }

    [Fact]
    public void LoadedRaisedTwiceWithoutUnload_ThemeChangeUpdatesExactlyOnce()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
            var border = new CountingClientAreaBorder();
            border.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            border.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            int baseline = border.ThemeChangedCount;

            ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);

            Assert.Equal(baseline + 1, border.ThemeChangedCount);
        });
    }

    [Fact]
    public void RepeatedLoadUnloadCycles_ThemeChangeUpdatesExactlyOncePerApply()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
            var border = new CountingClientAreaBorder();

            for (int cycle = 0; cycle < 3; cycle++)
            {
                border.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                border.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            }

            border.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            int baseline = border.ThemeChangedCount;

            ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);

            Assert.Equal(baseline + 1, border.ThemeChangedCount);
        });
    }

    private sealed class CountingClientAreaBorder : ClientAreaBorderControl
    {
        public int ThemeChangedCount { get; private set; }

        protected override void OnThemeChanged(ApplicationTheme currentApplicationTheme, Color systemAccent)
        {
            ThemeChangedCount++;
            base.OnThemeChanged(currentApplicationTheme, systemAccent);
        }
    }
}

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class LoadingScreenForegroundSlotTests
{
    [Fact]
    public void Foreground_DefaultResource_ReachesControlSpinnerAndContent()
    {
        WpfTestHost.Invoke(() =>
        {
            var foregroundSentinel = new SolidColorBrush(Colors.Lime);
            var backgroundSentinel = new SolidColorBrush(Colors.Magenta);

            WithReplacedResources(
                foregroundSentinel,
                backgroundSentinel,
                () =>
                {
                    var loadingScreen = new Wpf.Ui.Controls.LoadingScreen { Content = "Loading" };
                    Window window = WpfTestHost.CreateOffscreenOwnerWindow();
                    ((Grid)window.Content).Children.Add(loadingScreen);

                    try
                    {
                        window.Show();
                        window.UpdateLayout();

                        Assert.Equal(backgroundSentinel, loadingScreen.Background);
                        Assert.Equal(foregroundSentinel, loadingScreen.Foreground);

                        var progressRing = FindVisualChild<Wpf.Ui.Controls.ProgressRing>(loadingScreen);
                        Assert.NotNull(progressRing);
                        Assert.Equal(foregroundSentinel, progressRing!.Foreground);

                        var content = FindVisualChild<TextBlock>(loadingScreen);
                        Assert.NotNull(content);
                        Assert.Equal(foregroundSentinel, content!.Foreground);
                    }
                    finally
                    {
                        window.Close();
                    }
                }
            );
        });
    }

    [Fact]
    public void Foreground_ExplicitLocalValue_OverridesDefaultOnSpinnerAndContent()
    {
        WpfTestHost.Invoke(() =>
        {
            var explicitForeground = new SolidColorBrush(Colors.Orange);

            var loadingScreen = new Wpf.Ui.Controls.LoadingScreen
            {
                Content = "Loading",
                Foreground = explicitForeground,
            };
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();
            ((Grid)window.Content).Children.Add(loadingScreen);

            try
            {
                window.Show();
                window.UpdateLayout();

                var progressRing = FindVisualChild<Wpf.Ui.Controls.ProgressRing>(loadingScreen);
                Assert.NotNull(progressRing);
                Assert.Equal(explicitForeground, progressRing!.Foreground);

                var content = FindVisualChild<TextBlock>(loadingScreen);
                Assert.NotNull(content);
                Assert.Equal(explicitForeground, content!.Foreground);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Foreground_DefaultResource_ReachesSpinnerWithNoContent()
    {
        WpfTestHost.Invoke(() =>
        {
            var foregroundSentinel = new SolidColorBrush(Colors.Cyan);
            var backgroundSentinel = new SolidColorBrush(Colors.Magenta);

            WithReplacedResources(
                foregroundSentinel,
                backgroundSentinel,
                () =>
                {
                    var loadingScreen = new Wpf.Ui.Controls.LoadingScreen();
                    Window window = WpfTestHost.CreateOffscreenOwnerWindow();
                    ((Grid)window.Content).Children.Add(loadingScreen);

                    try
                    {
                        window.Show();
                        window.UpdateLayout();

                        var progressRing = FindVisualChild<Wpf.Ui.Controls.ProgressRing>(loadingScreen);
                        Assert.NotNull(progressRing);
                        Assert.Equal(foregroundSentinel, progressRing!.Foreground);
                        Assert.Equal(backgroundSentinel, loadingScreen.Background);
                    }
                    finally
                    {
                        window.Close();
                    }
                }
            );
        });
    }

    private static void WithReplacedResources(Brush foreground, Brush background, Action test)
    {
        ResourceDictionary resources = Application.Current!.Resources;
        WithReplacedResource(
            resources,
            "LoadingScreenForeground",
            foreground,
            () => WithReplacedResource(resources, "LoadingScreenBackground", background, test));
    }

    private static void WithReplacedResource(
        ResourceDictionary resources,
        string key,
        object replacement,
        Action test)
    {
        bool hadOriginal = resources.Contains(key);
        object? original = hadOriginal ? resources[key] : null;

        resources[key] = replacement;
        try
        {
            test();
        }
        finally
        {
            if (hadOriginal)
            {
                resources[key] = original!;
            }
            else
            {
                resources.Remove(key);
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent)
        where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed)
            {
                return typed;
            }

            T? result = FindVisualChild<T>(child);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}

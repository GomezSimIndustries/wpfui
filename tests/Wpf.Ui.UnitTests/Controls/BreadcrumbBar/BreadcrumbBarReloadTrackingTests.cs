// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class BreadcrumbBarReloadTrackingTests
{
    private static readonly string BreadcrumbBarTemplatePath = Path.GetFullPath(
        Path.Combine(
            Path.GetDirectoryName(ThisFilePath())!,
            "..",
            "..",
            "..",
            "..",
            "src",
            "Wpf.Ui",
            "Controls",
            "BreadcrumbBar",
            "BreadcrumbBar.xaml"
        )
    );

    [Fact]
    public void Reload_AfterDetachAndReinsert_MovesIsLastToTheItemAddedAfterReload()
    {
        StaThread.Run(() =>
        {
            var (window, bar) = CreateHostedBreadcrumbBar("Documents", "Reports", "Q3");

            try
            {
                Assert.True(ContainerFor(bar, "Q3").IsLast);

                bar.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
                bar.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                bar.Items.Add("Q4");
                bar.UpdateLayout();

                Assert.False(ContainerFor(bar, "Q3").IsLast);
                Assert.True(ContainerFor(bar, "Q4").IsLast);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Reload_AfterDetachAndReinsert_MovesIsLastWhenTheCurrentLastItemIsRemoved()
    {
        StaThread.Run(() =>
        {
            var (window, bar) = CreateHostedBreadcrumbBar("Documents", "Reports", "Q3");

            try
            {
                bar.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
                bar.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                bar.Items.RemoveAt(bar.Items.Count - 1);
                bar.UpdateLayout();

                Assert.True(ContainerFor(bar, "Reports").IsLast);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void RepeatedReloadCycles_LeaveExactlyOneItemMarkedLast()
    {
        StaThread.Run(() =>
        {
            var (window, bar) = CreateHostedBreadcrumbBar("Documents", "Reports", "Q3");

            try
            {
                for (var cycle = 0; cycle < 3; cycle++)
                {
                    bar.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
                    bar.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                }

                bar.Items.Add("Q4");
                bar.UpdateLayout();

                Assert.False(ContainerFor(bar, "Q3").IsLast);
                Assert.True(ContainerFor(bar, "Q4").IsLast);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Loaded_WithAnItemWhoseContainerIsNotYetGenerated_DoesNotThrow()
    {
        StaThread.Run(() =>
        {
            var bar = new Wpf.Ui.Controls.BreadcrumbBar();
            bar.Items.Add("Documents");

            var exception = Record.Exception(
                () => bar.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent))
            );

            Assert.Null(exception);
        });
    }

    private static BreadcrumbBarItem ContainerFor(Wpf.Ui.Controls.BreadcrumbBar bar, object item) =>
        (BreadcrumbBarItem)bar.ItemContainerGenerator.ContainerFromItem(item);

    private static (Window Window, Wpf.Ui.Controls.BreadcrumbBar Bar) CreateHostedBreadcrumbBar(
        params object[] items
    )
    {
        var xaml = File.ReadAllText(BreadcrumbBarTemplatePath)
            .Replace("clr-namespace:Wpf.Ui.Controls\"", "clr-namespace:Wpf.Ui.Controls;assembly=Wpf.Ui\"");
        var styles = (ResourceDictionary)XamlReader.Parse(xaml);

        var bar = new Wpf.Ui.Controls.BreadcrumbBar();

        foreach (var item in items)
        {
            bar.Items.Add(item);
        }

        var window = new Window
        {
            Content = bar,
            Width = 200,
            Height = 80,
            Left = -5000,
            Top = -5000,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
        };
        window.Resources.MergedDictionaries.Add(styles);
        window.Show();
        bar.UpdateLayout();

        return (window, bar);
    }

    private static string ThisFilePath([CallerFilePath] string path = "") => path;
}

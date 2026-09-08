// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Threading;
using Windows.Win32;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public partial class TitleBarAttachmentLifetimeTests
{
    [Fact]
    public void Reattach_ToADifferentWindow_TracksTheNewWindowNotTheOld()
    {
        WpfTestHost.Invoke(() =>
        {
            Window windowA = WpfTestHost.CreateOffscreenOwnerWindow();
            var bar = new TitleBar { Template = BuildTemplate() };
            windowA.Content = bar;
            windowA.Show();
            bar.ApplyTemplate();

            Window windowB = WpfTestHost.CreateOffscreenOwnerWindow();

            try
            {
                bar.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

                windowA.Content = null;
                windowB.Content = bar;
                windowB.Show();
                bar.ApplyTemplate();

                bar.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                windowA.SetCurrentValue(Window.WindowStateProperty, WindowState.Maximized);
                Assert.False(bar.IsMaximized);

                windowB.SetCurrentValue(Window.WindowStateProperty, WindowState.Maximized);
                Assert.True(bar.IsMaximized);
            }
            finally
            {
                windowA.Close();
                windowB.Close();
            }
        });
    }

    [Fact]
    public void Unload_RemovesTheHwndHookInstalledForTheParentWindow()
    {
        WpfTestHost.Invoke(() =>
        {
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();
            var bar = new TitleBar { Template = BuildTemplate() };
            window.Content = bar;
            window.Show();
            bar.ApplyTemplate();

            try
            {
                var invocations = 0;
                bar.WndProcInvoked += (_, _) => invocations++;

                IntPtr handle = new WindowInteropHelper(window).Handle;
                Assert.NotEqual(IntPtr.Zero, handle);

                PumpUntil(() =>
                {
                    _ = SendMessage(handle, PInvoke.WM_NCMOUSELEAVE, IntPtr.Zero, IntPtr.Zero);
                    return invocations > 0;
                });

                Assert.Equal(1, invocations);

                bar.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

                _ = SendMessage(handle, PInvoke.WM_NCMOUSELEAVE, IntPtr.Zero, IntPtr.Zero);

                Assert.Equal(1, invocations);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void OnApplyTemplate_AppliedRepeatedly_ShowsTheSystemMenuOnceForOneRightClick()
    {
        WpfTestHost.Invoke(() =>
        {
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();
            var bar = new RecordingSystemMenuTitleBar { Template = BuildTemplate() };
            window.Content = bar;
            window.Show();
            bar.ApplyTemplate();

            try
            {
                bar.Template = BuildTemplate();
                bar.ApplyTemplate();

                bar.Template = BuildTemplate();
                bar.ApplyTemplate();

                bar.RaiseEvent(
                    new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Right)
                    {
                        RoutedEvent = UIElement.MouseRightButtonUpEvent,
                    }
                );

                Assert.Equal(1, bar.SystemMenuRequests);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static ControlTemplate BuildTemplate()
    {
        const string Xaml = """
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                              xmlns:controls="clr-namespace:Wpf.Ui.Controls;assembly=Wpf.Ui"
                              TargetType="{x:Type controls:TitleBar}">
                <Grid>
                    <ContentPresenter x:Name="PART_Icon" />
                    <controls:TitleBarButton x:Name="PART_HelpButton" />
                    <controls:TitleBarButton x:Name="PART_MinimizeButton" />
                    <controls:TitleBarButton x:Name="PART_MaximizeButton" />
                    <controls:TitleBarButton x:Name="PART_CloseButton" />
                </Grid>
            </ControlTemplate>
            """;

        return (ControlTemplate)XamlReader.Parse(Xaml);
    }

    private static void PumpUntil(Func<bool> condition)
    {
        const int MaxIterations = 2000;

        var satisfied = condition();

        for (var i = 0; i < MaxIterations && !satisfied; i++)
        {
            var frame = new DispatcherFrame();
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => frame.Continue = false)
            );
            Dispatcher.PushFrame(frame);

            satisfied = condition();
        }

        if (!satisfied)
        {
            throw new InvalidOperationException(
                "Dispatcher pump exceeded its iteration bound without satisfying the condition."
            );
        }
    }

    [LibraryImport("user32.dll", EntryPoint = "SendMessageW")]
    private static partial IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private sealed class RecordingSystemMenuTitleBar : TitleBar
    {
        public int SystemMenuRequests { get; private set; }

        protected override void ShowSystemMenu(Point point)
        {
            SystemMenuRequests++;
        }
    }
}

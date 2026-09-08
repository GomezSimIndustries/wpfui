// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Appearance;

[CollectionDefinition("WindowBackgroundManagerState")]
public sealed class WindowBackgroundManagerStateCollectionDefinition;

[Collection("WindowBackgroundManagerState")]
public sealed class WindowBackgroundManagerLifecycleTests : IDisposable
{
    private readonly IWindowBackgroundNativeCalls _originalNativeCalls = WindowBackgroundManager.NativeCalls;
    private readonly RecordingWindowBackgroundNativeCalls _recorder = new();

    public WindowBackgroundManagerLifecycleTests()
    {
        WindowBackgroundManager.NativeCalls = _recorder;
    }

    public void Dispose()
    {
        WindowBackgroundManager.NativeCalls = _originalNativeCalls;
    }

    [Fact]
    public void ApplyDarkThemeToWindow_WindowAlreadyLoaded_DoesNotSubscribeAnotherLoadedHandler()
    {
        WpfTestHost.Invoke(() =>
        {
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();

            try
            {
                window.Show();

                WindowBackgroundManager.ApplyDarkThemeToWindow(window);

                Assert.Single(_recorder.ApplyWindowDarkModeCalls);
                Assert.Same(window, _recorder.ApplyWindowDarkModeCalls[0]);

                window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                Assert.Single(_recorder.ApplyWindowDarkModeCalls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ApplyDarkThemeToWindow_WindowNotYetLoaded_AppliesOnceWhenLoaded()
    {
        WpfTestHost.Invoke(() =>
        {
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();

            try
            {
                WindowBackgroundManager.ApplyDarkThemeToWindow(window);

                Assert.Empty(_recorder.ApplyWindowDarkModeCalls);

                window.Show();

                Assert.Single(_recorder.ApplyWindowDarkModeCalls);
                Assert.Same(window, _recorder.ApplyWindowDarkModeCalls[0]);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void RequestedThemeState_AlternatedBeforeLoad_OnlyLatestStateAppliesOnce()
    {
        WpfTestHost.Invoke(() =>
        {
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();

            try
            {
                WindowBackgroundManager.ApplyDarkThemeToWindow(window);
                WindowBackgroundManager.RemoveDarkThemeFromWindow(window);
                WindowBackgroundManager.ApplyDarkThemeToWindow(window);

                window.Show();

                Assert.Single(_recorder.ApplyWindowDarkModeCalls);
                Assert.Empty(_recorder.RemoveWindowDarkModeCalls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void UpdateBackground_OwnedWindow_RemovesTitlebarBackgroundOnParentAndOwnedWindowSeparately()
    {
        WpfTestHost.Invoke(() =>
        {
            Window parent = WpfTestHost.CreateOffscreenOwnerWindow();
            parent.Show();

            var child = new Window
            {
                Owner = parent,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false,
                Left = -32000,
                Top = -32000,
                Width = 100,
                Height = 100,
            };

            try
            {
                WindowBackgroundManager.UpdateBackground(parent, ApplicationTheme.Dark, WindowBackdropType.None);

                Assert.Equal(2, _recorder.RemoveTitlebarBackgroundCalls.Count);
                Assert.Contains(parent, _recorder.RemoveTitlebarBackgroundCalls);
                Assert.Contains(child, _recorder.RemoveTitlebarBackgroundCalls);

                Assert.Contains(
                    _recorder.ApplyBackdropCalls,
                    call => ReferenceEquals(call.Window, parent) && call.Backdrop == WindowBackdropType.None
                );
                Assert.Contains(
                    _recorder.ApplyBackdropCalls,
                    call => ReferenceEquals(call.Window, child) && call.Backdrop == WindowBackdropType.None
                );
            }
            finally
            {
                child.Owner = null;
                parent.Close();
            }
        });
    }
}

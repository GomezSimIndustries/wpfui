// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;
using Wpf.Ui.Appearance;
using Wpf.Ui.Markup;

namespace Wpf.Ui.UnitTests.TestHelpers;

/// <summary>Provides a lazily started background WPF <see cref="Application"/> and dispatcher for window tests.</summary>
public static class WpfTestHost
{
    private static readonly object Lock = new();

    private static Dispatcher? _dispatcher;

    /// <summary>Runs an action on the shared test dispatcher and blocks until it completes.</summary>
    public static void Invoke(Action action)
    {
        EnsureInitialized();
        _dispatcher!.Invoke(action);
    }

    /// <summary>Runs a function on the shared test dispatcher and returns its result.</summary>
    public static T Invoke<T>(Func<T> func)
    {
        EnsureInitialized();
        return _dispatcher!.Invoke(func);
    }

    /// <summary>Creates a window positioned off the visible desktop, for use as an owner in window lifecycle tests.</summary>
    public static Window CreateOffscreenOwnerWindow() =>
        new()
        {
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -32000,
            Top = -32000,
            Width = 200,
            Height = 100,
            Content = new Grid(),
        };

    /// <summary>Starts the shared test dispatcher thread if it has not already started, blocking until its resource dictionaries are loaded.</summary>
    internal static void EnsureInitialized()
    {
        if (_dispatcher is not null)
        {
            return;
        }

        lock (Lock)
        {
            if (_dispatcher is not null)
            {
                return;
            }

            using var ready = new ManualResetEventSlim(false);
            var thread = new Thread(() =>
            {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                Application.Current!.Resources.MergedDictionaries.Add(
                    new ThemesDictionary { Theme = ApplicationTheme.Dark });
                Application.Current.Resources.MergedDictionaries.Add(new ControlsDictionary());

                _dispatcher = Dispatcher.CurrentDispatcher;
                ready.Set();

                Dispatcher.Run();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Name = "WpfUiTestDispatcher";
            thread.Start();
            ready.Wait();
        }
    }
}

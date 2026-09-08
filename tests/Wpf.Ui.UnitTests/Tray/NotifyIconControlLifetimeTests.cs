// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Tray.Controls;

namespace Wpf.Ui.UnitTests.Tray;

public sealed class NotifyIconControlLifetimeTests
{
    [Fact]
    public void MenuSetToNull_AllowsThePreviousContextMenuToBeCollected()
    {
        StaThread.Run(() =>
        {
            WeakReference weakMenu = CreateNotifyIconWithMenuThenClearIt();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(weakMenu.IsAlive);
        });
    }

    [Fact]
    public void Menu_ReplacedWithANewContextMenu_AllowsThePreviousOneToBeCollected()
    {
        StaThread.Run(() =>
        {
            WeakReference weakFirstMenu = CreateNotifyIconAndReplaceMenu();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(weakFirstMenu.IsAlive);
        });
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrowAndStaysUnregistered()
    {
        StaThread.Run(() =>
        {
            var notifyIcon = new NotifyIcon();

#pragma warning disable IDISP016, IDISP017
            notifyIcon.Dispose();
            notifyIcon.Dispose();
#pragma warning restore IDISP016, IDISP017

            Assert.False(notifyIcon.IsRegistered);
        });
    }

    [Fact]
    public void OnRender_AfterDispose_DoesNotAttemptToReregister()
    {
        StaThread.Run(() =>
        {
            var notifyIcon = new TestableNotifyIcon();
            notifyIcon.Dispose();

            notifyIcon.SimulateRender();

            Assert.False(notifyIcon.IsRegistered);
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateNotifyIconWithMenuThenClearIt()
    {
        using var notifyIcon = new NotifyIcon();
        var menu = new ContextMenu();
        notifyIcon.Menu = menu;

        notifyIcon.Menu = null;

        return new WeakReference(menu);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateNotifyIconAndReplaceMenu()
    {
        using var notifyIcon = new NotifyIcon();
        var firstMenu = new ContextMenu();
        notifyIcon.Menu = firstMenu;

        notifyIcon.Menu = new ContextMenu();

        return new WeakReference(firstMenu);
    }

    private sealed class TestableNotifyIcon : NotifyIcon
    {
        public void SimulateRender()
        {
            var visual = new DrawingVisual();
            using DrawingContext drawingContext = visual.RenderOpen();
            OnRender(drawingContext);
        }
    }

    private static class StaThread
    {
        public static void Run(Action action)
        {
            ExceptionDispatchInfo? capturedException = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    capturedException = ExceptionDispatchInfo.Capture(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            capturedException?.Throw();
        }
    }
}

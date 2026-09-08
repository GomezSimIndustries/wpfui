// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Tray.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Tray;

public sealed class NotifyIconControlLifetimeTests
{
    [Fact]
    public void MenuSetToNull_AllowsThePreviousContextMenuToBeCollected()
    {
        WeakReference weakMenu = WpfTestHost.Invoke(() =>
        {
            WeakReference weak = CreateNotifyIconWithMenuThenClearIt();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
            return weak;
        });

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(weakMenu.IsAlive);
    }

    [Fact]
    public void Menu_ReplacedWithANewContextMenu_AllowsThePreviousOneToBeCollected()
    {
        WeakReference weakFirstMenu = WpfTestHost.Invoke(() =>
        {
            WeakReference weak = CreateNotifyIconAndReplaceMenu();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
            return weak;
        });

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(weakFirstMenu.IsAlive);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrowAndStaysUnregistered()
    {
        WpfTestHost.Invoke(() =>
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
        WpfTestHost.Invoke(() =>
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
}

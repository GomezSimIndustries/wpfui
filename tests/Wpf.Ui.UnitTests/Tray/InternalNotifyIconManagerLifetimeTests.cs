// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Wpf.Ui.Tray;
using Wpf.Ui.Tray.Internal;
using Wpf.Ui.Tray.Interop;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Tray;

[Collection("TrayManagerState")]
public sealed class InternalNotifyIconManagerLifetimeTests
{
    [Fact]
    public void Register_BeforeDispose_ReachesTheShell()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeShellNotifyIconGateway(true);
            var factory = new FakeTrayHookWindowFactory();
            using var fixture = new TrayManagerFixture(gateway, factory);
            Window owner = WpfTestHost.CreateOffscreenOwnerWindow();
            owner.Show();
            var manager = new InternalNotifyIconManager();

            bool result = manager.Register(owner);

            Assert.True(result);
            Assert.True(manager.IsRegistered);
            Assert.Contains(gateway.Calls, call => call.Message == Shell32.NIM.ADD);

            manager.Dispose();
            owner.Close();
        });
    }

    [Fact]
    public void Register_AfterDispose_ReturnsFalseAndNeverTouchesTheShell()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeShellNotifyIconGateway(true);
            var factory = new FakeTrayHookWindowFactory();
            using var fixture = new TrayManagerFixture(gateway, factory);
            Window owner = WpfTestHost.CreateOffscreenOwnerWindow();
            owner.Show();
            var manager = new InternalNotifyIconManager();
            manager.Dispose();

            bool result = manager.Register(owner);

            Assert.False(result);
            Assert.False(manager.IsRegistered);
            Assert.Empty(gateway.Calls);
            Assert.Empty(factory.Created);

            owner.Close();
        });
    }

    [Fact]
    public void Dispose_CalledTwice_UnregistersOnlyOnce()
    {
        var gateway = new FakeShellNotifyIconGateway();
        var factory = new FakeTrayHookWindowFactory();
        using var fixture = new TrayManagerFixture(gateway, factory);
        var manager = new InternalNotifyIconManager();
        Assert.True(TrayManager.RegisterCore(manager, hasParentSource: true, parentHandle: new IntPtr(1)));

#pragma warning disable IDISP016, IDISP017
        manager.Dispose();
        manager.Dispose();
#pragma warning restore IDISP016, IDISP017

        Assert.Single(gateway.Calls, call => call.Message == Shell32.NIM.DELETE);
    }

    [Fact]
    public void Dispose_ReleasesTheStaticThemeSubscription_AllowingTheManagerToBeCollected()
    {
        WeakReference weakManager = CreateAndDisposeManager();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(weakManager.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndDisposeManager()
    {
        using var manager = new InternalNotifyIconManager();

        return new WeakReference(manager);
    }
}

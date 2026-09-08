// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.Windows;
using Wpf.Ui.Tray;
using Wpf.Ui.Tray.Interop;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Tray;

[Collection("TrayManagerState")]
public sealed class NotifyIconServiceLifetimeTests
{
    [Fact]
    public void ParentWindowClosing_Canceled_KeepsTheIconRegistered()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeShellNotifyIconGateway(true);
            var factory = new FakeTrayHookWindowFactory();
            using var fixture = new TrayManagerFixture(gateway, factory);
            Window owner = WpfTestHost.CreateOffscreenOwnerWindow();
            owner.Show();
            var service = new NotifyIconService();
            service.SetParentWindow(owner);

            CancelEventHandler cancelHandler = (sender, e) => e.Cancel = true;
            owner.Closing += cancelHandler;

            bool registered = service.Register();
            owner.Close();

            Assert.True(registered);
            Assert.True(service.IsRegistered);
            Assert.DoesNotContain(gateway.Calls, call => call.Message == Shell32.NIM.DELETE);

            owner.Closing -= cancelHandler;
            service.Dispose();
            owner.Close();
        });
    }

    [Fact]
    public void ParentWindowClosed_ActualClose_DisposesTheIcon()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeShellNotifyIconGateway(true);
            var factory = new FakeTrayHookWindowFactory();
            using var fixture = new TrayManagerFixture(gateway, factory);
            Window owner = WpfTestHost.CreateOffscreenOwnerWindow();
            owner.Show();
            var service = new NotifyIconService();
            service.SetParentWindow(owner);

            bool registered = service.Register();
            owner.Close();

            Assert.True(registered);
            Assert.False(service.IsRegistered);
            Assert.Contains(gateway.Calls, call => call.Message == Shell32.NIM.DELETE);
        });
    }

    [Fact]
    public void SetParentWindow_ReplacingParent_TracksOnlyTheNewParent()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeShellNotifyIconGateway(true);
            var factory = new FakeTrayHookWindowFactory();
            using var fixture = new TrayManagerFixture(gateway, factory);
            Window firstOwner = WpfTestHost.CreateOffscreenOwnerWindow();
            Window secondOwner = WpfTestHost.CreateOffscreenOwnerWindow();
            firstOwner.Show();
            secondOwner.Show();
            var service = new NotifyIconService();
            service.SetParentWindow(firstOwner);
            service.SetParentWindow(secondOwner);

            bool registered = service.Register();
            firstOwner.Close();

            Assert.True(registered);
            Assert.True(service.IsRegistered);

            secondOwner.Close();

            Assert.False(service.IsRegistered);
        });
    }

    [Fact]
    public void Dispose_BeforeParentCloses_PreventsAnotherUnregisterOnClose()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeShellNotifyIconGateway(true);
            var factory = new FakeTrayHookWindowFactory();
            using var fixture = new TrayManagerFixture(gateway, factory);
            Window owner = WpfTestHost.CreateOffscreenOwnerWindow();
            owner.Show();
            var service = new NotifyIconService();
            service.SetParentWindow(owner);
            _ = service.Register();

            service.Dispose();
            owner.Close();

            Assert.False(service.IsRegistered);
            Assert.Single(gateway.Calls, call => call.Message == Shell32.NIM.DELETE);
        });
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var service = new NotifyIconService();

#pragma warning disable IDISP016, IDISP017
        service.Dispose();
        service.Dispose();
#pragma warning restore IDISP016, IDISP017

        Assert.False(service.IsRegistered);
    }
}

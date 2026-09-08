// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Tray;

namespace Wpf.Ui.UnitTests.Tray;

public class TrayManagerTests
{
    [Fact]
    public void RegisterCore_CommitsRegistration_OnlyAfterShellAcceptsAdd()
    {
        var gateway = new FakeShellNotifyIconGateway(true);
        var factory = new FakeTrayHookWindowFactory();
        using var fixture = new TrayManagerFixture(gateway, factory);
        var icon = new FakeNotifyIcon();

        bool result = TrayManager.RegisterCore(icon, hasParentSource: true, parentHandle: new IntPtr(1));

        Assert.True(result);
        Assert.True(icon.IsRegistered);
        Assert.Contains(icon, TrayData.NotifyIcons);

        _ = TrayManager.Unregister(icon);
    }

    [Fact]
    public void RegisterCore_DoesNotCommitRegistration_WhenShellRejectsAdd()
    {
        var gateway = new FakeShellNotifyIconGateway(false);
        var factory = new FakeTrayHookWindowFactory();
        using var fixture = new TrayManagerFixture(gateway, factory);
        var icon = new FakeNotifyIcon();

        bool result = TrayManager.RegisterCore(icon, hasParentSource: true, parentHandle: new IntPtr(1));

        Assert.False(result);
        Assert.False(icon.IsRegistered);
        Assert.DoesNotContain(icon, TrayData.NotifyIcons);
        Assert.Equal(-1, icon.Id);
        Assert.Single(factory.Created);
        Assert.True(factory.Created[0].Disposed);
    }

    [Fact]
    public void RegisterCore_WithoutParentSource_ReturnsFalse_WithoutTouchingShellOrHookFactory()
    {
        var gateway = new FakeShellNotifyIconGateway();
        var factory = new FakeTrayHookWindowFactory();
        using var fixture = new TrayManagerFixture(gateway, factory);
        var icon = new FakeNotifyIcon();

        bool result = TrayManager.RegisterCore(icon, hasParentSource: false, parentHandle: IntPtr.Zero);

        Assert.False(result);
        Assert.Empty(gateway.Calls);
        Assert.Empty(factory.Created);
    }

    [Fact]
    public void RegisterCore_WithZeroParentHandle_ReturnsFalse_WithoutTouchingShellOrHookFactory()
    {
        var gateway = new FakeShellNotifyIconGateway();
        var factory = new FakeTrayHookWindowFactory();
        using var fixture = new TrayManagerFixture(gateway, factory);
        var icon = new FakeNotifyIcon();

        bool result = TrayManager.RegisterCore(icon, hasParentSource: true, parentHandle: IntPtr.Zero);

        Assert.False(result);
        Assert.Empty(gateway.Calls);
        Assert.Empty(factory.Created);
    }

    [Fact]
    public void FailedAdd_DoesNotRetryAutomatically()
    {
        var gateway = new FakeShellNotifyIconGateway(false);
        var factory = new FakeTrayHookWindowFactory();
        using var fixture = new TrayManagerFixture(gateway, factory);
        var icon = new FakeNotifyIcon();

        bool result = TrayManager.RegisterCore(icon, hasParentSource: true, parentHandle: new IntPtr(1));

        Assert.False(result);
        Assert.Single(gateway.Calls);
        Assert.Single(factory.Created);
    }

    [Fact]
    public void Unregister_RemovesIconFromRegistry_AndDisposesHookWindow()
    {
        var gateway = new FakeShellNotifyIconGateway();
        var factory = new FakeTrayHookWindowFactory();
        using var fixture = new TrayManagerFixture(gateway, factory);
        var icon = new FakeNotifyIcon();
        Assert.True(TrayManager.RegisterCore(icon, hasParentSource: true, parentHandle: new IntPtr(1)));
        int before = TrayData.NotifyIcons.Count;

        bool result = TrayManager.Unregister(icon);

        Assert.True(result);
        Assert.False(icon.IsRegistered);
        Assert.Equal(-1, icon.Id);
        Assert.DoesNotContain(icon, TrayData.NotifyIcons);
        Assert.Equal(before - 1, TrayData.NotifyIcons.Count);
        Assert.True(factory.Created[0].Disposed);
    }

    [Fact]
    public void Unregister_ClearsLocalState_EvenWhenShellDeleteFails()
    {
        var gateway = new FakeShellNotifyIconGateway(true, false);
        var factory = new FakeTrayHookWindowFactory();
        using var fixture = new TrayManagerFixture(gateway, factory);
        var icon = new FakeNotifyIcon();
        Assert.True(TrayManager.RegisterCore(icon, hasParentSource: true, parentHandle: new IntPtr(1)));

        bool result = TrayManager.Unregister(icon);

        Assert.True(result);
        Assert.False(icon.IsRegistered);
        Assert.DoesNotContain(icon, TrayData.NotifyIcons);
        Assert.True(factory.Created[0].Disposed);
    }

    [Fact]
    public void Registry_StaysBounded_AcrossRepeatedRegisterUnregisterCycles()
    {
        var gateway = new FakeShellNotifyIconGateway();
        var factory = new FakeTrayHookWindowFactory();
        using var fixture = new TrayManagerFixture(gateway, factory);
        var icon = new FakeNotifyIcon();
        int baseline = TrayData.NotifyIcons.Count;

        for (int i = 0; i < 5; i++)
        {
            Assert.True(TrayManager.RegisterCore(icon, hasParentSource: true, parentHandle: new IntPtr(1)));
            Assert.True(TrayManager.Unregister(icon));
        }

        Assert.Equal(baseline, TrayData.NotifyIcons.Count);
    }

    [Fact]
    public void Registry_StaysBounded_WithMultipleIconsAndOneRemoved()
    {
        var gateway = new FakeShellNotifyIconGateway();
        var factory = new FakeTrayHookWindowFactory();
        using var fixture = new TrayManagerFixture(gateway, factory);
        var iconA = new FakeNotifyIcon();
        var iconB = new FakeNotifyIcon();
        int baseline = TrayData.NotifyIcons.Count;

        Assert.True(TrayManager.RegisterCore(iconA, hasParentSource: true, parentHandle: new IntPtr(1)));
        Assert.True(TrayManager.RegisterCore(iconB, hasParentSource: true, parentHandle: new IntPtr(1)));
        Assert.True(TrayManager.Unregister(iconA));

        Assert.Equal(baseline + 1, TrayData.NotifyIcons.Count);
        Assert.Contains(iconB, TrayData.NotifyIcons);
        Assert.DoesNotContain(iconA, TrayData.NotifyIcons);

        _ = TrayManager.Unregister(iconB);
    }

    [Fact]
    public void Reregistration_ReplacesTheHookWindow_WithoutDuplicatingRegistryEntry()
    {
        var gateway = new FakeShellNotifyIconGateway();
        var factory = new FakeTrayHookWindowFactory();
        using var fixture = new TrayManagerFixture(gateway, factory);
        var icon = new FakeNotifyIcon();
        int baseline = TrayData.NotifyIcons.Count;

        Assert.True(TrayManager.RegisterCore(icon, hasParentSource: true, parentHandle: new IntPtr(1)));
        int firstId = icon.Id;

        Assert.True(TrayManager.RegisterCore(icon, hasParentSource: true, parentHandle: new IntPtr(1)));
        int secondId = icon.Id;

        Assert.NotEqual(firstId, secondId);
        Assert.Equal(baseline + 1, TrayData.NotifyIcons.Count);
        Assert.True(factory.Created[0].Disposed);
        Assert.False(factory.Created[1].Disposed);

        _ = TrayManager.Unregister(icon);
    }

    [Fact]
    public void AllocatedIdentity_NeverCollidesWithAStillLiveIcon_AfterAnotherIconIsRemoved()
    {
        var gateway = new FakeShellNotifyIconGateway();
        var factory = new FakeTrayHookWindowFactory();
        using var fixture = new TrayManagerFixture(gateway, factory);
        var iconA = new FakeNotifyIcon();
        var iconB = new FakeNotifyIcon();
        var iconC = new FakeNotifyIcon();

        Assert.True(TrayManager.RegisterCore(iconA, hasParentSource: true, parentHandle: new IntPtr(1)));
        Assert.True(TrayManager.RegisterCore(iconB, hasParentSource: true, parentHandle: new IntPtr(1)));
        Assert.True(TrayManager.Unregister(iconA));
        Assert.True(TrayManager.RegisterCore(iconC, hasParentSource: true, parentHandle: new IntPtr(1)));

        Assert.NotEqual(iconB.Id, iconC.Id);

        _ = TrayManager.Unregister(iconB);
        _ = TrayManager.Unregister(iconC);
    }

    [Fact]
    public void RegisterCore_Reregistration_DestroysThePreviousOwnedHicon()
    {
        var gateway = new FakeShellNotifyIconGateway();
        var factory = new FakeTrayHookWindowFactory();
        var iconHandleGateway = new FakeNativeIconHandleGateway();
        using var fixture = new TrayManagerFixture(gateway, factory, iconHandleGateway);
        var icon = new FakeNotifyIcon();

        Assert.True(TrayManager.RegisterCore(icon, hasParentSource: true, parentHandle: new IntPtr(1)));
        IntPtr firstHicon = icon.ShellIconData.hIcon;
        Assert.NotEqual(IntPtr.Zero, firstHicon);

        Assert.True(TrayManager.RegisterCore(icon, hasParentSource: true, parentHandle: new IntPtr(1)));
        IntPtr secondHicon = icon.ShellIconData.hIcon;

        Assert.NotEqual(IntPtr.Zero, secondHicon);
        Assert.NotEqual(firstHicon, secondHicon);
        Assert.Contains(firstHicon, iconHandleGateway.DestroyedHandles);
        Assert.DoesNotContain(secondHicon, iconHandleGateway.DestroyedHandles);

        _ = TrayManager.Unregister(icon);
    }

    [Fact]
    public void Unregister_DestroysTheOwnedHicon_AndClearsIt()
    {
        var gateway = new FakeShellNotifyIconGateway();
        var factory = new FakeTrayHookWindowFactory();
        var iconHandleGateway = new FakeNativeIconHandleGateway();
        using var fixture = new TrayManagerFixture(gateway, factory, iconHandleGateway);
        var icon = new FakeNotifyIcon();
        Assert.True(TrayManager.RegisterCore(icon, hasParentSource: true, parentHandle: new IntPtr(1)));
        IntPtr ownedHicon = icon.ShellIconData.hIcon;
        Assert.NotEqual(IntPtr.Zero, ownedHicon);

        Assert.True(TrayManager.Unregister(icon));

        Assert.Contains(ownedHicon, iconHandleGateway.DestroyedHandles);
        Assert.Equal(IntPtr.Zero, icon.ShellIconData.hIcon);
    }

    [Fact]
    public void RegisterCore_ShellRejectsAdd_DestroysTheHiconItCreated()
    {
        var gateway = new FakeShellNotifyIconGateway(false);
        var factory = new FakeTrayHookWindowFactory();
        var iconHandleGateway = new FakeNativeIconHandleGateway();
        using var fixture = new TrayManagerFixture(gateway, factory, iconHandleGateway);
        var icon = new FakeNotifyIcon();

        bool result = TrayManager.RegisterCore(icon, hasParentSource: true, parentHandle: new IntPtr(1));

        Assert.False(result);
        Assert.Single(iconHandleGateway.DestroyedHandles);
        Assert.NotEqual(IntPtr.Zero, iconHandleGateway.DestroyedHandles[0]);
    }

    [Fact]
    public void RepeatedRegisterUnregisterCycles_NeverDestroyTheSameHiconTwice()
    {
        var gateway = new FakeShellNotifyIconGateway();
        var factory = new FakeTrayHookWindowFactory();
        var iconHandleGateway = new FakeNativeIconHandleGateway();
        using var fixture = new TrayManagerFixture(gateway, factory, iconHandleGateway);
        var icon = new FakeNotifyIcon();

        for (int i = 0; i < 5; i++)
        {
            Assert.True(TrayManager.RegisterCore(icon, hasParentSource: true, parentHandle: new IntPtr(1)));
            Assert.True(TrayManager.Unregister(icon));
        }

        Assert.Equal(0, iconHandleGateway.DuplicateDestroyCount);
        Assert.Equal(5, iconHandleGateway.DestroyedHandles.Count);
    }
}

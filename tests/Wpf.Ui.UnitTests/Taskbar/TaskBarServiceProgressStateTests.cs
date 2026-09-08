// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Interop;
using Wpf.Ui.TaskBar;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Taskbar;

public sealed class TaskBarServiceProgressStateTests : IDisposable
{
    public void Dispose()
    {
        TaskBarService.ProgressGateway = TaskBarProgressGateway.Instance;
    }

    [Fact]
    public void SetState_ThenGetState_ReturnsRequestedState()
    {
        var gateway = new FakeNativeTaskBarProgressGateway();
        TaskBarService.ProgressGateway = gateway;
        var service = new TaskBarService();
        var handle = new IntPtr(1);

        bool result = service.SetState(handle, TaskBarProgressState.Paused);

        Assert.True(result);
        Assert.Equal(TaskBarProgressState.Paused, service.GetState(handle));
    }

    [Fact]
    public void SetState_ThenValueOnlyUpdate_UsesRequestedStateNotNormal()
    {
        var gateway = new FakeNativeTaskBarProgressGateway();
        TaskBarService.ProgressGateway = gateway;
        var service = new TaskBarService();
        var handle = new IntPtr(2);

        service.SetState(handle, TaskBarProgressState.Paused);
        service.SetValue(handle, 5, 10);

        Assert.Equal(TaskBarProgressState.Paused, gateway.ValueCalls[^1].State);
    }

    [Fact]
    public void SetValue_ExplicitState_ThenGetState_ReturnsThatState()
    {
        var gateway = new FakeNativeTaskBarProgressGateway();
        TaskBarService.ProgressGateway = gateway;
        var service = new TaskBarService();
        var handle = new IntPtr(3);

        service.SetValue(handle, TaskBarProgressState.Error, 3, 10);

        Assert.Equal(TaskBarProgressState.Error, service.GetState(handle));
    }

    [Fact]
    public void SetState_None_ClearsCachedStateAndValueOnlyUpdateFallsBackToNormal()
    {
        var gateway = new FakeNativeTaskBarProgressGateway();
        TaskBarService.ProgressGateway = gateway;
        var service = new TaskBarService();
        var handle = new IntPtr(4);

        service.SetState(handle, TaskBarProgressState.Paused);
        service.SetState(handle, TaskBarProgressState.None);

        Assert.Equal(TaskBarProgressState.None, service.GetState(handle));

        service.SetValue(handle, 1, 2);

        Assert.Equal(TaskBarProgressState.Normal, gateway.ValueCalls[^1].State);
    }

    [Fact]
    public void SetState_FailedNativeCall_DoesNotOverwriteCachedState()
    {
        var gateway = new FakeNativeTaskBarProgressGateway(true, false);
        TaskBarService.ProgressGateway = gateway;
        var service = new TaskBarService();
        var handle = new IntPtr(5);

        service.SetState(handle, TaskBarProgressState.Paused);
        bool secondResult = service.SetState(handle, TaskBarProgressState.Error);

        Assert.False(secondResult);
        Assert.Equal(TaskBarProgressState.Paused, service.GetState(handle));
    }

    [Fact]
    public void SetState_NativeCallThrows_PropagatesAndDoesNotCacheState()
    {
        var gateway = new FakeNativeTaskBarProgressGateway();
        TaskBarService.ProgressGateway = gateway;
        var service = new TaskBarService();
        var handle = new IntPtr(6);
        var failure = new InvalidOperationException("native failure");
        gateway.ThrowOnNextCall = failure;

        var thrown = Assert.Throws<InvalidOperationException>(
            () => service.SetState(handle, TaskBarProgressState.Error));

        Assert.Same(failure, thrown);
        Assert.Equal(TaskBarProgressState.None, service.GetState(handle));
    }

    [Fact]
    public void IndependentHandles_TrackStateSeparately()
    {
        var gateway = new FakeNativeTaskBarProgressGateway();
        TaskBarService.ProgressGateway = gateway;
        var service = new TaskBarService();
        var handleA = new IntPtr(7);
        var handleB = new IntPtr(8);

        service.SetState(handleA, TaskBarProgressState.Paused);
        service.SetState(handleB, TaskBarProgressState.Error);

        Assert.Equal(TaskBarProgressState.Paused, service.GetState(handleA));
        Assert.Equal(TaskBarProgressState.Error, service.GetState(handleB));
    }

    [Fact]
    public void HandleReuse_NewStateReplacesStaleEntry()
    {
        var gateway = new FakeNativeTaskBarProgressGateway();
        TaskBarService.ProgressGateway = gateway;
        var service = new TaskBarService();
        var handle = new IntPtr(9);

        service.SetState(handle, TaskBarProgressState.Paused);
        service.SetState(handle, TaskBarProgressState.None);
        service.SetState(handle, TaskBarProgressState.Indeterminate);

        Assert.Equal(TaskBarProgressState.Indeterminate, service.GetState(handle));
    }

    [Fact]
    public void SetState_Window_ThenGetState_Window_ReturnsRequestedState()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeNativeTaskBarProgressGateway();
            TaskBarService.ProgressGateway = gateway;
            var service = new TaskBarService();
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();
            window.Show();

            bool result = service.SetState(window, TaskBarProgressState.Paused);

            Assert.True(result);
            Assert.Equal(TaskBarProgressState.Paused, service.GetState(window));

            window.Close();
        });
    }

    [Fact]
    public void SetValue_Window_ExplicitState_ThenValueOnlyUpdate_UsesCachedState()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeNativeTaskBarProgressGateway();
            TaskBarService.ProgressGateway = gateway;
            var service = new TaskBarService();
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();
            window.Show();

            service.SetValue(window, TaskBarProgressState.Error, 4, 10);
            service.SetValue(window, 6, 10);

            Assert.Equal(TaskBarProgressState.Error, gateway.ValueCalls[^1].State);

            window.Close();
        });
    }

    [Fact]
    public void IndependentWindows_TrackStateSeparately()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeNativeTaskBarProgressGateway();
            TaskBarService.ProgressGateway = gateway;
            var service = new TaskBarService();
            Window windowA = WpfTestHost.CreateOffscreenOwnerWindow();
            Window windowB = WpfTestHost.CreateOffscreenOwnerWindow();
            windowA.Show();
            windowB.Show();

            service.SetState(windowA, TaskBarProgressState.Paused);
            service.SetState(windowB, TaskBarProgressState.Error);

            Assert.Equal(TaskBarProgressState.Paused, service.GetState(windowA));
            Assert.Equal(TaskBarProgressState.Error, service.GetState(windowB));
            Assert.NotEqual(new WindowInteropHelper(windowA).Handle, new WindowInteropHelper(windowB).Handle);

            windowA.Close();
            windowB.Close();
        });
    }
}

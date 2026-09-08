// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;
using Wpf.Ui.TaskBar;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Taskbar;

[CollectionDefinition("TaskBarProgressGatewayState")]
public sealed class TaskBarProgressGatewayStateCollectionDefinition;

[Collection("TaskBarProgressGatewayState")]
public sealed class TaskBarProgressPendingLoadTests : IDisposable
{
    public void Dispose()
    {
        TaskBarService.ProgressGateway = TaskBarProgressGateway.Instance;
    }

    [Fact]
    public void SetState_WindowNotYetLoaded_QueuesAndReturnsFalse()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeNativeTaskBarProgressGateway();
            TaskBarService.ProgressGateway = gateway;
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();

            try
            {
                bool result = TaskBarProgress.SetState(window, TaskBarProgressState.Paused);

                Assert.False(result);
                Assert.Empty(gateway.StateCalls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SetState_WindowNotYetLoaded_AppliesOnceWhenLoaded()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeNativeTaskBarProgressGateway();
            TaskBarService.ProgressGateway = gateway;
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();

            try
            {
                TaskBarProgress.SetState(window, TaskBarProgressState.Paused);
                window.Show();

                Assert.Single(gateway.StateCalls);
                Assert.Equal(TaskBarProgressState.Paused, gateway.StateCalls[0].State);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SetState_RepeatedPreLoadRequests_OnlyLatestAppliesOnce()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeNativeTaskBarProgressGateway();
            TaskBarService.ProgressGateway = gateway;
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();

            try
            {
                TaskBarProgress.SetState(window, TaskBarProgressState.Paused);
                TaskBarProgress.SetState(window, TaskBarProgressState.Error);
                TaskBarProgress.SetState(window, TaskBarProgressState.Indeterminate);
                window.Show();

                Assert.Single(gateway.StateCalls);
                Assert.Equal(TaskBarProgressState.Indeterminate, gateway.StateCalls[0].State);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SetState_LatestPreLoadRequestIsNone_AppliesNoneOnce()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeNativeTaskBarProgressGateway();
            TaskBarService.ProgressGateway = gateway;
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();

            try
            {
                TaskBarProgress.SetState(window, TaskBarProgressState.Paused);
                TaskBarProgress.SetState(window, TaskBarProgressState.None);
                window.Show();

                Assert.Single(gateway.StateCalls);
                Assert.Equal(TaskBarProgressState.None, gateway.StateCalls[0].State);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SetValue_WindowNotYetLoaded_QueuesAndReturnsFalse()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeNativeTaskBarProgressGateway();
            TaskBarService.ProgressGateway = gateway;
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();

            try
            {
                bool result = TaskBarProgress.SetValue(window, TaskBarProgressState.Error, 4, 10);

                Assert.False(result);
                Assert.Empty(gateway.ValueCalls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SetValue_RepeatedPreLoadRequests_OnlyLatestAppliesOnce()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeNativeTaskBarProgressGateway();
            TaskBarService.ProgressGateway = gateway;
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();

            try
            {
                TaskBarProgress.SetValue(window, TaskBarProgressState.Error, 2, 10);
                TaskBarProgress.SetValue(window, TaskBarProgressState.Normal, 7, 10);
                window.Show();

                Assert.Single(gateway.ValueCalls);
                Assert.Equal(TaskBarProgressState.Normal, gateway.ValueCalls[0].State);
                Assert.Equal(7, gateway.ValueCalls[0].Current);
                Assert.Equal(10, gateway.ValueCalls[0].Total);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SetState_ThenSetValue_BeforeLoad_OnlyLatestRequestAppliesOnce()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeNativeTaskBarProgressGateway();
            TaskBarService.ProgressGateway = gateway;
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();

            try
            {
                TaskBarProgress.SetState(window, TaskBarProgressState.Paused);
                TaskBarProgress.SetValue(window, TaskBarProgressState.Error, 3, 10);
                window.Show();

                Assert.Empty(gateway.StateCalls);
                Assert.Single(gateway.ValueCalls);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SetState_WindowClosedBeforeLoad_NeverAppliesAndRemovesPendingRequest()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeNativeTaskBarProgressGateway();
            TaskBarService.ProgressGateway = gateway;
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();

            TaskBarProgress.SetState(window, TaskBarProgressState.Paused);
            Assert.True(TaskBarProgress.HasPendingRequest(window));

            window.Close();

            Assert.Empty(gateway.StateCalls);
            Assert.False(TaskBarProgress.HasPendingRequest(window));
        });
    }

    [Fact]
    public void SetState_WindowAlreadyLoaded_DoesNotLeavePendingRequest()
    {
        WpfTestHost.Invoke(() =>
        {
            var gateway = new FakeNativeTaskBarProgressGateway();
            TaskBarService.ProgressGateway = gateway;
            Window window = WpfTestHost.CreateOffscreenOwnerWindow();
            window.Show();

            try
            {
                bool result = TaskBarProgress.SetState(window, TaskBarProgressState.Paused);

                Assert.True(result);
                Assert.False(TaskBarProgress.HasPendingRequest(window));
                Assert.Single(gateway.StateCalls);
            }
            finally
            {
                window.Close();
            }
        });
    }
}

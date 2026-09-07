// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class MessageBoxLifecycleTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShowDialogAsync_PreCanceledToken_NeverShowsTheWindowAndSettlesCanceled(bool showAsDialog)
    {
        Window owner = null!;
        Wpf.Ui.Controls.MessageBox messageBox = null!;

        WpfTestHost.Invoke(() =>
        {
            owner = WpfTestHost.CreateOffscreenOwnerWindow();
            owner.Show();
            messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowActivated = false,
            };
        });

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        Task<Wpf.Ui.Controls.MessageBoxResult> resultTask = WpfTestHost.Invoke(() => messageBox.ShowDialogAsync(showAsDialog, cts.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resultTask);
        Assert.True(resultTask.IsCanceled);
        Assert.False(messageBox.IsVisible);

        WpfTestHost.Invoke(owner.Close);
    }

    [Fact]
    public void ShowDialogAsync_CancellationDuringModalShow_ClosesTheWindowAndSettlesCanceled()
    {
        using var cts = new CancellationTokenSource();

        WpfTestHost.Invoke(() =>
        {
            Window owner = WpfTestHost.CreateOffscreenOwnerWindow();
            owner.Show();

            var messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowActivated = false,
            };
            messageBox.Loaded += (_, _) => cts.Cancel();

            Task<Wpf.Ui.Controls.MessageBoxResult> resultTask = messageBox.ShowDialogAsync(true, cts.Token);

            Assert.True(resultTask.IsCompleted);
            Assert.True(resultTask.IsCanceled);
            Assert.False(messageBox.IsVisible);

            owner.Close();
        });
    }

    [Fact]
    public void ShowDialogAsync_BackgroundThreadCancellationDuringModalShow_ClosesTheWindowAndSettlesCanceled()
    {
        using var cts = new CancellationTokenSource();
        using var loaded = new ManualResetEventSlim(false);
        Window owner = null!;
        Wpf.Ui.Controls.MessageBox messageBox = null!;

        WpfTestHost.Invoke(() =>
        {
            owner = WpfTestHost.CreateOffscreenOwnerWindow();
            owner.Show();
            messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowActivated = false,
            };
            messageBox.Loaded += (_, _) => loaded.Set();
        });

        var canceller = new Thread(() =>
        {
            Assert.True(loaded.Wait(TimeSpan.FromSeconds(15)));
            cts.Cancel();
        })
        {
            IsBackground = true,
        };
        canceller.Start();

        Task<Wpf.Ui.Controls.MessageBoxResult> resultTask = WpfTestHost.Invoke(() => messageBox.ShowDialogAsync(true, cts.Token));

        Assert.True(canceller.Join(TimeSpan.FromSeconds(15)));
        Assert.True(resultTask.IsCompleted);
        Assert.True(resultTask.IsCanceled);
        Assert.False(messageBox.IsVisible);

        WpfTestHost.Invoke(owner.Close);
    }

    [Fact]
    public async Task ShowDialogAsync_CancellationDuringModelessShow_ClosesTheWindowAndSettlesCanceled()
    {
        using var cts = new CancellationTokenSource();
        var loadedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Window owner = null!;
        Wpf.Ui.Controls.MessageBox messageBox = null!;
        Task<Wpf.Ui.Controls.MessageBoxResult> resultTask = null!;

        WpfTestHost.Invoke(() =>
        {
            owner = WpfTestHost.CreateOffscreenOwnerWindow();
            owner.Show();
            messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowActivated = false,
            };
            messageBox.Loaded += (_, _) => loadedTcs.TrySetResult();

            resultTask = messageBox.ShowDialogAsync(false, cts.Token);
        });

        using var hangGuard = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await loadedTcs.Task.WaitAsync(hangGuard.Token);

        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => resultTask.WaitAsync(hangGuard.Token));
        Assert.True(resultTask.IsCanceled);
        Assert.False(WpfTestHost.Invoke(() => messageBox.IsVisible));

        WpfTestHost.Invoke(owner.Close);
    }

    [Fact]
    public async Task ShowDialogAsync_OwnerWindowCloses_SettlesResultNoneAndClosesTheWindow()
    {
        var loadedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Window owner = null!;
        Wpf.Ui.Controls.MessageBox messageBox = null!;
        Task<Wpf.Ui.Controls.MessageBoxResult> resultTask = null!;

        WpfTestHost.Invoke(() =>
        {
            owner = WpfTestHost.CreateOffscreenOwnerWindow();
            owner.Show();
            messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowActivated = false,
            };
            messageBox.Loaded += (_, _) => loadedTcs.TrySetResult();

            resultTask = messageBox.ShowDialogAsync(false);
        });

        using var hangGuard = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await loadedTcs.Task.WaitAsync(hangGuard.Token);

        WpfTestHost.Invoke(owner.Close);

        Wpf.Ui.Controls.MessageBoxResult result = await resultTask.WaitAsync(hangGuard.Token);

        Assert.Equal(Wpf.Ui.Controls.MessageBoxResult.None, result);
        Assert.False(WpfTestHost.Invoke(() => messageBox.IsVisible));
    }

    [Fact]
    public async Task ShowDialogAsync_PrimaryButtonClicked_SettlesPrimaryResultAndClosesTheWindow()
    {
        var loadedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Window owner = null!;
        Wpf.Ui.Controls.MessageBox messageBox = null!;
        Task<Wpf.Ui.Controls.MessageBoxResult> resultTask = null!;

        WpfTestHost.Invoke(() =>
        {
            owner = WpfTestHost.CreateOffscreenOwnerWindow();
            owner.Show();
            messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowActivated = false,
            };
            messageBox.Loaded += (_, _) => loadedTcs.TrySetResult();

            resultTask = messageBox.ShowDialogAsync(false);
        });

        using var hangGuard = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await loadedTcs.Task.WaitAsync(hangGuard.Token);

        WpfTestHost.Invoke(() => messageBox.TemplateButtonCommand.Execute(Wpf.Ui.Controls.MessageBoxButton.Primary));

        Wpf.Ui.Controls.MessageBoxResult result = await resultTask.WaitAsync(hangGuard.Token);

        Assert.Equal(Wpf.Ui.Controls.MessageBoxResult.Primary, result);
        Assert.False(WpfTestHost.Invoke(() => messageBox.IsVisible));

        WpfTestHost.Invoke(owner.Close);
    }

    [Fact]
    public async Task ShowDialogAsync_CloseButtonClicked_SettlesNoneResultAndClosesTheWindow()
    {
        var loadedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Window owner = null!;
        Wpf.Ui.Controls.MessageBox messageBox = null!;
        Task<Wpf.Ui.Controls.MessageBoxResult> resultTask = null!;

        WpfTestHost.Invoke(() =>
        {
            owner = WpfTestHost.CreateOffscreenOwnerWindow();
            owner.Show();
            messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowActivated = false,
            };
            messageBox.Loaded += (_, _) => loadedTcs.TrySetResult();

            resultTask = messageBox.ShowDialogAsync(false);
        });

        using var hangGuard = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await loadedTcs.Task.WaitAsync(hangGuard.Token);

        WpfTestHost.Invoke(() => messageBox.TemplateButtonCommand.Execute(Wpf.Ui.Controls.MessageBoxButton.Close));

        Wpf.Ui.Controls.MessageBoxResult result = await resultTask.WaitAsync(hangGuard.Token);

        Assert.Equal(Wpf.Ui.Controls.MessageBoxResult.None, result);
        Assert.False(WpfTestHost.Invoke(() => messageBox.IsVisible));

        WpfTestHost.Invoke(owner.Close);
    }
}

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class MessageBoxTemplatePropertyTests
{
    [Fact]
    public async Task ShowTitle_DefaultTrue_TitleTextIsVisible()
    {
        (Window owner, Wpf.Ui.Controls.MessageBox messageBox) = await ShowLoadedMessageBox(
            messageBox => messageBox.Title = "Sample title"
        );

        WpfTestHost.Invoke(() =>
        {
            var titleText = (TextBlock)messageBox.Template.FindName("Title", messageBox)!;

            Assert.Equal(Visibility.Visible, titleText.Visibility);

            owner.Close();
        });
    }

    [Fact]
    public async Task ShowTitle_SetFalse_CollapsesTitleText()
    {
        (Window owner, Wpf.Ui.Controls.MessageBox messageBox) = await ShowLoadedMessageBox(messageBox =>
        {
            messageBox.Title = "Sample title";
            messageBox.ShowTitle = false;
        });

        WpfTestHost.Invoke(() =>
        {
            var titleText = (TextBlock)messageBox.Template.FindName("Title", messageBox)!;

            Assert.Equal(Visibility.Collapsed, titleText.Visibility);

            owner.Close();
        });
    }

    [Fact]
    public async Task IsCloseButtonEnabled_DefaultTrue_CloseButtonIsEnabledAndVisible()
    {
        (Window owner, Wpf.Ui.Controls.MessageBox messageBox) = await ShowLoadedMessageBox(_ => { });

        WpfTestHost.Invoke(() =>
        {
            var closeButton = (Wpf.Ui.Controls.Button)messageBox.Template.FindName("CloseButton", messageBox)!;

            Assert.True(closeButton.IsEnabled);
            Assert.Equal(Visibility.Visible, closeButton.Visibility);

            owner.Close();
        });
    }

    [Fact]
    public async Task IsCloseButtonEnabled_SetFalse_DisablesAndCollapsesCloseButton()
    {
        (Window owner, Wpf.Ui.Controls.MessageBox messageBox) = await ShowLoadedMessageBox(
            messageBox => messageBox.IsCloseButtonEnabled = false
        );

        WpfTestHost.Invoke(() =>
        {
            var closeButton = (Wpf.Ui.Controls.Button)messageBox.Template.FindName("CloseButton", messageBox)!;

            Assert.False(closeButton.IsEnabled);
            Assert.Equal(Visibility.Collapsed, closeButton.Visibility);

            owner.Close();
        });
    }

    private static async Task<(Window Owner, Wpf.Ui.Controls.MessageBox MessageBox)> ShowLoadedMessageBox(
        Action<Wpf.Ui.Controls.MessageBox> configure
    )
    {
        var loadedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
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
            configure(messageBox);
            messageBox.Loaded += (_, _) => loadedTcs.TrySetResult();

            _ = messageBox.ShowDialogAsync(false);
        });

        using var hangGuard = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await loadedTcs.Task.WaitAsync(hangGuard.Token);

        return (owner, messageBox);
    }
}

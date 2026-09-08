// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

public class ContentDialogClosingEventArgs : RoutedEventArgs
{
    private int _outstandingDeferrals;

    private Action<ContentDialogClosingEventArgs>? _resume;

    private bool _resumed;

    public ContentDialogClosingEventArgs(RoutedEvent routedEvent, object source)
        : base(routedEvent, source) { }

    public required ContentDialogResult Result { get; init; }

    public bool Cancel { get; set; }

    /// <summary>
    /// Takes a deferral that keeps the dialog open until <see cref="ContentDialogClosingDeferral.Complete"/> is called.
    /// </summary>
    /// <returns>The deferral to complete once the handler's work finishes.</returns>
    /// <remarks>
    /// Take the deferral before the handler returns. <see cref="Cancel"/> is read again when the last outstanding deferral completes.
    /// </remarks>
    public ContentDialogClosingDeferral GetDeferral()
    {
        _outstandingDeferrals++;

        return new ContentDialogClosingDeferral(CompleteDeferral);
    }

    internal bool TryDefer(Action<ContentDialogClosingEventArgs> resume)
    {
        if (_outstandingDeferrals == 0)
        {
            return false;
        }

        _resume = resume;

        return true;
    }

    private void CompleteDeferral()
    {
        _outstandingDeferrals--;

        if (_outstandingDeferrals > 0 || _resumed || _resume is null)
        {
            return;
        }

        _resumed = true;

        _resume(this);
    }
}

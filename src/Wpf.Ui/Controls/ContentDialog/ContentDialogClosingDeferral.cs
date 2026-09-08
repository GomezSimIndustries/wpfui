// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Holds a <see cref="ContentDialog"/> open while a handler of its closing event finishes asynchronous work.
/// </summary>
/// <remarks>
/// Complete the deferral on the dispatcher thread that owns the dialog. The dialog settles once every deferral taken during one closing pass is completed.
/// </remarks>
public sealed class ContentDialogClosingDeferral
{
    private readonly Action _completion;

    private bool _isCompleted;

    internal ContentDialogClosingDeferral(Action completion)
    {
        _completion = completion;
    }

    /// <summary>
    /// Reports that the deferred work is finished.
    /// </summary>
    public void Complete()
    {
        if (_isCompleted)
        {
            return;
        }

        _isCompleted = true;

        _completion();
    }
}

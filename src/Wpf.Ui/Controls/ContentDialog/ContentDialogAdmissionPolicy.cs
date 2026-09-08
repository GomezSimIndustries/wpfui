// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Specifies what a <see cref="ContentDialogHost"/> does with a show request that arrives while it already presents a dialog.
/// </summary>
public enum ContentDialogAdmissionPolicy
{
    /// <summary>
    /// The incoming dialog takes the host and the presented dialog is displaced.
    /// </summary>
    Replace,

    /// <summary>
    /// The incoming request is refused with an <see cref="InvalidOperationException"/> and the presented dialog keeps the host.
    /// </summary>
    Reject,

    /// <summary>
    /// The incoming request waits and is presented in arrival order once the host is free.
    /// </summary>
    Queue,
}

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Tray;

/// <summary>
/// Sends Shell_NotifyIcon requests to the shell.
/// </summary>
internal interface IShellNotifyIconGateway
{
    /// <summary>
    /// Sends a Shell_NotifyIcon request and reports whether the shell accepted it.
    /// </summary>
    /// <param name="message">The Shell_NotifyIcon message to send.</param>
    /// <param name="data">The icon data for the request.</param>
    bool Send(Interop.Shell32.NIM message, Interop.Shell32.NOTIFYICONDATA data);
}

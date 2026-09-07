// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Tray;

/// <summary>
/// Sends Shell_NotifyIcon requests through the Shell32 P/Invoke.
/// </summary>
internal sealed class Shell32NotifyIconGateway : IShellNotifyIconGateway
{
    /// <summary>
    /// Gets the shared instance used in production.
    /// </summary>
    public static Shell32NotifyIconGateway Instance { get; } = new();

    /// <inheritdoc />
    public bool Send(Interop.Shell32.NIM message, Interop.Shell32.NOTIFYICONDATA data)
    {
        return Interop.Shell32.Shell_NotifyIcon(message, data);
    }
}

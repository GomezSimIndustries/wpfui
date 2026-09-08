// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Tray;

/// <summary>
/// Destroys native icon handles through the User32 P/Invoke.
/// </summary>
internal sealed class User32NativeIconHandleGateway : INativeIconHandleGateway
{
    /// <summary>
    /// Gets the shared instance used in production.
    /// </summary>
    public static User32NativeIconHandleGateway Instance { get; } = new();

    /// <inheritdoc />
    public bool Destroy(IntPtr handle)
    {
        return Interop.User32.DestroyIcon(handle);
    }
}

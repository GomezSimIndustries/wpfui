// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Tray;

/// <summary>
/// Destroys native icon handles owned by the tray icon lifecycle.
/// </summary>
internal interface INativeIconHandleGateway
{
    /// <summary>
    /// Destroys the given icon handle.
    /// </summary>
    /// <param name="handle">The handle to destroy.</param>
    bool Destroy(IntPtr handle);
}

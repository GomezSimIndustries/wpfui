// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Tray;

/// <summary>
/// Represents the native message-only window backing a registered tray icon.
/// </summary>
internal interface ITrayHookWindow : IDisposable
{
    /// <summary>
    /// Gets the window handle receiving shell notifications for the icon.
    /// </summary>
    IntPtr Handle { get; }

    /// <summary>
    /// Adds a message hook invoked for every message the window receives.
    /// </summary>
    /// <param name="hook">The hook to add.</param>
    void AddHook(HwndSourceHook hook);

    /// <summary>
    /// Removes a message hook added by <see cref="AddHook"/>.
    /// </summary>
    /// <param name="hook">The hook to remove.</param>
    void RemoveHook(HwndSourceHook hook);
}

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Threading;

namespace Wpf.Ui.Tray;

/// <summary>
/// Singleton containing persistent information about icons in the tray menu for application session.
/// </summary>
internal static class TrayData
{
    private static int _lastAllocatedId;

    /// <summary>
    /// Gets or sets the collection of registered tray icons.
    /// </summary>
    public static List<INotifyIcon> NotifyIcons { get; set; } = new();

    /// <summary>
    /// Allocates a tray icon identifier that has never been handed out before.
    /// </summary>
    public static int AllocateId()
    {
        return Interlocked.Increment(ref _lastAllocatedId);
    }
}

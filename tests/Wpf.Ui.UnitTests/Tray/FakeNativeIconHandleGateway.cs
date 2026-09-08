// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using Wpf.Ui.Tray;
using Wpf.Ui.Tray.Interop;

namespace Wpf.Ui.UnitTests.Tray;

/// <summary>
/// Records every handle passed to <see cref="Destroy"/> and forwards it to the real DestroyIcon call.
/// </summary>
internal sealed class FakeNativeIconHandleGateway : INativeIconHandleGateway
{
    private readonly HashSet<IntPtr> _seen = new();

    public List<IntPtr> DestroyedHandles { get; } = new();

    public int DuplicateDestroyCount { get; private set; }

    public bool Destroy(IntPtr handle)
    {
        DestroyedHandles.Add(handle);

        if (!_seen.Add(handle))
        {
            DuplicateDestroyCount++;
        }

        return User32.DestroyIcon(handle);
    }
}

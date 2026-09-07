// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Windows.Interop;
using Wpf.Ui.Tray;

namespace Wpf.Ui.UnitTests.Tray;

/// <summary>
/// A hook window test double that records hooks and disposal without creating a native window.
/// </summary>
internal sealed class FakeTrayHookWindow : ITrayHookWindow
{
    private readonly List<HwndSourceHook> _hooks = new();

    public FakeTrayHookWindow(IntPtr handle)
    {
        Handle = handle;
    }

    public IntPtr Handle { get; }

    public bool Disposed { get; private set; }

    public IReadOnlyList<HwndSourceHook> Hooks => _hooks;

    public void AddHook(HwndSourceHook hook)
    {
        _hooks.Add(hook);
    }

    public void RemoveHook(HwndSourceHook hook)
    {
        _hooks.Remove(hook);
    }

    public void Dispose()
    {
        Disposed = true;
    }
}

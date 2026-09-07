// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Tray;
using Wpf.Ui.Tray.Interop;

namespace Wpf.Ui.UnitTests.Tray;

/// <summary>
/// A bare <see cref="INotifyIcon"/> test double with no Win32 or shell dependencies.
/// </summary>
internal sealed class FakeNotifyIcon : INotifyIcon
{
    public Shell32.NOTIFYICONDATA ShellIconData { get; set; } = null!;

    public bool IsRegistered { get; set; }

    public int Id { get; set; } = -1;

    public string TooltipText { get; set; } = string.Empty;

    public ImageSource? Icon { get; set; }

    public ITrayHookWindow HookWindow { get; set; } = null!;

    public ContextMenu? ContextMenu { get; set; }

    public bool FocusOnLeftClick { get; set; }

    public bool MenuOnRightClick { get; set; }

    public IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        return IntPtr.Zero;
    }

    public bool Register()
    {
        return TrayManager.Register(this);
    }

    public bool Register(Window parentWindow)
    {
        return TrayManager.Register(this, parentWindow);
    }

    public bool ModifyIcon()
    {
        return TrayManager.ModifyIcon(this);
    }

    public bool ModifyToolTip()
    {
        return TrayManager.ModifyToolTip(this);
    }

    public bool Unregister()
    {
        return TrayManager.Unregister(this);
    }
}

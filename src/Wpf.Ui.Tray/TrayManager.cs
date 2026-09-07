// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;

namespace Wpf.Ui.Tray;

/*
 * TODO: Handle closing of the parent window.
 * NOTE
 * The problem is as follows:
 * If the main window is closed with the Debugger or simply destroyed,
 * it will not send WM_CLOSE or WM_DESTROY to its child windows. This
 * way, we can't tell tray to close the icon. Thus, we need to add to
 * the TrayHandler a mechanism that detects that the parent window has
 * been closed and then send
 * Shell32.Shell_NotifyIcon(Shell32.NIM.DELETE, Shell32.NOTIFYICONDATA);
 *
 * In another situation, the TrayHandler can also be forced to close,
 * so there is need to detect from the side somehow if this has happened
 * and remove the icon.
 */

/// <summary>
/// Responsible for managing the icons in the Tray bar.
/// </summary>
internal static class TrayManager
{
    /// <summary>
    /// Gets or sets the gateway used to send Shell_NotifyIcon requests.
    /// </summary>
    internal static IShellNotifyIconGateway ShellGateway { get; set; } = Shell32NotifyIconGateway.Instance;

    /// <summary>
    /// Gets or sets the factory used to create the hook window backing a registered icon.
    /// </summary>
    internal static ITrayHookWindowFactory HookWindowFactory { get; set; } = TrayHookWindowFactory.Instance;

    public static bool Register(INotifyIcon notifyIcon)
    {
        if (notifyIcon is null)
        {
            return false;
        }

        return Register(notifyIcon, GetParentSource());
    }

    public static bool Register(INotifyIcon notifyIcon, Window parentWindow)
    {
        if (parentWindow == null)
        {
            return false;
        }

        return Register(notifyIcon, (HwndSource)PresentationSource.FromVisual(parentWindow));
    }

    public static bool Register(INotifyIcon notifyIcon, HwndSource? parentSource)
    {
        return RegisterCore(notifyIcon, parentSource is not null, parentSource?.Handle ?? IntPtr.Zero);
    }

    /// <summary>
    /// Registers the icon against the given parent handle, committing registration state only after the shell accepts the icon.
    /// </summary>
    /// <param name="notifyIcon">The icon to register.</param>
    /// <param name="hasParentSource">Whether a parent presentation source was resolved.</param>
    /// <param name="parentHandle">The parent window handle, used only to name the hook window.</param>
    /// <remarks>A rejected shell add returns false without retrying; the caller must invoke registration again to retry.</remarks>
    internal static bool RegisterCore(INotifyIcon notifyIcon, bool hasParentSource, IntPtr parentHandle)
    {
        if (!hasParentSource)
        {
            if (!notifyIcon.IsRegistered)
            {
                return false;
            }

            _ = Unregister(notifyIcon);

            return false;
        }

        if (parentHandle == IntPtr.Zero)
        {
            return false;
        }

        if (notifyIcon.IsRegistered)
        {
            _ = Unregister(notifyIcon);
        }

        int id = TrayData.AllocateId();

        // Use IntPtr.Zero as parent so the TrayHandler is a standalone top-level window.
        ITrayHookWindow hookWindow = HookWindowFactory.Create($"wpfui_th_{parentHandle}_{id}", IntPtr.Zero, id);

        var shellIconData = new Interop.Shell32.NOTIFYICONDATA
        {
            uID = id,
            uFlags = Interop.Shell32.NIF.MESSAGE,
            uCallbackMessage = (int)Interop.User32.WM.TRAYMOUSEMESSAGE,
            hWnd = hookWindow.Handle,
            dwState = 0x2,
        };

        if (!string.IsNullOrEmpty(notifyIcon.TooltipText))
        {
            shellIconData.szTip = notifyIcon.TooltipText;
            shellIconData.uFlags |= Interop.Shell32.NIF.TIP;
        }

        notifyIcon.Id = id;
        notifyIcon.HookWindow = hookWindow;
        notifyIcon.ShellIconData = shellIconData;

        ReloadHicon(notifyIcon);

        hookWindow.AddHook(notifyIcon.WndProc);

        bool added = ShellGateway.Send(Interop.Shell32.NIM.ADD, notifyIcon.ShellIconData);

        if (!added)
        {
            hookWindow.RemoveHook(notifyIcon.WndProc);
            hookWindow.Dispose();

            notifyIcon.HookWindow = null!;
            notifyIcon.Id = -1;
            notifyIcon.IsRegistered = false;

            return false;
        }

        TrayData.NotifyIcons.Add(notifyIcon);

        notifyIcon.IsRegistered = true;

        return true;
    }

    public static bool ModifyIcon(INotifyIcon notifyIcon)
    {
        if (!notifyIcon.IsRegistered)
        {
            return true;
        }

        ReloadHicon(notifyIcon);

        return ShellGateway.Send(Interop.Shell32.NIM.MODIFY, notifyIcon.ShellIconData);
    }

    public static bool ModifyToolTip(INotifyIcon notifyIcon)
    {
        if (!notifyIcon.IsRegistered)
        {
            return true;
        }

        notifyIcon.ShellIconData.szTip = notifyIcon.TooltipText;
        notifyIcon.ShellIconData.uFlags |= Interop.Shell32.NIF.TIP;

        return ShellGateway.Send(Interop.Shell32.NIM.MODIFY, notifyIcon.ShellIconData);
    }

    /// <summary>
    /// Tries to remove the <see cref="INotifyIcon"/> from the shell.
    /// </summary>
    public static bool Unregister(INotifyIcon notifyIcon)
    {
        if (notifyIcon.ShellIconData == null || !notifyIcon.IsRegistered)
        {
            return false;
        }

        _ = ShellGateway.Send(Interop.Shell32.NIM.DELETE, notifyIcon.ShellIconData);

        notifyIcon.IsRegistered = false;

        TrayData.NotifyIcons.Remove(notifyIcon);

        // Dispose the HookWindow now that the tray icon is unregistered.
        // Since the TrayHandler is a standalone top-level window (not parented),
        // it must be explicitly disposed to avoid orphaned HwndSource objects.
        notifyIcon.HookWindow?.RemoveHook(notifyIcon.WndProc);
        notifyIcon.HookWindow?.Dispose();
        notifyIcon.HookWindow = null!;

        notifyIcon.Id = -1;

        return true;
    }

    /// <summary>
    /// Gets application source.
    /// </summary>
    private static HwndSource? GetParentSource()
    {
        Window mainWindow = Application.Current.MainWindow;

        if (mainWindow == null)
        {
            return null;
        }

        return (HwndSource)PresentationSource.FromVisual(mainWindow);
    }

    private static void ReloadHicon(INotifyIcon notifyIcon)
    {
        IntPtr hIcon = IntPtr.Zero;

        if (notifyIcon.Icon is not null)
        {
            hIcon = Hicon.FromSource(notifyIcon.Icon);
        }

        if (hIcon == IntPtr.Zero)
        {
            hIcon = Hicon.FromApp();
        }

        if (hIcon != IntPtr.Zero)
        {
            notifyIcon.ShellIconData.hIcon = hIcon;
            notifyIcon.ShellIconData.uFlags |= Interop.Shell32.NIF.ICON;
        }
    }
}

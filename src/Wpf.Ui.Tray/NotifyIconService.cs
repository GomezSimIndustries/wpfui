// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Wpf.Ui.Tray;

/// <summary>
/// Base implementation of the notify icon service.
/// </summary>
public class NotifyIconService : INotifyIconService
{
    private readonly Internal.InternalNotifyIconManager internalNotifyIconManager;

    private bool _disposed;

    public Window ParentWindow { get; internal set; } = null!;

    public int Id => internalNotifyIconManager.Id;

    public bool IsRegistered => internalNotifyIconManager.IsRegistered;

    public string TooltipText
    {
        get => internalNotifyIconManager.TooltipText;
        set => internalNotifyIconManager.TooltipText = value;
    }

    public ContextMenu? ContextMenu
    {
        get => internalNotifyIconManager.ContextMenu;
        set => internalNotifyIconManager.ContextMenu = value;
    }

    public ImageSource? Icon
    {
        get => internalNotifyIconManager.Icon;
        set => internalNotifyIconManager.Icon = value;
    }

    public NotifyIconService()
    {
        internalNotifyIconManager = new Internal.InternalNotifyIconManager();

        RegisterHandlers();
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="NotifyIconService"/> class.
    /// </summary>
    ~NotifyIconService() => Dispose(false);

    public bool Register()
    {
        if (_disposed)
        {
            return false;
        }

        if (ParentWindow is not null)
        {
            return internalNotifyIconManager.Register(ParentWindow);
        }

        return internalNotifyIconManager.Register();
    }

    public bool Unregister()
    {
        return internalNotifyIconManager.Unregister();
    }

    /// <inheritdoc />
    public void SetParentWindow(Window parentWindow)
    {
        if (_disposed)
        {
            return;
        }

        DetachParentWindow();

        ParentWindow = parentWindow;
        ParentWindow.Closed += OnParentWindowClosed;
    }

    /// <summary>
    /// Releases the tray icon, detaches it from its parent window, and unregisters the mouse handlers.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// This virtual method is called when the user clicks the left mouse button on the tray icon.
    /// </summary>
    protected virtual void OnLeftClick() { }

    /// <summary>
    /// This virtual method is called when the user double-clicks the left mouse button on the tray icon.
    /// </summary>
    protected virtual void OnLeftDoubleClick() { }

    /// <summary>
    /// This virtual method is called when the user clicks the right mouse button on the tray icon.
    /// </summary>
    protected virtual void OnRightClick() { }

    /// <summary>
    /// This virtual method is called when the user double-clicks the right mouse button on the tray icon.
    /// </summary>
    protected virtual void OnRightDoubleClick() { }

    /// <summary>
    /// This virtual method is called when the user clicks the middle mouse button on the tray icon.
    /// </summary>
    protected virtual void OnMiddleClick() { }

    /// <summary>
    /// This virtual method is called when the user double-clicks the middle mouse button on the tray icon.
    /// </summary>
    protected virtual void OnMiddleDoubleClick() { }

    /// <summary>
    /// Releases the tray icon, the parent window subscription and the mouse handlers.
    /// </summary>
    /// <param name="disposing">If disposing equals <see langword="true"/>, dispose all managed and unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!disposing)
        {
            return;
        }

        DetachParentWindow();
        UnregisterHandlers();

        internalNotifyIconManager.Dispose();
    }

    private void OnParentWindowClosed(object? sender, EventArgs e)
    {
        Dispose();
    }

    private void DetachParentWindow()
    {
        if (ParentWindow is not null)
        {
            ParentWindow.Closed -= OnParentWindowClosed;
        }
    }

    private void RegisterHandlers()
    {
        internalNotifyIconManager.LeftClick += OnLeftClick;
        internalNotifyIconManager.LeftDoubleClick += OnLeftDoubleClick;
        internalNotifyIconManager.RightClick += OnRightClick;
        internalNotifyIconManager.RightDoubleClick += OnRightDoubleClick;
        internalNotifyIconManager.MiddleClick += OnMiddleClick;
        internalNotifyIconManager.MiddleDoubleClick += OnMiddleDoubleClick;
    }

    private void UnregisterHandlers()
    {
        internalNotifyIconManager.LeftClick -= OnLeftClick;
        internalNotifyIconManager.LeftDoubleClick -= OnLeftDoubleClick;
        internalNotifyIconManager.RightClick -= OnRightClick;
        internalNotifyIconManager.RightDoubleClick -= OnRightDoubleClick;
        internalNotifyIconManager.MiddleClick -= OnMiddleClick;
        internalNotifyIconManager.MiddleDoubleClick -= OnMiddleDoubleClick;
    }
}

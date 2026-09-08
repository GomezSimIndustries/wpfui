// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.TaskBar;

/// <summary>
/// Sends taskbar progress requests to the shell.
/// </summary>
internal interface INativeTaskBarProgressGateway
{
    /// <summary>
    /// Sets the taskbar progress state for the selected window and reports whether the shell accepted it.
    /// </summary>
    /// <param name="window">Window to modify.</param>
    /// <param name="taskBarProgressState">Progress state to set.</param>
    bool SetState(Window? window, TaskBarProgressState taskBarProgressState);

    /// <summary>
    /// Sets the taskbar progress state for the selected window handle and reports whether the shell accepted it.
    /// </summary>
    /// <param name="hWnd">Window handle to modify.</param>
    /// <param name="taskBarProgressState">Progress state to set.</param>
    bool SetState(IntPtr hWnd, TaskBarProgressState taskBarProgressState);

    /// <summary>
    /// Sets the taskbar progress value for the selected window and reports whether the shell accepted it.
    /// </summary>
    /// <param name="window">Window to modify.</param>
    /// <param name="taskBarProgressState">Progress state to set.</param>
    /// <param name="current">Current value to display.</param>
    /// <param name="total">Maximum number for division.</param>
    bool SetValue(Window? window, TaskBarProgressState taskBarProgressState, int current, int total);

    /// <summary>
    /// Sets the taskbar progress value for the selected window handle and reports whether the shell accepted it.
    /// </summary>
    /// <param name="hWnd">Window handle to modify.</param>
    /// <param name="taskBarProgressState">Progress state to set.</param>
    /// <param name="current">Current value to display.</param>
    /// <param name="total">Maximum number for division.</param>
    bool SetValue(IntPtr hWnd, TaskBarProgressState taskBarProgressState, int current, int total);
}

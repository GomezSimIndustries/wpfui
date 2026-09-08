// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.TaskBar;

/// <summary>
/// Sends taskbar progress requests through <see cref="TaskBarProgress"/>.
/// </summary>
internal sealed class TaskBarProgressGateway : INativeTaskBarProgressGateway
{
    /// <summary>
    /// Gets the shared instance used in production.
    /// </summary>
    public static TaskBarProgressGateway Instance { get; } = new();

    /// <inheritdoc />
    public bool SetState(Window? window, TaskBarProgressState taskBarProgressState)
    {
        return TaskBarProgress.SetState(window, taskBarProgressState);
    }

    /// <inheritdoc />
    public bool SetState(IntPtr hWnd, TaskBarProgressState taskBarProgressState)
    {
        return TaskBarProgress.SetState(hWnd, taskBarProgressState);
    }

    /// <inheritdoc />
    public bool SetValue(Window? window, TaskBarProgressState taskBarProgressState, int current, int total)
    {
        return TaskBarProgress.SetValue(window, taskBarProgressState, current, total);
    }

    /// <inheritdoc />
    public bool SetValue(IntPtr hWnd, TaskBarProgressState taskBarProgressState, int current, int total)
    {
        return TaskBarProgress.SetValue(hWnd, taskBarProgressState, current, total);
    }
}

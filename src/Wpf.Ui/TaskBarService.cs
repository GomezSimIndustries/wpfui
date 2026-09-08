// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.TaskBar;

namespace Wpf.Ui;

/// <summary>
/// Allows you to manage the animations of the window icon in the taskbar.
/// </summary>
public partial class TaskBarService : ITaskBarService
{
    /// <summary>
    /// Gets or sets the gateway used to send taskbar progress requests.
    /// </summary>
    internal static INativeTaskBarProgressGateway ProgressGateway { get; set; } = TaskBarProgressGateway.Instance;

    private readonly Dictionary<IntPtr, TaskBarProgressState> _progressStates = [];

    /// <inheritdoc />
    public virtual TaskBarProgressState GetState(IntPtr hWnd)
    {
        if (!_progressStates.TryGetValue(hWnd, out TaskBarProgressState progressState))
        {
            return TaskBarProgressState.None;
        }

        return progressState;
    }

    /// <inheritdoc />
    public virtual TaskBarProgressState GetState(Window? window)
    {
        if (window is null)
        {
            return TaskBarProgressState.None;
        }

        IntPtr windowHandle = new WindowInteropHelper(window).Handle;

        if (!_progressStates.TryGetValue(windowHandle, out TaskBarProgressState progressState))
        {
            return TaskBarProgressState.None;
        }

        return progressState;
    }

    /// <inheritdoc />
    public virtual bool SetState(Window? window, TaskBarProgressState taskBarProgressState)
    {
        if (window is null)
        {
            return false;
        }

        bool result = ProgressGateway.SetState(window, taskBarProgressState);

        if (result)
        {
            IntPtr windowHandle = new WindowInteropHelper(window).Handle;
            StoreProgressState(windowHandle, taskBarProgressState);
        }

        return result;
    }

    /// <inheritdoc />
    public virtual bool SetValue(
        Window? window,
        TaskBarProgressState taskBarProgressState,
        int current,
        int total
    )
    {
        if (window is null)
        {
            return false;
        }

        bool result = ProgressGateway.SetValue(window, taskBarProgressState, current, total);

        if (result)
        {
            IntPtr windowHandle = new WindowInteropHelper(window).Handle;
            StoreProgressState(windowHandle, taskBarProgressState);
        }

        return result;
    }

    /// <inheritdoc />
    public virtual bool SetValue(Window? window, int current, int total)
    {
        if (window == null)
        {
            return false;
        }

        IntPtr windowHandle = new WindowInteropHelper(window).Handle;

        if (!_progressStates.TryGetValue(windowHandle, out TaskBarProgressState progressState))
        {
            return ProgressGateway.SetValue(window, TaskBarProgressState.Normal, current, total);
        }

        return ProgressGateway.SetValue(window, progressState, current, total);
    }

    /// <inheritdoc />
    public virtual bool SetState(IntPtr hWnd, TaskBarProgressState taskBarProgressState)
    {
        bool result = ProgressGateway.SetState(hWnd, taskBarProgressState);

        if (result)
        {
            StoreProgressState(hWnd, taskBarProgressState);
        }

        return result;
    }

    /// <inheritdoc/>
    public virtual bool SetValue(
        IntPtr hWnd,
        TaskBarProgressState taskBarProgressState,
        int current,
        int total
    )
    {
        bool result = ProgressGateway.SetValue(hWnd, taskBarProgressState, current, total);

        if (result)
        {
            StoreProgressState(hWnd, taskBarProgressState);
        }

        return result;
    }

    /// <inheritdoc />
    public virtual bool SetValue(IntPtr hWnd, int current, int total)
    {
        if (!_progressStates.TryGetValue(hWnd, out TaskBarProgressState progressState))
        {
            return ProgressGateway.SetValue(hWnd, TaskBarProgressState.Normal, current, total);
        }

        return ProgressGateway.SetValue(hWnd, progressState, current, total);
    }

    /// <summary>
    /// Records the last successfully requested progress state for a window handle, dropping the entry once the state clears to <see cref="TaskBarProgressState.None"/>.
    /// </summary>
    /// <param name="hWnd">Window handle the state was requested for.</param>
    /// <param name="taskBarProgressState">Progress state that was successfully requested.</param>
    private void StoreProgressState(IntPtr hWnd, TaskBarProgressState taskBarProgressState)
    {
        if (hWnd == IntPtr.Zero)
        {
            return;
        }

        if (taskBarProgressState == TaskBarProgressState.None)
        {
            _ = _progressStates.Remove(hWnd);
        }
        else
        {
            _progressStates[hWnd] = taskBarProgressState;
        }
    }
}

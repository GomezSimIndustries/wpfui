// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Runtime.CompilerServices;
using Wpf.Ui.Interop;

namespace Wpf.Ui.TaskBar;

/// <summary>
/// Allows to change the status of the displayed notification in the application icon on the TaskBar.
/// </summary>
public static class TaskBarProgress
{
    private static readonly ConditionalWeakTable<Window, PendingRequest> PendingRequests = new();

    /// <summary>
    /// Gets a value indicating whether the current operating system supports task bar manipulation.
    /// </summary>
    private static bool IsSupported()
    {
        return Win32.Utilities.IsOSWindows7OrNewer;
    }

    /// <summary>
    /// Allows to change the status of the progress bar in the task bar.
    /// </summary>
    /// <param name="window">Window to manipulate.</param>
    /// <param name="taskBarProgressState">State of the progress indicator.</param>
    /// <returns><see langword="true"/> if the state was applied immediately; <see langword="false"/> if the window has not loaded yet and the request was queued for when it does.</returns>
    public static bool SetState(Window? window, TaskBarProgressState taskBarProgressState)
    {
        if (window is null)
        {
            return false;
        }

        if (window.IsLoaded)
        {
            return ApplyState(window, taskBarProgressState);
        }

        QueueRequest(window, w => ApplyState(w, taskBarProgressState));

        return false;
    }

    /// <summary>
    /// Allows to change the status of the progress bar in the task bar.
    /// </summary>
    /// <param name="hWnd">Window handle.</param>
    /// <param name="taskBarProgressState">State of the progress indicator.</param>
    public static bool SetState(IntPtr hWnd, TaskBarProgressState taskBarProgressState)
    {
        if (!IsSupported())
        {
            throw new Exception("Taskbar functions not available.");
        }

        return UnsafeNativeMethods.SetTaskbarState(hWnd, UnsafeReflection.Cast(taskBarProgressState));
    }

    /// <summary>
    /// Allows to change the fill of the task bar.
    /// </summary>
    /// <param name="window">Window to manipulate.</param>
    /// <param name="taskBarProgressState">Progress sate to set.</param>
    /// <param name="current">Current value to display</param>
    public static bool SetValue(Window window, TaskBarProgressState taskBarProgressState, int current)
    {
        if (current > 100)
        {
            current = 100;
        }

        if (current < 0)
        {
            current = 0;
        }

        return SetValue(window, taskBarProgressState, current, 100);
    }

    /// <summary>
    /// Allows to change the fill of the task bar.
    /// </summary>
    /// <param name="window">Window to manipulate.</param>
    /// <param name="taskBarProgressState">Progress sate to set.</param>
    /// <param name="current">Current value to display</param>
    /// <param name="total">Total number for division.</param>
    /// <returns><see langword="true"/> if the value was applied immediately; <see langword="false"/> if the window has not loaded yet and the request was queued for when it does.</returns>
    public static bool SetValue(
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

        if (window.IsLoaded)
        {
            return ApplyValue(window, taskBarProgressState, current, total);
        }

        QueueRequest(window, w => ApplyValue(w, taskBarProgressState, current, total));

        return false;
    }

    /// <summary>
    /// Allows to change the fill of the task bar.
    /// </summary>
    /// <param name="hWnd">Window handle.</param>
    /// <param name="taskBarProgressState">Progress sate to set.</param>
    /// <param name="current">Current value to display</param>
    public static bool SetValue(IntPtr hWnd, TaskBarProgressState taskBarProgressState, int current)
    {
        if (current > 100)
        {
            current = 100;
        }

        if (current < 0)
        {
            current = 0;
        }

        return SetValue(hWnd, taskBarProgressState, current, 100);
    }

    /// <summary>
    /// Allows to change the fill of the task bar.
    /// </summary>
    /// <param name="hWnd">Window handle.</param>
    /// <param name="taskBarProgressState">Progress sate to set.</param>
    /// <param name="current">Current value to display</param>
    /// <param name="total">Total number for division.</param>
    public static bool SetValue(
        IntPtr hWnd,
        TaskBarProgressState taskBarProgressState,
        int current,
        int total
    )
    {
        if (!IsSupported())
        {
            throw new Exception("Taskbar functions not available.");
        }

        return UnsafeNativeMethods.SetTaskbarValue(
            hWnd,
            UnsafeReflection.Cast(taskBarProgressState),
            current,
            total
        );
    }

    /// <summary>
    /// Gets a value indicating whether a window still has a queued pre-load request.
    /// </summary>
    /// <param name="window">Window to check.</param>
    internal static bool HasPendingRequest(Window window) => PendingRequests.TryGetValue(window, out _);

    private static bool ApplyState(Window window, TaskBarProgressState taskBarProgressState) =>
        TaskBarService.ProgressGateway.SetState(new WindowInteropHelper(window).Handle, taskBarProgressState);

    private static bool ApplyValue(
        Window window,
        TaskBarProgressState taskBarProgressState,
        int current,
        int total
    ) =>
        TaskBarService.ProgressGateway.SetValue(
            new WindowInteropHelper(window).Handle,
            taskBarProgressState,
            current,
            total
        );

    /// <summary>
    /// Replaces any queued pre-load request for the window with a single one-shot Loaded subscription.
    /// </summary>
    private static void QueueRequest(Window window, Func<Window, bool> apply)
    {
        RemovePendingRequest(window);

        RoutedEventHandler loadedHandler = (_, _) =>
        {
            RemovePendingRequest(window);
            _ = apply(window);
        };

        EventHandler closedHandler = (_, _) =>
        {
            RemovePendingRequest(window);
        };

        PendingRequests.Add(window, new PendingRequest(loadedHandler, closedHandler));
        window.Loaded += loadedHandler;
        window.Closed += closedHandler;
    }

    /// <summary>
    /// Unsubscribes and drops the window's queued pre-load request, if any.
    /// </summary>
    private static void RemovePendingRequest(Window window)
    {
        if (!PendingRequests.TryGetValue(window, out PendingRequest? pending))
        {
            return;
        }

        window.Loaded -= pending.LoadedHandler;
        window.Closed -= pending.ClosedHandler;
        PendingRequests.Remove(window);
    }

    /// <summary>
    /// Holds the handlers attached for a window's queued pre-load request.
    /// </summary>
    private sealed class PendingRequest(RoutedEventHandler loadedHandler, EventHandler closedHandler)
    {
        /// <summary>
        /// Gets the handler that applies the request once the window loads.
        /// </summary>
        public RoutedEventHandler LoadedHandler { get; } = loadedHandler;

        /// <summary>
        /// Gets the handler that drops the request if the window closes before loading.
        /// </summary>
        public EventHandler ClosedHandler { get; } = closedHandler;
    }
}

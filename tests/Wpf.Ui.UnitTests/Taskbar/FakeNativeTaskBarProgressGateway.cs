// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Windows.Interop;
using Wpf.Ui.TaskBar;

namespace Wpf.Ui.UnitTests.Taskbar;

/// <summary>
/// Records every taskbar progress request and replays a configured result or exception.
/// </summary>
internal sealed class FakeNativeTaskBarProgressGateway : INativeTaskBarProgressGateway
{
    private readonly Queue<bool> _results;

    public FakeNativeTaskBarProgressGateway(params bool[] results)
    {
        _results = new Queue<bool>(results);
    }

    public List<(IntPtr Handle, TaskBarProgressState State)> StateCalls { get; } = new();

    public List<(IntPtr Handle, TaskBarProgressState State, int Current, int Total)> ValueCalls { get; } = new();

    public Exception? ThrowOnNextCall { get; set; }

    public bool SetState(Window? window, TaskBarProgressState taskBarProgressState) =>
        SetState(HandleOf(window), taskBarProgressState);

    public bool SetState(IntPtr hWnd, TaskBarProgressState taskBarProgressState)
    {
        RaiseIfArmed();
        StateCalls.Add((hWnd, taskBarProgressState));

        return NextResult();
    }

    public bool SetValue(Window? window, TaskBarProgressState taskBarProgressState, int current, int total) =>
        SetValue(HandleOf(window), taskBarProgressState, current, total);

    public bool SetValue(IntPtr hWnd, TaskBarProgressState taskBarProgressState, int current, int total)
    {
        RaiseIfArmed();
        ValueCalls.Add((hWnd, taskBarProgressState, current, total));

        return NextResult();
    }

    private static IntPtr HandleOf(Window? window) =>
        window is null ? IntPtr.Zero : new WindowInteropHelper(window).Handle;

    private void RaiseIfArmed()
    {
        if (ThrowOnNextCall is { } exception)
        {
            ThrowOnNextCall = null;

            throw exception;
        }
    }

    private bool NextResult() => _results.Count > 0 ? _results.Dequeue() : true;
}

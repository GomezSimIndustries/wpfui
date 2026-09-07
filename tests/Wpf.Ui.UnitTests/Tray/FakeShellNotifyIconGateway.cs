// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using Wpf.Ui.Tray;
using Wpf.Ui.Tray.Interop;

namespace Wpf.Ui.UnitTests.Tray;

/// <summary>
/// Records Shell_NotifyIcon requests and replays a fixed sequence of shell responses.
/// </summary>
internal sealed class FakeShellNotifyIconGateway : IShellNotifyIconGateway
{
    private readonly Queue<bool> _results;

    public FakeShellNotifyIconGateway(params bool[] results)
    {
        _results = new Queue<bool>(results);
    }

    public List<(Shell32.NIM Message, int Id)> Calls { get; } = new();

    public bool Send(Shell32.NIM message, Shell32.NOTIFYICONDATA data)
    {
        Calls.Add((message, data.uID));

        return _results.Count > 0 ? _results.Dequeue() : true;
    }
}

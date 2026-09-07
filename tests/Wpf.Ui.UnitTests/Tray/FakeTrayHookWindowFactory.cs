// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using Wpf.Ui.Tray;

namespace Wpf.Ui.UnitTests.Tray;

/// <summary>
/// Creates <see cref="FakeTrayHookWindow"/> instances and records each one created.
/// </summary>
internal sealed class FakeTrayHookWindowFactory : ITrayHookWindowFactory
{
    private int _nextHandle = 1;

    public List<FakeTrayHookWindow> Created { get; } = new();

    public ITrayHookWindow Create(string name, IntPtr parent, int elementId)
    {
        var window = new FakeTrayHookWindow(new IntPtr(_nextHandle++));

        Created.Add(window);

        return window;
    }
}

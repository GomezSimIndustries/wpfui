// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Tray;

/// <summary>
/// Creates standalone <see cref="TrayHandler"/> windows.
/// </summary>
internal sealed class TrayHookWindowFactory : ITrayHookWindowFactory
{
    /// <summary>
    /// Gets the shared instance used in production.
    /// </summary>
    public static TrayHookWindowFactory Instance { get; } = new();

    /// <inheritdoc />
    public ITrayHookWindow Create(string name, IntPtr parent, int elementId)
    {
        return new TrayHandler(name, parent) { ElementId = elementId };
    }
}

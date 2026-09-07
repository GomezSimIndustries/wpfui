// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Tray;

/// <summary>
/// Creates the native hook window backing a registered tray icon.
/// </summary>
internal interface ITrayHookWindowFactory
{
    /// <summary>
    /// Creates a standalone hook window for a tray icon.
    /// </summary>
    /// <param name="name">The window name.</param>
    /// <param name="parent">The parent window handle.</param>
    /// <param name="elementId">The tray icon identifier the window is hooked for.</param>
    ITrayHookWindow Create(string name, IntPtr parent, int elementId);
}

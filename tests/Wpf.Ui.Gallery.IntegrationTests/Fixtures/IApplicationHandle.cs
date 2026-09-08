// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using FlaUI.Core;
using FlaUI.Core.AutomationElements;

namespace Wpf.Ui.Gallery.IntegrationTests.Fixtures;

/// <summary>
/// Represents a launched application process.
/// </summary>
internal interface IApplicationHandle : IDisposable
{
    /// <summary>
    /// Gets the underlying <see cref="FlaUI.Core.Application"/> wrapper, when one exists.
    /// </summary>
    Application? Underlying { get; }

    /// <summary>
    /// Gets a value indicating whether the process has exited.
    /// </summary>
    bool HasExited { get; }

    /// <summary>
    /// Requests that the application close.
    /// </summary>
    void Close();

    /// <summary>
    /// Gets the application's main window.
    /// </summary>
    /// <param name="automation">The automation instance used to resolve the window.</param>
    Window? GetMainWindow(AutomationBase automation);
}

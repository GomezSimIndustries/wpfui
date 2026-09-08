// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls.Primitives;

namespace Wpf.Ui.UnitTests.Controls;

/// <summary>Popup that can raise its Opened and Closed events directly, without opening a real popup window.</summary>
public sealed class RaisablePopup : Popup
{
    /// <summary>Raises the Opened event.</summary>
    public void RaiseOpenedForTest() => OnOpened(EventArgs.Empty);

    /// <summary>Raises the Closed event.</summary>
    public void RaiseClosedForTest() => OnClosed(EventArgs.Empty);
}

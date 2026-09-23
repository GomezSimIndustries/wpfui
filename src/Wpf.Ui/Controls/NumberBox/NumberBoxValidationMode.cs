// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// This Source Code is partially based on the source code provided by the .NET Foundation.

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Defines values that specify how a <see cref="NumberBox"/> reconciles typed text with <see cref="NumberBox.Value"/>.
/// </summary>
public enum NumberBoxValidationMode
{
    /// <summary>
    /// At commit, overwrites text that fails to parse as a number with the current <see cref="NumberBox.Value"/> and coerces the value into the
    /// <see cref="NumberBox.Minimum"/>/<see cref="NumberBox.Maximum"/> range.
    /// </summary>
    InvalidInputOverwritten,

    /// <summary>
    /// Leaves unparseable text as the user typed it and never coerces a committed <see cref="NumberBox.Value"/> into the
    /// <see cref="NumberBox.Minimum"/>/<see cref="NumberBox.Maximum"/> range; a spin step still stops at the bound.
    /// </summary>
    Disabled,
}

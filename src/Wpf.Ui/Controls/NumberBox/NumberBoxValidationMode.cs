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
    /// Overwrites text that fails to parse as a number with the current <see cref="NumberBox.Value"/> when input is committed.
    /// </summary>
    InvalidInputOverwritten,

    /// <summary>
    /// Defers <see cref="NumberBox.Value"/> updates to commit and leaves unparseable text as the user typed it.
    /// </summary>
    Disabled,
}

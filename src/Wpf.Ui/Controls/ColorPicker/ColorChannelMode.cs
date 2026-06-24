// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Defines the channel input mode for the color picker.
/// </summary>
public enum ColorChannelMode
{
    /// <summary>
    /// Show RGB (Red, Green, Blue) channel inputs.
    /// </summary>
    Rgb,

    /// <summary>
    /// Show HSV (Hue, Saturation, Value) channel inputs.
    /// </summary>
    Hsv,
}

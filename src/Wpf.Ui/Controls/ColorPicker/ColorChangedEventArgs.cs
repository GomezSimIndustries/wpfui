// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Media;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Provides data for the <see cref="ColorPicker.ColorChanged"/> event.
/// </summary>
public class ColorChangedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Gets the previous color value.
    /// </summary>
    public Color OldColor { get; }

    /// <summary>
    /// Gets the new color value.
    /// </summary>
    public Color NewColor { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ColorChangedEventArgs"/> class.
    /// </summary>
    public ColorChangedEventArgs(RoutedEvent routedEvent, object source, Color oldColor, Color newColor)
        : base(routedEvent, source)
    {
        OldColor = oldColor;
        NewColor = newColor;
    }
}

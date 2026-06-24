// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// A slider control with a dynamic gradient track for use in color pickers.
/// </summary>
internal class ColorSlider : Slider
{
    private Rectangle? _trackBackground;

    /// <summary>Identifies the <see cref="LeftColor"/> dependency property.</summary>
    public static readonly DependencyProperty LeftColorProperty = DependencyProperty.Register(
        nameof(LeftColor),
        typeof(Color),
        typeof(ColorSlider),
        new PropertyMetadata(Colors.Black, OnGradientColorChanged));

    /// <summary>Identifies the <see cref="RightColor"/> dependency property.</summary>
    public static readonly DependencyProperty RightColorProperty = DependencyProperty.Register(
        nameof(RightColor),
        typeof(Color),
        typeof(ColorSlider),
        new PropertyMetadata(Colors.White, OnGradientColorChanged));

    /// <summary>Identifies the <see cref="ShowCheckerboard"/> dependency property.</summary>
    public static readonly DependencyProperty ShowCheckerboardProperty = DependencyProperty.Register(
        nameof(ShowCheckerboard),
        typeof(bool),
        typeof(ColorSlider),
        new PropertyMetadata(false));

    /// <summary>Gets or sets the color at the left (minimum) end of the slider.</summary>
    public Color LeftColor
    {
        get => (Color)GetValue(LeftColorProperty);
        set => SetValue(LeftColorProperty, value);
    }

    /// <summary>Gets or sets the color at the right (maximum) end of the slider.</summary>
    public Color RightColor
    {
        get => (Color)GetValue(RightColorProperty);
        set => SetValue(RightColorProperty, value);
    }

    /// <summary>Gets or sets whether to show a checkerboard pattern behind the gradient (for alpha sliders).</summary>
    public bool ShowCheckerboard
    {
        get => (bool)GetValue(ShowCheckerboardProperty);
        set => SetValue(ShowCheckerboardProperty, value);
    }

    static ColorSlider()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorSlider), new FrameworkPropertyMetadata(typeof(ColorSlider)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _trackBackground = GetTemplateChild("PART_TrackBackground") as Rectangle;
        UpdateGradient();
    }

    private void UpdateGradient()
    {
        if (_trackBackground == null)
        {
            return;
        }

#pragma warning disable WPF0041 // Template child — no external binding to preserve
        _trackBackground.Fill = new LinearGradientBrush(LeftColor, RightColor, 0.0);
#pragma warning restore WPF0041
    }

    private static void OnGradientColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorSlider slider)
        {
            slider.UpdateGradient();
        }
    }
}

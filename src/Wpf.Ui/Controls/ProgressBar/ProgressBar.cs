// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Extends <see cref="System.Windows.Controls.ProgressBar"/> with a secondary value indicator.
/// </summary>
[TemplatePart(Name = SecondaryIndicatorName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = SecondaryMarkerName, Type = typeof(FrameworkElement))]
public class ProgressBar : System.Windows.Controls.ProgressBar
{
    private const string SecondaryIndicatorName = "PART_SecondaryIndicator";
    private const string SecondaryMarkerName = "PART_SecondaryMarker";

    private FrameworkElement? _secondaryIndicator;
    private FrameworkElement? _secondaryMarker;

    /// <summary>Identifies the <see cref="SecondaryValue"/> dependency property.</summary>
    public static readonly DependencyProperty SecondaryValueProperty = DependencyProperty.Register(
        nameof(SecondaryValue),
        typeof(double),
        typeof(ProgressBar),
        new FrameworkPropertyMetadata(
            0.0,
            OnSecondaryValueChanged,
            CoerceSecondaryValue));

    /// <summary>Identifies the <see cref="SecondaryForeground"/> dependency property.</summary>
    public static readonly DependencyProperty SecondaryForegroundProperty = DependencyProperty.Register(
        nameof(SecondaryForeground),
        typeof(Brush),
        typeof(ProgressBar),
        new FrameworkPropertyMetadata(null));

    static ProgressBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ProgressBar),
            new FrameworkPropertyMetadata(typeof(ProgressBar)));
    }

    /// <summary>
    /// Gets or sets the secondary progress value, clamped to [Minimum, Maximum].
    /// </summary>
    public double SecondaryValue
    {
        get => (double)GetValue(SecondaryValueProperty);
        set => SetValue(SecondaryValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used for the secondary fill and marker.
    /// </summary>
    public Brush? SecondaryForeground
    {
        get => (Brush?)GetValue(SecondaryForegroundProperty);
        set => SetValue(SecondaryForegroundProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _secondaryIndicator = GetTemplateChild(SecondaryIndicatorName) as FrameworkElement;
        _secondaryMarker = GetTemplateChild(SecondaryMarkerName) as FrameworkElement;

        UpdateSecondaryIndicator();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateSecondaryIndicator();
    }

    protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
    {
        base.OnMinimumChanged(oldMinimum, newMinimum);
        CoerceValue(SecondaryValueProperty);
        UpdateSecondaryIndicator();
    }

    protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
    {
        base.OnMaximumChanged(oldMaximum, newMaximum);
        CoerceValue(SecondaryValueProperty);
        UpdateSecondaryIndicator();
    }

    /// <summary>
    /// Updates the position and visibility of the secondary indicator and marker.
    /// </summary>
    protected virtual void UpdateSecondaryIndicator()
    {
        if (_secondaryIndicator is null && _secondaryMarker is null)
        {
            return;
        }

        double secondaryValue = SecondaryValue;
        double minimum = Minimum;
        double maximum = Maximum;

        if (secondaryValue <= minimum || IsIndeterminate || maximum <= minimum)
        {
            SetSecondaryVisibility(Visibility.Collapsed);
            return;
        }

        double trackWidth = Math.Max(0, ActualWidth - 2.0);
        double fraction = Math.Clamp((secondaryValue - minimum) / (maximum - minimum), 0.0, 1.0);
        double fillWidth = trackWidth * fraction;

        if (_secondaryIndicator is not null)
        {
            _secondaryIndicator.Width = fillWidth;
            _secondaryIndicator.Visibility = Visibility.Visible;
        }

        if (_secondaryMarker is not null)
        {
            double markerLeft = Math.Clamp(fillWidth, 1.0, Math.Max(1.0, trackWidth - 2.0));
            _secondaryMarker.Margin = new Thickness(markerLeft, 1.0, 0, 1.0);
            _secondaryMarker.Visibility = Visibility.Visible;
        }
    }

    private void SetSecondaryVisibility(Visibility visibility)
    {
        if (_secondaryIndicator is not null)
        {
            _secondaryIndicator.Visibility = visibility;
        }

        if (_secondaryMarker is not null)
        {
            _secondaryMarker.Visibility = visibility;
        }
    }

    private static void OnSecondaryValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProgressBar progressBar)
        {
            progressBar.UpdateSecondaryIndicator();
        }
    }

    private static object CoerceSecondaryValue(DependencyObject d, object baseValue)
    {
        if (d is ProgressBar progressBar && baseValue is double value)
        {
            return Math.Clamp(value, progressBar.Minimum, progressBar.Maximum);
        }

        return baseValue;
    }
}

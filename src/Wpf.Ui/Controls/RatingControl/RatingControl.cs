// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Displays the rating scale with interactions.
/// </summary>
[TemplatePart(Name = "PART_StarsContainer", Type = typeof(Grid))]
public class RatingControl : System.Windows.Controls.ContentControl
{
    private enum StarValue
    {
        Empty,
        HalfFilled,
        Filled,
    }

    private const double MinValue = 0.0D;
    private const int OffsetTolerance = 8;
    private static readonly SymbolRegular StarSymbol = SymbolRegular.Star28;
    private static readonly SymbolRegular StarHalfSymbol = SymbolRegular.StarHalf28;
    private readonly List<SymbolIcon> _stars = new();
    private Grid? _starsContainer;

    /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(RatingControl),
        new PropertyMetadata(0.0D, OnValueChanged)
    );

    /// <summary>Identifies the <see cref="MaxRating"/> dependency property.</summary>
    public static readonly DependencyProperty MaxRatingProperty = DependencyProperty.Register(
        nameof(MaxRating),
        typeof(int),
        typeof(RatingControl),
        new PropertyMetadata(5, OnMaxRatingChanged, CoerceMaxRating)
    );

    /// <summary>Identifies the <see cref="HalfStarEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty HalfStarEnabledProperty = DependencyProperty.Register(
        nameof(HalfStarEnabled),
        typeof(bool),
        typeof(RatingControl),
        new PropertyMetadata(true)
    );

    /// <summary>Identifies the <see cref="ValueChanged"/> routed event.</summary>
    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(RatingControl)
    );

    /// <summary>
    /// Gets or sets the rating value.
    /// </summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum allowed rating value.
    /// </summary>
    public int MaxRating
    {
        get => (int)GetValue(MaxRatingProperty);
        set => SetValue(MaxRatingProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether half of the star can be selected.
    /// </summary>
    public bool HalfStarEnabled
    {
        get => (bool)GetValue(HalfStarEnabledProperty);
        set => SetValue(HalfStarEnabledProperty, value);
    }

    /// <summary>
    /// Occurs after the user selects the rating.
    /// </summary>
    public event RoutedEventHandler ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    /// <summary>
    /// Is called when <see cref="Value"/> changes.
    /// </summary>
    protected virtual void OnValueChanged(double oldValue)
    {
        if (Value > MaxRating)
        {
            SetCurrentValue(ValueProperty, (double)MaxRating);

            return;
        }

        if (Value < MinValue)
        {
            SetCurrentValue(ValueProperty, MinValue);

            return;
        }

        if (!Value.Equals(oldValue))
        {
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent));

            if (UIElementAutomationPeer.FromElement(this) is RatingControlAutomationPeer peer)
            {
                peer.NotifyValueChanged(oldValue, Value);
            }
        }

        UpdateStarsFromValue();
    }

    /// <summary>
    /// Is called when mouse is moved away from the control.
    /// </summary>
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        UpdateStarsFromValue();
    }

    /// <summary>
    /// Is called when mouse is moved around the control.
    /// </summary>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        Point currentPossition = e.GetPosition(this);
        var mouseOffset = currentPossition.X * 100 / ActualWidth;

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            UpdateStarsOnMousePreview(mouseOffset);
        }
    }

    /// <summary>
    /// Is called when mouse is cliked down.
    /// </summary>
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        Point currentPossition = e.GetPosition(this);
        var mouseOffset = currentPossition.X * 100 / ActualWidth;

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            UpdateStarsOnMouseClick(mouseOffset);
        }
    }

    /// <summary>
    /// Adjusts the control's <see cref="Value" /> in response to keyboard input, incrementing or decrementing based on the key pressed.
    /// </summary>
    /// <param name="e">Key event arguments containing details about the key press.</param>
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if ((e.Key == Key.Right || e.Key == Key.Up) && Value < MaxRating)
        {
            Value += HalfStarEnabled ? 0.5D : 1;
        }

        if ((e.Key == Key.Left || e.Key == Key.Down) && Value > MinValue)
        {
            Value -= HalfStarEnabled ? 0.5D : 1;
        }
    }

    /// <summary>
    /// Is called when Template is changed.
    /// </summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _starsContainer = GetTemplateChild("PART_StarsContainer") as Grid;

        BuildStars();

        UpdateStarsFromValue();
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RatingControlAutomationPeer(this);

    private void BuildStars()
    {
        _stars.Clear();

        if (_starsContainer is null)
        {
            return;
        }

        _starsContainer.Children.Clear();
        _starsContainer.ColumnDefinitions.Clear();

        for (var i = 0; i < MaxRating; i++)
        {
            var star = new SymbolIcon { Symbol = StarSymbol };
            star.SetBinding(SymbolIcon.FontSizeProperty, new Binding(nameof(FontSize)) { Source = this });
            star.SetBinding(SymbolIcon.ForegroundProperty, new Binding(nameof(Foreground)) { Source = this });

            _starsContainer.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(star, i);
            _starsContainer.Children.Add(star);

            _stars.Add(star);
        }
    }

    private void UpdateStarsOnMousePreview(double offsetPercentage)
    {
        SetStarsPresence(ExtractValueFromOffset(offsetPercentage));
    }

    private void UpdateStarsOnMouseClick(double offsetPercentage)
    {
        var currentValue = ExtractValueFromOffset(offsetPercentage);

        SetCurrentValue(ValueProperty, currentValue / 2.0);
    }

    private void UpdateStarsFromValue()
    {
        SetStarsPresence((int)Math.Round(Value * 2, MidpointRounding.AwayFromZero));
    }

    private void SetStarsPresence(int index)
    {
        var maxIndex = MaxRating * 2;

        for (var starIndex = 0; starIndex < _stars.Count; starIndex++)
        {
            UpdateStar(starIndex, StarValueAt(starIndex, index, maxIndex));
        }
    }

    private static StarValue StarValueAt(int starIndex, int index, int maxIndex)
    {
        if (index < 1 || index > maxIndex)
        {
            return StarValue.Empty;
        }

        var threshold = (starIndex + 1) * 2;

        if (index >= threshold)
        {
            return StarValue.Filled;
        }

        return index == threshold - 1 ? StarValue.HalfFilled : StarValue.Empty;
    }

    private void UpdateStar(int starIndex, StarValue starValue)
    {
        if (starIndex < 0 || starIndex >= _stars.Count)
        {
            return;
        }

        SymbolIcon selectedIcon = _stars[starIndex];

        switch (starValue)
        {
            case StarValue.HalfFilled:
                selectedIcon.Filled = false;
                selectedIcon.Symbol = StarHalfSymbol;
                break;

            case StarValue.Filled:
                selectedIcon.Filled = true;
                selectedIcon.Symbol = StarSymbol;
                break;

            default:
                selectedIcon.Filled = false;
                selectedIcon.Symbol = StarSymbol;
                break;
        }
    }

    private int ExtractValueFromOffset(double offset)
    {
        var divisor = 50.0 / MaxRating;
        var starValue = (int)((offset + OffsetTolerance) / divisor);

        if (!HalfStarEnabled)
        {
            if (starValue < 2)
            {
                return 0;
            }

            if (starValue % 2 != 0)
            {
                starValue += 1;
            }
        }

        return starValue;
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RatingControl ratingControl)
        {
            return;
        }

        ratingControl.OnValueChanged((double)e.OldValue);
    }

    private static void OnMaxRatingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RatingControl ratingControl)
        {
            return;
        }

        ratingControl.BuildStars();

        if (ratingControl.Value > ratingControl.MaxRating)
        {
            ratingControl.SetCurrentValue(ValueProperty, (double)ratingControl.MaxRating);
        }

        ratingControl.UpdateStarsFromValue();
    }

    private static object CoerceMaxRating(DependencyObject d, object baseValue)
    {
        var value = (int)baseValue;

        return value < 1 ? 1 : value;
    }
}

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Custom <see cref="System.Windows.Controls.ScrollViewer"/> with events depending on actions taken by the user.
/// </summary>
[DefaultEvent("ScrollChangedEvent")]
public class DynamicScrollViewer : PassiveScrollViewer
{
    /// <summary>Timeout values at or above this many milliseconds use <see cref="CappedTimeoutMilliseconds"/> instead.</summary>
    private const int UncappedTimeoutCeilingMilliseconds = 10000;

    /// <summary>The hide delay used for a <see cref="Timeout"/> at or above <see cref="UncappedTimeoutCeilingMilliseconds"/>.</summary>
    private const int CappedTimeoutMilliseconds = 1000;

    private readonly EventIdentifier _verticalIdentifier = new();

    private readonly EventIdentifier _horizontalIdentifier = new();

    private readonly TimeProvider _timeProvider;

    // Due to the large number of triggered events, we limit the complex logic of DependencyProperty
    private bool _scrollingVertically = false;

    private bool _scrollingHorizontally = false;

    private int _timeout = 1200;

    private double _minimalChange = 40d;

    /// <summary>Initializes a new instance of the <see cref="DynamicScrollViewer"/> class.</summary>
    public DynamicScrollViewer()
        : this(TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DynamicScrollViewer"/> class using the given time source for its scroll indicator hide delay.</summary>
    /// <param name="timeProvider">The source of the hide-delay timing.</param>
    public DynamicScrollViewer(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        Unloaded += static (sender, _) => ((DynamicScrollViewer)sender).OnUnloaded();
    }

    /// <summary>Identifies the <see cref="IsScrollingVertically"/> dependency property.</summary>
    public static readonly DependencyProperty IsScrollingVerticallyProperty = DependencyProperty.Register(
        nameof(IsScrollingVertically),
        typeof(bool),
        typeof(DynamicScrollViewer),
        new PropertyMetadata(false, OnIsScrollingVerticallyChanged)
    );

    /// <summary>Identifies the <see cref="IsScrollingHorizontally"/> dependency property.</summary>
    public static readonly DependencyProperty IsScrollingHorizontallyProperty = DependencyProperty.Register(
        nameof(IsScrollingHorizontally),
        typeof(bool),
        typeof(DynamicScrollViewer),
        new PropertyMetadata(false, OnIsScrollingHorizontallyChanged)
    );

    /// <summary>Identifies the <see cref="MinimalChange"/> dependency property.</summary>
    public static readonly DependencyProperty MinimalChangeProperty = DependencyProperty.Register(
        nameof(MinimalChange),
        typeof(double),
        typeof(DynamicScrollViewer),
        new PropertyMetadata(40d, OnMinimalChangeChanged)
    );

    /// <summary>Identifies the <see cref="Timeout"/> dependency property.</summary>
    public static readonly DependencyProperty TimeoutProperty = DependencyProperty.Register(
        nameof(Timeout),
        typeof(int),
        typeof(DynamicScrollViewer),
        new PropertyMetadata(1200, OnTimeoutChanged, CoerceTimeout)
    );

    /// <summary>
    /// Gets or sets a value indicating whether the user was scrolling vertically for the last few seconds.
    /// </summary>
    public bool IsScrollingVertically
    {
        get => (bool)GetValue(IsScrollingVerticallyProperty);
        set => SetValue(IsScrollingVerticallyProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the user was scrolling horizontally for the last few seconds.
    /// </summary>
    public bool IsScrollingHorizontally
    {
        get => (bool)GetValue(IsScrollingHorizontallyProperty);
        set => SetValue(IsScrollingHorizontallyProperty, value);
    }

    /// <summary>
    /// Gets or sets the value required for the scroll to show automatically.
    /// </summary>
    public double MinimalChange
    {
        get => (double)GetValue(MinimalChangeProperty);
        set => SetValue(MinimalChangeProperty, value);
    }

    /// <summary>
    /// Gets or sets time in milliseconds after which the scroll is to be hidden.
    /// </summary>
    /// <remarks>
    /// Negative values are coerced to 0. Values of 10000 or greater use a fixed 1000 millisecond hide delay.
    /// </remarks>
    public int Timeout
    {
        get => (int)GetValue(TimeoutProperty);
        set => SetValue(TimeoutProperty, value);
    }

    /// <summary>
    /// OnScrollChanged is an override called whenever scrolling state changes on this <see cref="DynamicScrollViewer"/>.
    /// </summary>
    /// <remarks>
    /// OnScrollChanged fires the ScrollChangedEvent. Overriders of this method should call
    /// base.OnScrollChanged(args) if they want the event to be fired.
    /// </remarks>
    /// <param name="e">ScrollChangedEventArgs containing information about the change in scrolling state.</param>
    protected override void OnScrollChanged(ScrollChangedEventArgs e)
    {
        base.OnScrollChanged(e);

        if (e.HorizontalChange > _minimalChange || e.HorizontalChange < -_minimalChange)
        {
            UpdateHorizontalScrollingState();
        }

        if (e.VerticalChange > _minimalChange || e.VerticalChange < -_minimalChange)
        {
            UpdateVerticalScrollingState();
        }
    }

    private async void UpdateVerticalScrollingState()
    {
        // TODO: Optimize
        // My main assumption here is that each scroll causes a new "event / thread" to be assigned.
        // If more than Timeout has passed since the last event, there is no interaction.
        // We pass this value to the ScrollBar and link it to IsMouseOver.
        // This way we have a dynamic scrollbar that responds to scroll / mouse over.
        await UpdateScrollingStateAsync(_verticalIdentifier, IsScrollingVerticallyProperty, () => _scrollingVertically);
    }

    private async void UpdateHorizontalScrollingState()
    {
        // TODO: Optimize
        // My main assumption here is that each scroll causes a new "event / thread" to be assigned.
        // If more than Timeout has passed since the last event, there is no interaction.
        // We pass this value to the ScrollBar and link it to IsMouseOver.
        // This way we have a dynamic scrollbar that responds to scroll / mouse over.
        await UpdateScrollingStateAsync(_horizontalIdentifier, IsScrollingHorizontallyProperty, () => _scrollingHorizontally);
    }

    private async Task UpdateScrollingStateAsync(
        EventIdentifier identifier,
        DependencyProperty isScrollingProperty,
        Func<bool> isScrolling
    )
    {
        long currentEvent = identifier.GetNext();

        if (!isScrolling())
        {
            SetCurrentValue(isScrollingProperty, true);
        }

        int delay = _timeout < UncappedTimeoutCeilingMilliseconds ? _timeout : CappedTimeoutMilliseconds;

        if (delay > 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(delay), _timeProvider);
        }

        if (identifier.IsEqual(currentEvent) && isScrolling())
        {
            SetCurrentValue(isScrollingProperty, false);
        }
    }

    private void OnUnloaded()
    {
        _verticalIdentifier.GetNext();
        _horizontalIdentifier.GetNext();

        if (_scrollingVertically)
        {
            SetCurrentValue(IsScrollingVerticallyProperty, false);
        }

        if (_scrollingHorizontally)
        {
            SetCurrentValue(IsScrollingHorizontallyProperty, false);
        }
    }

    private static void OnIsScrollingVerticallyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not DynamicScrollViewer scroll)
        {
            return;
        }

        scroll._scrollingVertically = scroll.IsScrollingVertically;
    }

    private static void OnIsScrollingHorizontallyChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is not DynamicScrollViewer scroll)
        {
            return;
        }

        scroll._scrollingHorizontally = scroll.IsScrollingHorizontally;
    }

    private static void OnMinimalChangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DynamicScrollViewer scroll)
        {
            return;
        }

        scroll._minimalChange = scroll.MinimalChange;
    }

    private static void OnTimeoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DynamicScrollViewer scroll)
        {
            return;
        }

        scroll._timeout = scroll.Timeout;
    }

    private static object CoerceTimeout(DependencyObject d, object baseValue)
    {
        int value = (int)baseValue;

        return value < 0 ? 0 : value;
    }
}

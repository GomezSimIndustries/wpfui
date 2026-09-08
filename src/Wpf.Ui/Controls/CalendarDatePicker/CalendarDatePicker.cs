// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Represents a control that allows a user to pick a date from a calendar display.
/// </summary>
/// <example>
/// <code lang="xml">
/// &lt;ui:CalendarDatePicker /&gt;
/// </code>
/// </example>
public class CalendarDatePicker : Button
{
    /// <summary>Marks the <see cref="FirstDayOfWeek"/> base value as not yet resolved from a culture.</summary>
    private const DayOfWeek UnresolvedFirstDayOfWeek = (DayOfWeek)(-1);

    private Popup? _popup;

    /// <summary>Identifies the <see cref="IsCalendarOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsCalendarOpenProperty = DependencyProperty.Register(
        nameof(IsCalendarOpen),
        typeof(bool),
        typeof(CalendarDatePicker),
        new PropertyMetadata(false, OnIsCalendarOpenChanged)
    );

    /// <summary>Identifies the <see cref="IsTodayHighlighted"/> dependency property.</summary>
    public static readonly DependencyProperty IsTodayHighlightedProperty = DependencyProperty.Register(
        nameof(IsTodayHighlighted),
        typeof(bool),
        typeof(CalendarDatePicker),
        new PropertyMetadata(false)
    );

    /// <summary>Identifies the <see cref="Date"/> dependency property.</summary>
    public static readonly DependencyProperty DateProperty = DependencyProperty.Register(
        nameof(Date),
        typeof(DateTime?),
        typeof(CalendarDatePicker),
        new PropertyMetadata(null)
    );

    /// <summary>Identifies the <see cref="FirstDayOfWeek"/> dependency property.</summary>
    public static readonly DependencyProperty FirstDayOfWeekProperty = DependencyProperty.Register(
        nameof(FirstDayOfWeek),
        typeof(DayOfWeek),
        typeof(CalendarDatePicker),
        new PropertyMetadata(UnresolvedFirstDayOfWeek, null, CoerceFirstDayOfWeek)
    );

    /// <summary>Identifies the <see cref="PreviousButtonAutomationName"/> dependency property.</summary>
    public static readonly DependencyProperty PreviousButtonAutomationNameProperty = DependencyProperty.Register(
        nameof(PreviousButtonAutomationName),
        typeof(string),
        typeof(CalendarDatePicker),
        new PropertyMetadata("Previous")
    );

    /// <summary>Identifies the <see cref="NextButtonAutomationName"/> dependency property.</summary>
    public static readonly DependencyProperty NextButtonAutomationNameProperty = DependencyProperty.Register(
        nameof(NextButtonAutomationName),
        typeof(string),
        typeof(CalendarDatePicker),
        new PropertyMetadata("Next")
    );

    /// <summary>
    /// Gets or sets a value indicating whether the current date is highlighted.
    /// </summary>
    public bool IsTodayHighlighted
    {
        get { return (bool)GetValue(IsTodayHighlightedProperty); }
        set { SetValue(IsTodayHighlightedProperty, value); }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the calendar view of the <see cref="CalendarDatePicker"/> is currently shown.
    /// </summary>
    [Bindable(true)]
    public bool IsCalendarOpen
    {
        get => (bool)GetValue(IsCalendarOpenProperty);
        set => SetValue(IsCalendarOpenProperty, value);
    }

    /// <summary>
    /// Gets or sets the day that is considered the beginning of the week.
    /// </summary>
    /// <remarks>
    /// When never explicitly set, this resolves from <see cref="CultureInfo.CurrentCulture"/> at construction
    /// time; a culture change after construction does not affect an already-created instance.
    /// </remarks>
    public DayOfWeek FirstDayOfWeek
    {
        get { return (DayOfWeek)GetValue(FirstDayOfWeekProperty); }
        set { SetValue(FirstDayOfWeekProperty, value); }
    }

    /// <summary>
    /// Gets or sets the accessible name announced for the popup calendar's previous-page navigation button.
    /// </summary>
    public string PreviousButtonAutomationName
    {
        get => (string)GetValue(PreviousButtonAutomationNameProperty);
        set => SetValue(PreviousButtonAutomationNameProperty, value);
    }

    /// <summary>
    /// Gets or sets the accessible name announced for the popup calendar's next-page navigation button.
    /// </summary>
    public string NextButtonAutomationName
    {
        get => (string)GetValue(NextButtonAutomationNameProperty);
        set => SetValue(NextButtonAutomationNameProperty, value);
    }

    /// <summary>
    /// Gets or sets the date currently set in the calendar picker.
    /// </summary>
    [Bindable(true)]
    public DateTime? Date
    {
        get => (DateTime?)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarDatePicker"/> class.
    /// </summary>
    public CalendarDatePicker()
    {
        Unloaded += OnCalendarDatePickerUnloaded;
        CoerceValue(FirstDayOfWeekProperty);
    }

    /// <inheritdoc />
    protected override void OnClick()
    {
        base.OnClick();

        SetCurrentValue(IsCalendarOpenProperty, !IsCalendarOpen);
    }

    private static void OnIsCalendarOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (CalendarDatePicker)d;

        if ((bool)e.NewValue)
        {
            picker.InitializePopup();
        }
    }

    private static object CoerceFirstDayOfWeek(DependencyObject d, object baseValue)
    {
        return (DayOfWeek)baseValue == UnresolvedFirstDayOfWeek
            ? DateTimeHelper.GetCurrentDateFormat().FirstDayOfWeek
            : baseValue;
    }

    private void InitializePopup()
    {
        if (_popup is not null)
        {
            return;
        }

        var calendar = new System.Windows.Controls.Calendar();
        _ = calendar.SetBinding(
            System.Windows.Controls.Calendar.SelectedDateProperty,
            new Binding(nameof(Date))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            }
        );
        _ = calendar.SetBinding(
            System.Windows.Controls.Calendar.IsTodayHighlightedProperty,
            new Binding(nameof(IsTodayHighlighted))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            }
        );
        _ = calendar.SetBinding(
            System.Windows.Controls.Calendar.FirstDayOfWeekProperty,
            new Binding(nameof(FirstDayOfWeek))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            }
        );
        _ = calendar.SetBinding(
            CalendarNavigation.PreviousButtonNameProperty,
            new Binding(nameof(PreviousButtonAutomationName))
            {
                Source = this,
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            }
        );
        _ = calendar.SetBinding(
            CalendarNavigation.NextButtonNameProperty,
            new Binding(nameof(NextButtonAutomationName))
            {
                Source = this,
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            }
        );

        calendar.SelectedDatesChanged += OnSelectedDatesChanged;
        calendar.PreviewKeyDown += OnCalendarPreviewKeyDown;

        _popup = new Popup
        {
            PlacementTarget = this,
            Placement = PlacementMode.Bottom,
            Child = calendar,
            Focusable = false,
            StaysOpen = false,
            VerticalOffset = 1D,
            VerticalAlignment = VerticalAlignment.Center,
            PopupAnimation = PopupAnimation.None,
            AllowsTransparency = true,
        };

        _popup.Opened += OnPopupOpened;

        _ = _popup.SetBinding(
            Popup.IsOpenProperty,
            new Binding(nameof(IsCalendarOpen))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            }
        );
    }

    private void OnCalendarDatePickerUnloaded(object sender, RoutedEventArgs e)
    {
        if (IsCalendarOpen)
        {
            SetCurrentValue(IsCalendarOpenProperty, false);
        }
    }

    private void OnCalendarPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SetCurrentValue(IsCalendarOpenProperty, false);
        }
    }

    protected virtual void OnPopupOpened(object? sender, EventArgs e)
    {
        if (sender is not Popup popup)
        {
            return;
        }

        if (popup.Child is null)
        {
            return;
        }

        _ = popup.Focus();
        _ = Keyboard.Focus(popup.Child);
    }

    protected virtual void OnSelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsCalendarOpen)
        {
            SetCurrentValue(IsCalendarOpenProperty, false);
        }
    }
}

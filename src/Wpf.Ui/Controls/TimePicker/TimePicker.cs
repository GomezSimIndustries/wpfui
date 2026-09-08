// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls.Primitives;
using System.Windows.Input;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Represents a control that allows a user to pick a time value.
/// </summary>
/// <remarks>
/// The clock system default and every string the columns display are resolved from
/// <see cref="CultureInfo.CurrentCulture"/> as it stands when the instance is constructed.
/// </remarks>
[TemplatePart(Name = TemplateElementPopup, Type = typeof(Popup))]
[TemplatePart(Name = TemplateElementHourSelector, Type = typeof(Selector))]
[TemplatePart(Name = TemplateElementMinuteSelector, Type = typeof(Selector))]
[TemplatePart(Name = TemplateElementDesignatorSelector, Type = typeof(Selector))]
[TemplatePart(Name = TemplateElementAcceptButton, Type = typeof(ButtonBase))]
[TemplatePart(Name = TemplateElementDismissButton, Type = typeof(ButtonBase))]
public class TimePicker : ButtonBase
{
    private const string TemplateElementPopup = "PART_Popup";

    private const string TemplateElementHourSelector = "PART_HourSelector";

    private const string TemplateElementMinuteSelector = "PART_MinuteSelector";

    private const string TemplateElementDesignatorSelector = "PART_DesignatorSelector";

    private const string TemplateElementAcceptButton = "PART_AcceptButton";

    private const string TemplateElementDismissButton = "PART_DismissButton";

    private const int AfternoonDesignatorIndex = 1;

    private const int LowestMinuteIncrement = 1;

    private const int HighestMinuteIncrement = 59;

    private const int HoursPerDay = 24;

    private const int HoursPerHalfDay = 12;

    private const int MinutesPerHour = 60;

    private static readonly TimeSpan LastMinuteOfDay = new(23, 59, 0);

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header),
        typeof(object),
        typeof(TimePicker),
        new PropertyMetadata(null)
    );

    /// <summary>Identifies the <see cref="Time"/> dependency property.</summary>
    public static readonly DependencyProperty TimeProperty = DependencyProperty.Register(
        nameof(Time),
        typeof(TimeSpan),
        typeof(TimePicker),
        new PropertyMetadata(TimeSpan.Zero, OnTimeChanged, CoerceTime)
    );

    /// <summary>Identifies the <see cref="SelectedTime"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedTimeProperty = DependencyProperty.Register(
        nameof(SelectedTime),
        typeof(TimeSpan?),
        typeof(TimePicker),
        new PropertyMetadata(null, OnSelectedTimeChanged, CoerceSelectedTime)
    );

    /// <summary>Identifies the <see cref="MinuteIncrement"/> dependency property.</summary>
    public static readonly DependencyProperty MinuteIncrementProperty = DependencyProperty.Register(
        nameof(MinuteIncrement),
        typeof(int),
        typeof(TimePicker),
        new PropertyMetadata(LowestMinuteIncrement, OnMinuteIncrementChanged, CoerceMinuteIncrement)
    );

    /// <summary>Identifies the <see cref="ClockIdentifier"/> dependency property.</summary>
    public static readonly DependencyProperty ClockIdentifierProperty = DependencyProperty.Register(
        nameof(ClockIdentifier),
        typeof(ClockIdentifier),
        typeof(TimePicker),
        new PropertyMetadata(ClockIdentifier.Clock24Hour, OnClockIdentifierChanged)
    );

    /// <summary>Identifies the <see cref="IsTimePickerOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsTimePickerOpenProperty =
        DependencyProperty.Register(
            nameof(IsTimePickerOpen),
            typeof(bool),
            typeof(TimePicker),
            new PropertyMetadata(false, OnIsTimePickerOpenChanged)
        );

    /// <summary>Identifies the <see cref="AcceptButtonText"/> dependency property.</summary>
    public static readonly DependencyProperty AcceptButtonTextProperty =
        DependencyProperty.Register(
            nameof(AcceptButtonText),
            typeof(string),
            typeof(TimePicker),
            new PropertyMetadata("Accept")
        );

    /// <summary>Identifies the <see cref="DismissButtonText"/> dependency property.</summary>
    public static readonly DependencyProperty DismissButtonTextProperty =
        DependencyProperty.Register(
            nameof(DismissButtonText),
            typeof(string),
            typeof(TimePicker),
            new PropertyMetadata("Dismiss")
        );

    /// <summary>Identifies the <see cref="HourPlaceholderText"/> dependency property.</summary>
    public static readonly DependencyProperty HourPlaceholderTextProperty =
        DependencyProperty.Register(
            nameof(HourPlaceholderText),
            typeof(string),
            typeof(TimePicker),
            new PropertyMetadata("hour", OnPlaceholderTextChanged)
        );

    /// <summary>Identifies the <see cref="MinutePlaceholderText"/> dependency property.</summary>
    public static readonly DependencyProperty MinutePlaceholderTextProperty =
        DependencyProperty.Register(
            nameof(MinutePlaceholderText),
            typeof(string),
            typeof(TimePicker),
            new PropertyMetadata("minute", OnPlaceholderTextChanged)
        );

    private static readonly DependencyPropertyKey HourTextPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HourText),
            typeof(string),
            typeof(TimePicker),
            new PropertyMetadata(string.Empty)
        );

    /// <summary>Identifies the <see cref="HourText"/> dependency property.</summary>
    public static readonly DependencyProperty HourTextProperty =
        HourTextPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey MinuteTextPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(MinuteText),
            typeof(string),
            typeof(TimePicker),
            new PropertyMetadata(string.Empty)
        );

    /// <summary>Identifies the <see cref="MinuteText"/> dependency property.</summary>
    public static readonly DependencyProperty MinuteTextProperty =
        MinuteTextPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey DesignatorTextPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(DesignatorText),
            typeof(string),
            typeof(TimePicker),
            new PropertyMetadata(string.Empty)
        );

    /// <summary>Identifies the <see cref="DesignatorText"/> dependency property.</summary>
    public static readonly DependencyProperty DesignatorTextProperty =
        DesignatorTextPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey HourItemsPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(HourItems),
            typeof(IReadOnlyList<string>),
            typeof(TimePicker),
            new PropertyMetadata(Array.Empty<string>())
        );

    /// <summary>Identifies the <see cref="HourItems"/> dependency property.</summary>
    public static readonly DependencyProperty HourItemsProperty =
        HourItemsPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey MinuteItemsPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(MinuteItems),
            typeof(IReadOnlyList<string>),
            typeof(TimePicker),
            new PropertyMetadata(Array.Empty<string>())
        );

    /// <summary>Identifies the <see cref="MinuteItems"/> dependency property.</summary>
    public static readonly DependencyProperty MinuteItemsProperty =
        MinuteItemsPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey DesignatorItemsPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(DesignatorItems),
            typeof(IReadOnlyList<string>),
            typeof(TimePicker),
            new PropertyMetadata(Array.Empty<string>())
        );

    /// <summary>Identifies the <see cref="DesignatorItems"/> dependency property.</summary>
    public static readonly DependencyProperty DesignatorItemsProperty =
        DesignatorItemsPropertyKey.DependencyProperty;

    private readonly CultureInfo _culture;

    private Popup? _popup;

    private Selector? _hourSelector;

    private Selector? _minuteSelector;

    private Selector? _designatorSelector;

    private ButtonBase? _acceptButton;

    private ButtonBase? _dismissButton;

    private bool _synchronizing;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimePicker"/> class.
    /// </summary>
    public TimePicker()
    {
        _culture = CultureInfo.CurrentCulture;

        SetCurrentValue(ClockIdentifierProperty, ResolveClockIdentifier(_culture));

        RebuildHourItems();
        RebuildMinuteItems();
        RebuildDesignatorItems();
        UpdateDisplayText();

        Unloaded += OnTimePickerUnloaded;
    }

    /// <summary>
    /// Gets or sets the content for the control's header.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets the time currently set in the time picker.
    /// </summary>
    /// <remarks>
    /// Assignments are truncated to whole minutes and clamped to the range 00:00 through 23:59, and
    /// they move <see cref="SelectedTime"/> to the same value. Clearing <see cref="SelectedTime"/>
    /// leaves this property at <see cref="TimeSpan.Zero"/>.
    /// </remarks>
    public TimeSpan Time
    {
        get => (TimeSpan)GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    /// <summary>
    /// Gets or sets the time currently selected in the time picker, or <see langword="null"/> when nothing is selected.
    /// </summary>
    /// <remarks>Assignments are truncated to whole minutes and clamped to the range 00:00 through 23:59.</remarks>
    public TimeSpan? SelectedTime
    {
        get => (TimeSpan?)GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the time increments shown in the minute picker.
    /// For example, 15 specifies that the TimePicker minute control displays only the choices 00, 15, 30, 45.
    /// </summary>
    /// <remarks>Assignments are clamped to the range 1 through 59.</remarks>
    public int MinuteIncrement
    {
        get => (int)GetValue(MinuteIncrementProperty);
        set => SetValue(MinuteIncrementProperty, value);
    }

    /// <summary>
    /// Gets or sets the clock system to use.
    /// </summary>
    public ClockIdentifier ClockIdentifier
    {
        get => (ClockIdentifier)GetValue(ClockIdentifierProperty);
        set => SetValue(ClockIdentifierProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the selection popup is currently shown.
    /// </summary>
    [Bindable(true)]
    public bool IsTimePickerOpen
    {
        get => (bool)GetValue(IsTimePickerOpenProperty);
        set => SetValue(IsTimePickerOpenProperty, value);
    }

    /// <summary>
    /// Gets or sets the accessible name of the button that commits the popup selection.
    /// </summary>
    public string AcceptButtonText
    {
        get => (string)GetValue(AcceptButtonTextProperty);
        set => SetValue(AcceptButtonTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the accessible name of the button that closes the popup without committing.
    /// </summary>
    public string DismissButtonText
    {
        get => (string)GetValue(DismissButtonTextProperty);
        set => SetValue(DismissButtonTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the text the hour column shows while nothing is selected.
    /// </summary>
    public string HourPlaceholderText
    {
        get => (string)GetValue(HourPlaceholderTextProperty);
        set => SetValue(HourPlaceholderTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the text the minute column shows while nothing is selected.
    /// </summary>
    public string MinutePlaceholderText
    {
        get => (string)GetValue(MinutePlaceholderTextProperty);
        set => SetValue(MinutePlaceholderTextProperty, value);
    }

    /// <summary>
    /// Gets the text the hour column displays.
    /// </summary>
    public string HourText => (string)GetValue(HourTextProperty);

    /// <summary>
    /// Gets the text the minute column displays.
    /// </summary>
    public string MinuteText => (string)GetValue(MinuteTextProperty);

    /// <summary>
    /// Gets the text the AM/PM column displays.
    /// </summary>
    public string DesignatorText => (string)GetValue(DesignatorTextProperty);

    /// <summary>
    /// Gets the hours offered by the hour selector.
    /// </summary>
    public IReadOnlyList<string> HourItems => (IReadOnlyList<string>)GetValue(HourItemsProperty);

    /// <summary>
    /// Gets the minutes offered by the minute selector.
    /// </summary>
    public IReadOnlyList<string> MinuteItems =>
        (IReadOnlyList<string>)GetValue(MinuteItemsProperty);

    /// <summary>
    /// Gets the AM and PM designators offered by the designator selector.
    /// </summary>
    public IReadOnlyList<string> DesignatorItems =>
        (IReadOnlyList<string>)GetValue(DesignatorItemsProperty);

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        DetachTemplateParts();

        _popup = GetTemplateChild(TemplateElementPopup) as Popup;
        _hourSelector = GetTemplateChild(TemplateElementHourSelector) as Selector;
        _minuteSelector = GetTemplateChild(TemplateElementMinuteSelector) as Selector;
        _designatorSelector = GetTemplateChild(TemplateElementDesignatorSelector) as Selector;
        _acceptButton = GetTemplateChild(TemplateElementAcceptButton) as ButtonBase;
        _dismissButton = GetTemplateChild(TemplateElementDismissButton) as ButtonBase;

        if (_popup is not null)
        {
            _popup.Opened += OnPopupOpened;
            _popup.PreviewKeyDown += OnPopupPreviewKeyDown;
        }

        if (_acceptButton is not null)
        {
            _acceptButton.Click += OnAcceptButtonClick;
        }

        if (_dismissButton is not null)
        {
            _dismissButton.Click += OnDismissButtonClick;
        }
    }

    /// <inheritdoc />
    protected override void OnClick()
    {
        base.OnClick();

        SetCurrentValue(IsTimePickerOpenProperty, !IsTimePickerOpen);
    }

    /// <summary>Moves keyboard focus into the hour column once the popup is shown.</summary>
    protected virtual void OnPopupOpened(object? sender, EventArgs e)
    {
        if (_hourSelector is null)
        {
            return;
        }

        _ = _hourSelector.Focus();
        _ = Keyboard.Focus(_hourSelector);
    }

    /// <summary>Returns the clock system that the culture's short time pattern writes.</summary>
    internal static ClockIdentifier ResolveClockIdentifier(CultureInfo culture)
    {
        return WritesAHalfDayHour(culture.DateTimeFormat.ShortTimePattern)
            ? ClockIdentifier.Clock12Hour
            : ClockIdentifier.Clock24Hour;
    }

    /// <summary>Truncates a time of day to whole minutes and clamps it to the range 00:00 through 23:59.</summary>
    internal static TimeSpan ClampToMinuteOfDay(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        if (value >= LastMinuteOfDay)
        {
            return LastMinuteOfDay;
        }

        return TimeSpan.FromTicks(value.Ticks - (value.Ticks % TimeSpan.TicksPerMinute));
    }

    private static bool WritesAHalfDayHour(string pattern)
    {
        var quote = '\0';

        for (var index = 0; index < pattern.Length; index++)
        {
            char character = pattern[index];

            if (character == '\\')
            {
                index++;
                continue;
            }

            if (quote != '\0')
            {
                if (character == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            if (character is '\'' or '"')
            {
                quote = character;
                continue;
            }

            if (character == 'h')
            {
                return true;
            }
        }

        return false;
    }

    private static int ToHalfDayHour(int hour)
    {
        var clockHour = hour % HoursPerHalfDay;

        return clockHour == 0 ? HoursPerHalfDay : clockHour;
    }

    private static object CoerceTime(DependencyObject d, object? baseValue)
    {
        return baseValue is TimeSpan value ? ClampToMinuteOfDay(value) : TimeSpan.Zero;
    }

    private static object? CoerceSelectedTime(DependencyObject d, object? baseValue)
    {
        return baseValue is TimeSpan value ? ClampToMinuteOfDay(value) : null;
    }

    private static object CoerceMinuteIncrement(DependencyObject d, object? baseValue)
    {
        var increment = baseValue is int value ? value : LowestMinuteIncrement;

        return Math.Clamp(increment, LowestMinuteIncrement, HighestMinuteIncrement);
    }

    private static void OnTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((TimePicker)d).SynchronizeSelectedTime((TimeSpan)e.NewValue);
    }

    private static void OnSelectedTimeChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        var picker = (TimePicker)d;

        picker.SynchronizeTime(e.NewValue as TimeSpan?);
        picker.UpdateDisplayText();
    }

    private static void OnMinuteIncrementChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        ((TimePicker)d).RebuildMinuteItems();
    }

    private static void OnClockIdentifierChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        var picker = (TimePicker)d;

        picker.RebuildHourItems();
        picker.UpdateDisplayText();
    }

    private static void OnPlaceholderTextChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        ((TimePicker)d).UpdateDisplayText();
    }

    private static void OnIsTimePickerOpenChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if ((bool)e.NewValue)
        {
            ((TimePicker)d).SeedSelectors();
        }
    }

    private void DetachTemplateParts()
    {
        if (_popup is not null)
        {
            _popup.Opened -= OnPopupOpened;
            _popup.PreviewKeyDown -= OnPopupPreviewKeyDown;
        }

        if (_acceptButton is not null)
        {
            _acceptButton.Click -= OnAcceptButtonClick;
        }

        if (_dismissButton is not null)
        {
            _dismissButton.Click -= OnDismissButtonClick;
        }
    }

    private void OnTimePickerUnloaded(object sender, RoutedEventArgs e)
    {
        SetCurrentValue(IsTimePickerOpenProperty, false);
    }

    private void OnPopupPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                CommitSelection();
                e.Handled = true;
                break;

            case Key.Escape:
                SetCurrentValue(IsTimePickerOpenProperty, false);
                e.Handled = true;
                break;
        }
    }

    private void OnAcceptButtonClick(object sender, RoutedEventArgs e)
    {
        CommitSelection();
        e.Handled = true;
    }

    private void OnDismissButtonClick(object sender, RoutedEventArgs e)
    {
        SetCurrentValue(IsTimePickerOpenProperty, false);
        e.Handled = true;
    }

    private void SeedSelectors()
    {
        TimeSpan? selected = SelectedTime;
        var hour = selected?.Hours ?? 0;
        var minute = selected?.Minutes ?? 0;

        _hourSelector?.SetCurrentValue(
            Selector.SelectedIndexProperty,
            ClockIdentifier == ClockIdentifier.Clock12Hour ? hour % HoursPerHalfDay : hour
        );

        _minuteSelector?.SetCurrentValue(
            Selector.SelectedIndexProperty,
            Math.Min(minute / MinuteIncrement, Math.Max(_minuteSelector.Items.Count - 1, 0))
        );

        _designatorSelector?.SetCurrentValue(
            Selector.SelectedIndexProperty,
            hour < HoursPerHalfDay ? 0 : AfternoonDesignatorIndex
        );
    }

    private void CommitSelection()
    {
        SetCurrentValue(
            SelectedTimeProperty,
            (TimeSpan?)new TimeSpan(ReadSelectedHour(), ReadSelectedMinute(), 0)
        );
        SetCurrentValue(IsTimePickerOpenProperty, false);
    }

    private int ReadSelectedHour()
    {
        var index = Math.Max(_hourSelector?.SelectedIndex ?? 0, 0);

        if (ClockIdentifier != ClockIdentifier.Clock12Hour)
        {
            return Math.Min(index, HoursPerDay - 1);
        }

        var clockHour = index % HoursPerHalfDay;

        return _designatorSelector?.SelectedIndex == AfternoonDesignatorIndex
            ? clockHour + HoursPerHalfDay
            : clockHour;
    }

    private int ReadSelectedMinute()
    {
        var index = Math.Max(_minuteSelector?.SelectedIndex ?? 0, 0);

        return Math.Min(index * MinuteIncrement, MinutesPerHour - 1);
    }

    private void SynchronizeSelectedTime(TimeSpan value)
    {
        if (_synchronizing)
        {
            return;
        }

        _synchronizing = true;

        try
        {
            SetCurrentValue(SelectedTimeProperty, value);
        }
        finally
        {
            _synchronizing = false;
        }
    }

    private void SynchronizeTime(TimeSpan? value)
    {
        if (_synchronizing)
        {
            return;
        }

        _synchronizing = true;

        try
        {
            SetCurrentValue(TimeProperty, value ?? TimeSpan.Zero);
        }
        finally
        {
            _synchronizing = false;
        }
    }

    private void RebuildHourItems()
    {
        var twelveHour = ClockIdentifier == ClockIdentifier.Clock12Hour;
        var count = twelveHour ? HoursPerHalfDay : HoursPerDay;
        var texts = new string[count];

        for (var index = 0; index < count; index++)
        {
            texts[index] = twelveHour
                ? ToHalfDayHour(index).ToString(_culture)
                : index.ToString("00", _culture);
        }

        SetValue(HourItemsPropertyKey, texts);
    }

    private void RebuildMinuteItems()
    {
        var increment = MinuteIncrement;
        var count = ((MinutesPerHour - 1) / increment) + 1;
        var texts = new string[count];

        for (var index = 0; index < count; index++)
        {
            texts[index] = (index * increment).ToString("00", _culture);
        }

        SetValue(MinuteItemsPropertyKey, texts);
    }

    private void RebuildDesignatorItems()
    {
        DateTimeFormatInfo format = _culture.DateTimeFormat;

        SetValue(DesignatorItemsPropertyKey, new[] { format.AMDesignator, format.PMDesignator });
    }

    private void UpdateDisplayText()
    {
        DateTimeFormatInfo format = _culture.DateTimeFormat;
        TimeSpan? selected = SelectedTime;

        if (selected is null)
        {
            SetValue(HourTextPropertyKey, HourPlaceholderText);
            SetValue(MinuteTextPropertyKey, MinutePlaceholderText);
            SetValue(DesignatorTextPropertyKey, format.AMDesignator);

            return;
        }

        var hour = selected.Value.Hours;

        SetValue(
            HourTextPropertyKey,
            ClockIdentifier == ClockIdentifier.Clock12Hour
                ? ToHalfDayHour(hour).ToString(_culture)
                : hour.ToString("00", _culture)
        );
        SetValue(MinuteTextPropertyKey, selected.Value.Minutes.ToString("00", _culture));
        SetValue(
            DesignatorTextPropertyKey,
            hour < HoursPerHalfDay ? format.AMDesignator : format.PMDesignator
        );
    }
}

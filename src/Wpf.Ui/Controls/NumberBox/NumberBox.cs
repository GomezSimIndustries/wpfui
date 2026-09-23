// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// This Source Code is partially based on the source code provided by the .NET Foundation.

// TODO: Mask (with placeholder); Clipboard paste;
// TODO: Constant decimals when formatting. Although this can actually be done with NumberFormatter.
// TODO: Lock to digit characters only by property
//
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Represents a control that can be used to display and edit numbers.
/// </summary>
[TemplatePart(Name = PART_ClearButton, Type = typeof(Button))]
[TemplatePart(Name = PART_InlineIncrementButton, Type = typeof(RepeatButton))]
[TemplatePart(Name = PART_InlineDecrementButton, Type = typeof(RepeatButton))]
public partial class NumberBox : TextBox
{
    // Template part names
    private const string PART_ClearButton = nameof(PART_ClearButton);
    private const string PART_InlineIncrementButton = nameof(PART_InlineIncrementButton);
    private const string PART_InlineDecrementButton = nameof(PART_InlineDecrementButton);

    private bool _valueUpdating;

    /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double?),
        typeof(NumberBox),
        new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnValueChanged,
            null,
            false,
            UpdateSourceTrigger.LostFocus
        )
    );

    /// <summary>Identifies the <see cref="MaxDecimalPlaces"/> dependency property.</summary>
    public static readonly DependencyProperty MaxDecimalPlacesProperty = DependencyProperty.Register(
        nameof(MaxDecimalPlaces),
        typeof(int),
        typeof(NumberBox),
        new PropertyMetadata(6, OnMaxDecimalPlacesChanged)
    );

    /// <summary>Identifies the <see cref="SmallChange"/> dependency property.</summary>
    public static readonly DependencyProperty SmallChangeProperty = DependencyProperty.Register(
        nameof(SmallChange),
        typeof(double),
        typeof(NumberBox),
        new PropertyMetadata(1.0d)
    );

    /// <summary>Identifies the <see cref="LargeChange"/> dependency property.</summary>
    public static readonly DependencyProperty LargeChangeProperty = DependencyProperty.Register(
        nameof(LargeChange),
        typeof(double),
        typeof(NumberBox),
        new PropertyMetadata(10.0d)
    );

    /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum),
        typeof(double),
        typeof(NumberBox),
        new PropertyMetadata(double.MaxValue, OnRangeChanged)
    );

    /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum),
        typeof(double),
        typeof(NumberBox),
        new PropertyMetadata(double.MinValue, OnRangeChanged)
    );

    /// <summary>Identifies the <see cref="AcceptsExpression"/> dependency property.</summary>
    public static readonly DependencyProperty AcceptsExpressionProperty = DependencyProperty.Register(
        nameof(AcceptsExpression),
        typeof(bool),
        typeof(NumberBox),
        new PropertyMetadata(false)
    );

    /// <summary>Identifies the <see cref="SpinButtonPlacementMode"/> dependency property.</summary>
    public static readonly DependencyProperty SpinButtonPlacementModeProperty = DependencyProperty.Register(
        nameof(SpinButtonPlacementMode),
        typeof(NumberBoxSpinButtonPlacementMode),
        typeof(NumberBox),
        new PropertyMetadata(NumberBoxSpinButtonPlacementMode.Inline)
    );

    /// <summary>Identifies the <see cref="ValidationMode"/> dependency property.</summary>
    public static readonly DependencyProperty ValidationModeProperty = DependencyProperty.Register(
        nameof(ValidationMode),
        typeof(NumberBoxValidationMode),
        typeof(NumberBox),
        new PropertyMetadata(NumberBoxValidationMode.InvalidInputOverwritten)
    );

    /// <summary>Identifies the <see cref="NumberFormatter"/> dependency property.</summary>
    public static readonly DependencyProperty NumberFormatterProperty = DependencyProperty.Register(
        nameof(NumberFormatter),
        typeof(INumberFormatter),
        typeof(NumberBox),
        new PropertyMetadata(null, OnNumberFormatterChanged)
    );

    /// <summary>Identifies the <see cref="ValueChanged"/> routed event.</summary>
    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged),
        RoutingStrategy.Bubble,
        typeof(NumberBoxValueChangedEvent),
        typeof(NumberBox)
    );

    /// <summary>
    /// Gets or sets the numeric value of a <see cref="NumberBox"/>.
    /// </summary>
    public double? Value
    {
        get => (double?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Gets or sets the number of decimal places to be rounded when converting from Text to Value.
    /// </summary>
    public int MaxDecimalPlaces
    {
        get => (int)GetValue(MaxDecimalPlacesProperty);
        set => SetValue(MaxDecimalPlacesProperty, value);
    }

    /// <summary>
    /// Gets or sets the value that is added to or subtracted from <see cref="Value"/> when a small change is made, such as with an arrow key or scrolling.
    /// </summary>
    public double SmallChange
    {
        get => (double)GetValue(SmallChangeProperty);
        set => SetValue(SmallChangeProperty, value);
    }

    /// <summary>
    /// Gets or sets the value that is added to or subtracted from <see cref="Value"/> when a large change is made, such as with the PageUP and PageDown keys.
    /// </summary>
    public double LargeChange
    {
        get => (double)GetValue(LargeChangeProperty);
        set => SetValue(LargeChangeProperty, value);
    }

    /// <summary>
    /// Gets or sets the numerical maximum for <see cref="Value"/>.
    /// </summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>
    /// Gets or sets the numerical minimum for <see cref="Value"/>.
    /// </summary>
    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>
    /// Gets or sets a value that has no effect; <see cref="NumberBox"/> parses plain numbers only.
    /// </summary>
    [Obsolete("AcceptsExpression has no effect. NumberBox does not evaluate expressions.")]
    public bool AcceptsExpression
    {
        get => (bool)GetValue(AcceptsExpressionProperty);
        set => SetValue(AcceptsExpressionProperty, value);
    }

    /// <summary>
    /// Gets or sets the number formatter.
    /// </summary>
    public INumberFormatter? NumberFormatter
    {
        get => (INumberFormatter?)GetValue(NumberFormatterProperty);
        set => SetValue(NumberFormatterProperty, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the placement of buttons used to increment or decrement the <see cref="Value"/> property.
    /// </summary>
    public NumberBoxSpinButtonPlacementMode SpinButtonPlacementMode
    {
        get => (NumberBoxSpinButtonPlacementMode)GetValue(SpinButtonPlacementModeProperty);
        set => SetValue(SpinButtonPlacementModeProperty, value);
    }

    /// <summary>
    /// Gets or sets how <see cref="NumberBox"/> reconciles typed text with <see cref="Value"/> at commit.
    /// </summary>
    /// <remarks>
    /// Typed text reaches <see cref="Value"/> only at commit in every mode. <see cref="NumberBoxValidationMode.InvalidInputOverwritten"/>
    /// coerces <see cref="Value"/> into <see cref="Minimum"/>/<see cref="Maximum"/>; <see cref="NumberBoxValidationMode.Disabled"/> never does,
    /// except that a spin step stops at the bound. It is not a WPF <see cref="System.Windows.Controls.ValidationRule"/>; a binding-level
    /// validation error requires a <see cref="System.Windows.Controls.ValidationRule"/> on the <see cref="Value"/> binding.
    /// </remarks>
    public NumberBoxValidationMode ValidationMode
    {
        get => (NumberBoxValidationMode)GetValue(ValidationModeProperty);
        set => SetValue(ValidationModeProperty, value);
    }

    /// <summary>
    /// Occurs after the user triggers evaluation of new input by pressing the Enter key, clicking a spin button, or by changing focus.
    /// </summary>
    public event NumberBoxValueChangedEvent ValueChanged
    {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    static NumberBox()
    {
        AcceptsReturnProperty.OverrideMetadata(typeof(NumberBox), new FrameworkPropertyMetadata(false));
        MaxLinesProperty.OverrideMetadata(typeof(NumberBox), new FrameworkPropertyMetadata(1));
        MinLinesProperty.OverrideMetadata(typeof(NumberBox), new FrameworkPropertyMetadata(1));
    }

    public NumberBox()
        : base()
    {
        NumberFormatter ??= GetRegionalSettingsAwareDecimalFormatter();

        DataObject.AddPastingHandler(this, OnClipboardPaste);
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (IsReadOnly)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.PageUp:
                StepValue(LargeChange);
                e.Handled = true;
                break;
            case Key.PageDown:
                StepValue(-LargeChange);
                e.Handled = true;
                break;
            case Key.Up:
                StepValue(SmallChange);
                e.Handled = true;
                break;
            case Key.Down:
                StepValue(-SmallChange);
                e.Handled = true;
                break;
        }

        base.OnPreviewKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyUp(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                if (TextWrapping != TextWrapping.Wrap)
                {
                    ValidateInput();
                    MoveCaretToTextEnd();
                }

                e.Handled = true;
                break;

            case Key.Escape:
                UpdateTextToValue();
                e.Handled = true;
                break;
        }

        base.OnPreviewKeyUp(e);
    }

    /// <inheritdoc />
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        var oldValue = Value;
        ValidateInput();

        // Update binding source if value changed
        if (!Equals(oldValue, Value))
        {
            GetBindingExpression(ValueProperty)?.UpdateSource();
        }
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        SubscribeToButtonClickEvent<System.Windows.Controls.Button>(
            PART_ClearButton,
            () => OnClearButtonClick()
        );
        SubscribeToButtonClickEvent<RepeatButton>(PART_InlineIncrementButton, () => StepValue(SmallChange));
        SubscribeToButtonClickEvent<RepeatButton>(PART_InlineDecrementButton, () => StepValue(-SmallChange));

        // If Text has been set, but Value hasn't, update Value based on Text.
        if (string.IsNullOrEmpty(Text) && Value != null)
        {
            UpdateValueToText();
        }
        else
        {
            UpdateTextToValue();
        }

        base.OnApplyTemplate();
    }

    private void SubscribeToButtonClickEvent<TButton>(string elementName, Action action)
        where TButton : ButtonBase
    {
        if (GetTemplateChild(elementName) is TButton button)
        {
            button.Click += (s, e) =>
            {
                Debug.InfoWriteLineForButtonClick(s);
                action();
            };
        }
    }

    /// <summary>
    /// Is called when <see cref="Value"/> in this <see cref="NumberBox"/> changes.
    /// </summary>
    protected virtual void OnValueChanged(DependencyObject d, double? oldValue)
    {
        CommitValue(oldValue);
    }

    /// <summary>
    /// Clamps the current <see cref="Value"/> into range, publishes at most one <see cref="ValueChanged"/>
    /// event carrying the final clamped value, and refreshes <see cref="System.Windows.Controls.TextBox.Text"/>.
    /// </summary>
    private void CommitValue(double? previousValue)
    {
        if (_valueUpdating)
        {
            return;
        }

        _valueUpdating = true;

        double? requestedValue = Value;
        double? finalValue = requestedValue is double requested && CoercesToRange ? ClampToRange(requested) : requestedValue;

        if (!Equals(finalValue, requestedValue))
        {
            SetCurrentValue(ValueProperty, finalValue);
        }

        if (!Equals(finalValue, previousValue))
        {
            RaiseEvent(new NumberBoxValueChangedEventArgs(previousValue, finalValue, this));
        }

        UpdateTextToValue();

        _valueUpdating = false;
    }

    private bool CoercesToRange => ValidationMode == NumberBoxValidationMode.InvalidInputOverwritten;

    /// <summary>
    /// Clamps <paramref name="value"/> into the effective <see cref="Minimum"/>/<see cref="Maximum"/> range,
    /// treating an inverted range as its swapped bounds and a non-finite bound as no bound on that side.
    /// </summary>
    private double ClampToRange(double value)
    {
        if (double.IsNaN(value))
        {
            return value;
        }

        double minimum = double.IsNaN(Minimum) ? double.NegativeInfinity : Minimum;
        double maximum = double.IsNaN(Maximum) ? double.PositiveInfinity : Maximum;

        if (minimum > maximum)
        {
            (minimum, maximum) = (maximum, minimum);
        }

        return Math.Clamp(value, minimum, maximum);
    }

    /// <summary>
    /// Gets <see cref="MaxDecimalPlaces"/> clamped to the range <see cref="Math.Round(double, int)"/> accepts.
    /// </summary>
    private int ClampDecimalPlaces()
    {
        return Math.Clamp(MaxDecimalPlaces, 0, 15);
    }

    /// <summary>
    /// Is called when something is pasted in this <see cref="NumberBox"/>.
    /// </summary>
    protected virtual void OnClipboardPaste(object sender, DataObjectPastingEventArgs e)
    {
        // TODO: Fix clipboard
        if (sender is not NumberBox)
        {
            return;
        }

        ValidateInput();
    }

    private void StepValue(double? change)
    {
        Debug.InfoWriteLine($"{typeof(NumberBox)} {nameof(StepValue)} raised, change {change}");

        // Before adjusting the value, validate the contents of the textbox so we don't override it.
        ValidateInput();

        var newValue = Value ?? 0;

        if (change is not null)
        {
            newValue += change ?? 0d;
        }

        SetCurrentValue(ValueProperty, ClampToRange(newValue));

        MoveCaretToTextEnd();
    }

    private void UpdateTextToValue()
    {
        var newText = string.Empty;

        if (Value is not null && NumberFormatter is not null)
        {
            newText = NumberFormatter.FormatDouble(Math.Round((double)Value, ClampDecimalPlaces()));
        }

        SetCurrentValue(TextProperty, newText);
    }

    private void UpdateValueToText()
    {
        ValidateInput();
    }

    private void ValidateInput()
    {
        var text = Text.Trim();

        if (string.IsNullOrEmpty(text))
        {
            SetCurrentValue(ValueProperty, null);

            return;
        }

        var numberParser = NumberFormatter as INumberParser;
        var value = numberParser!.ParseDouble(text);

        if (value is null)
        {
            if (ValidationMode == NumberBoxValidationMode.Disabled)
            {
                return;
            }

            UpdateTextToValue();

            return;
        }

        if (Equals(Value, value))
        {
            UpdateTextToValue();

            return;
        }

        if (CoercesToRange)
        {
            value = ClampToRange(value.Value);
        }

        SetCurrentValue(ValueProperty, value);
    }

    private void MoveCaretToTextEnd()
    {
        CaretIndex = Text.Length;
    }

    /// <summary>
    /// Re-rounds the current <see cref="Value"/> to <see cref="MaxDecimalPlaces"/> and publishes the
    /// change through the same clamp/event path as any other value change.
    /// </summary>
    private void RoundValueToDecimalPlaces()
    {
        if (_valueUpdating || Value is not double value)
        {
            return;
        }

        double rounded = Math.Round(value, ClampDecimalPlaces());

        if (!Equals(rounded, value))
        {
            SetCurrentValue(ValueProperty, rounded);
        }
        else
        {
            UpdateTextToValue();
        }
    }

    private static INumberFormatter GetRegionalSettingsAwareDecimalFormatter()
    {
        return new ValidateNumberFormatter();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumberBox numberBox)
        {
            numberBox.OnValueChanged(d, (double?)e.OldValue);
        }
    }

    private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumberBox numberBox)
        {
            numberBox.CommitValue(numberBox.Value);
        }
    }

    private static void OnMaxDecimalPlacesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NumberBox numberBox)
        {
            numberBox.RoundValueToDecimalPlaces();
        }
    }

    private static void OnNumberFormatterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not INumberParser)
        {
            throw new InvalidOperationException(
                $"{nameof(NumberFormatter)} must implement {typeof(INumberParser)}"
            );
        }
    }

    private static partial class Debug
    {
        public static partial void InfoWriteLine(string debugLine);

        public static partial void InfoWriteLineForButtonClick(object sender);

#if DEBUG
        public static partial void InfoWriteLine(string debugLine)
        {
            System.Diagnostics.Debug.WriteLine($"INFO: {debugLine}", "Wpf.Ui.NumberBox");
        }

        public static partial void InfoWriteLineForButtonClick(object sender)
        {
            var buttonName =
                (sender is ButtonBase element)
                    ? element.Name
                    : throw new InvalidCastException(nameof(sender));

            InfoWriteLine($"{typeof(NumberBox)} {buttonName} clicked");
        }

#else
        public static partial void InfoWriteLine(string debugLine)
        {
            // Do nothing in non-DEBUG builds
        }

        public static partial void InfoWriteLineForButtonClick(object sender)
        {
            // Do nothing in non-DEBUG builds
        }

#endif // DEBUG
    }
}

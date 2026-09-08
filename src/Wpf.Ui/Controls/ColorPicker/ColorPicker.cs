// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Represents a control that lets a user pick a color using a color spectrum, sliders, and text input.
/// </summary>
[TemplatePart(Name = PartColorSpectrum, Type = typeof(ColorSpectrum))]
[TemplatePart(Name = PartValueSlider, Type = typeof(ColorSlider))]
[TemplatePart(Name = PartAlphaSlider, Type = typeof(ColorSlider))]
[TemplatePart(Name = PartHexTextBox, Type = typeof(TextBox))]
[TemplatePart(Name = PartColorPreview, Type = typeof(Border))]
[TemplatePart(Name = PartPreviousColorPreview, Type = typeof(Border))]
[TemplatePart(Name = PartRedInput, Type = typeof(NumberBox))]
[TemplatePart(Name = PartGreenInput, Type = typeof(NumberBox))]
[TemplatePart(Name = PartBlueInput, Type = typeof(NumberBox))]
[TemplatePart(Name = PartHueInput, Type = typeof(NumberBox))]
[TemplatePart(Name = PartSaturationInput, Type = typeof(NumberBox))]
[TemplatePart(Name = PartValueInput, Type = typeof(NumberBox))]
[TemplatePart(Name = PartAlphaInput, Type = typeof(NumberBox))]
[TemplatePart(Name = PartChannelToggle, Type = typeof(ToggleButton))]
[TemplatePart(Name = PartCopyHexButton, Type = typeof(System.Windows.Controls.Button))]
[TemplatePart(Name = PartColorPalette, Type = typeof(ItemsControl))]
[TemplatePart(Name = PartRecentColors, Type = typeof(ItemsControl))]
public class ColorPicker : Control
{
    private const string PartColorSpectrum = "PART_ColorSpectrum";
    private const string PartValueSlider = "PART_ValueSlider";
    private const string PartAlphaSlider = "PART_AlphaSlider";
    private const string PartHexTextBox = "PART_HexTextBox";
    private const string PartColorPreview = "PART_ColorPreview";
    private const string PartPreviousColorPreview = "PART_PreviousColorPreview";
    private const string PartRedInput = "PART_RedInput";
    private const string PartGreenInput = "PART_GreenInput";
    private const string PartBlueInput = "PART_BlueInput";
    private const string PartHueInput = "PART_HueInput";
    private const string PartSaturationInput = "PART_SaturationInput";
    private const string PartValueInput = "PART_ValueInput";
    private const string PartAlphaInput = "PART_AlphaInput";
    private const string PartChannelToggle = "PART_ChannelToggle";
    private const string PartCopyHexButton = "PART_CopyHexButton";
    private const string PartColorPalette = "PART_ColorPalette";
    private const string PartRecentColors = "PART_RecentColors";

    private bool _isUpdating;
    private bool _spectrumInitiated;
    private ColorSpectrum? _spectrum;
    private ColorSlider? _valueSlider;
    private ColorSlider? _alphaSlider;
    private TextBox? _hexTextBox;
    private Border? _colorPreview;
    private Border? _previousColorPreview;
    private NumberBox? _redInput;
    private NumberBox? _greenInput;
    private NumberBox? _blueInput;
    private NumberBox? _hueInput;
    private NumberBox? _saturationInput;
    private NumberBox? _valueInput;
    private NumberBox? _alphaInput;
    private ToggleButton? _channelToggle;
    private System.Windows.Controls.Button? _copyHexButton;
    private DispatcherTimer? _copyFeedbackTimer;
    private object? _copyHexButtonOriginalToolTip;

    // HSV state (source of truth for internal calculations)
    private double _hue;
    private double _saturation = 100;
    private double _value = 100;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Color"/> dependency property.</summary>
    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
        nameof(Color),
        typeof(Color),
        typeof(ColorPicker),
        new FrameworkPropertyMetadata(Colors.White, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorChanged));

    /// <summary>Identifies the <see cref="PreviousColor"/> dependency property.</summary>
    public static readonly DependencyProperty PreviousColorProperty = DependencyProperty.Register(
        nameof(PreviousColor),
        typeof(Color?),
        typeof(ColorPicker),
        new PropertyMetadata(null, OnPreviousColorChanged));

    /// <summary>Identifies the <see cref="IsAlphaEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsAlphaEnabledProperty = DependencyProperty.Register(
        nameof(IsAlphaEnabled), typeof(bool), typeof(ColorPicker), new PropertyMetadata(false, OnIsAlphaEnabledChanged));

    /// <summary>Identifies the <see cref="IsColorSpectrumVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsColorSpectrumVisibleProperty = DependencyProperty.Register(
        nameof(IsColorSpectrumVisible), typeof(bool), typeof(ColorPicker), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="IsColorSliderVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsColorSliderVisibleProperty = DependencyProperty.Register(
        nameof(IsColorSliderVisible), typeof(bool), typeof(ColorPicker), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="IsHexInputVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsHexInputVisibleProperty = DependencyProperty.Register(
        nameof(IsHexInputVisible), typeof(bool), typeof(ColorPicker), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="IsColorChannelTextInputVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsColorChannelTextInputVisibleProperty = DependencyProperty.Register(
        nameof(IsColorChannelTextInputVisible), typeof(bool), typeof(ColorPicker), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="IsColorPreviewVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsColorPreviewVisibleProperty = DependencyProperty.Register(
        nameof(IsColorPreviewVisible), typeof(bool), typeof(ColorPicker), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="IsColorPaletteVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsColorPaletteVisibleProperty = DependencyProperty.Register(
        nameof(IsColorPaletteVisible), typeof(bool), typeof(ColorPicker), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="ColorSpectrumShape"/> dependency property.</summary>
    public static readonly DependencyProperty ColorSpectrumShapeProperty = DependencyProperty.Register(
        nameof(ColorSpectrumShape), typeof(ColorSpectrumShape), typeof(ColorPicker), new PropertyMetadata(ColorSpectrumShape.Box));

    /// <summary>Identifies the <see cref="ColorChannelMode"/> dependency property.</summary>
    public static readonly DependencyProperty ColorChannelModeProperty = DependencyProperty.Register(
        nameof(ColorChannelMode), typeof(ColorChannelMode), typeof(ColorPicker), new PropertyMetadata(ColorChannelMode.Rgb));

    /// <summary>Identifies the <see cref="MinHue"/> dependency property.</summary>
    public static readonly DependencyProperty MinHueProperty = DependencyProperty.Register(
        nameof(MinHue), typeof(int), typeof(ColorPicker), new PropertyMetadata(0));

    /// <summary>Identifies the <see cref="MaxHue"/> dependency property.</summary>
    public static readonly DependencyProperty MaxHueProperty = DependencyProperty.Register(
        nameof(MaxHue), typeof(int), typeof(ColorPicker), new PropertyMetadata(359));

    /// <summary>Identifies the <see cref="MinSaturation"/> dependency property.</summary>
    public static readonly DependencyProperty MinSaturationProperty = DependencyProperty.Register(
        nameof(MinSaturation), typeof(int), typeof(ColorPicker), new PropertyMetadata(0));

    /// <summary>Identifies the <see cref="MaxSaturation"/> dependency property.</summary>
    public static readonly DependencyProperty MaxSaturationProperty = DependencyProperty.Register(
        nameof(MaxSaturation), typeof(int), typeof(ColorPicker), new PropertyMetadata(100));

    /// <summary>Identifies the <see cref="MinValue"/> dependency property.</summary>
    public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register(
        nameof(MinValue), typeof(int), typeof(ColorPicker), new PropertyMetadata(0));

    /// <summary>Identifies the <see cref="MaxValue"/> dependency property.</summary>
    public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
        nameof(MaxValue), typeof(int), typeof(ColorPicker), new PropertyMetadata(100));

    /// <summary>Identifies the <see cref="PaletteColors"/> dependency property.</summary>
    public static readonly DependencyProperty PaletteColorsProperty = DependencyProperty.Register(
        nameof(PaletteColors),
        typeof(IList<Color>),
        typeof(ColorPicker),
        new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="MaxRecentColors"/> dependency property.</summary>
    public static readonly DependencyProperty MaxRecentColorsProperty = DependencyProperty.Register(
        nameof(MaxRecentColors), typeof(int), typeof(ColorPicker), new PropertyMetadata(10));

    /// <summary>Identifies the <see cref="ValueSliderLabelText"/> dependency property.</summary>
    public static readonly DependencyProperty ValueSliderLabelTextProperty = DependencyProperty.Register(
        nameof(ValueSliderLabelText), typeof(string), typeof(ColorPicker), new PropertyMetadata("Value"));

    /// <summary>Identifies the <see cref="OpacitySliderLabelText"/> dependency property.</summary>
    public static readonly DependencyProperty OpacitySliderLabelTextProperty = DependencyProperty.Register(
        nameof(OpacitySliderLabelText), typeof(string), typeof(ColorPicker), new PropertyMetadata("Opacity"));

    /// <summary>Identifies the <see cref="HexInputLabelText"/> dependency property.</summary>
    public static readonly DependencyProperty HexInputLabelTextProperty = DependencyProperty.Register(
        nameof(HexInputLabelText), typeof(string), typeof(ColorPicker), new PropertyMetadata("Hex"));

    /// <summary>Identifies the <see cref="CopyHexButtonToolTip"/> dependency property.</summary>
    public static readonly DependencyProperty CopyHexButtonToolTipProperty = DependencyProperty.Register(
        nameof(CopyHexButtonToolTip), typeof(string), typeof(ColorPicker), new PropertyMetadata("Copy hex value"));

    /// <summary>Identifies the <see cref="CopyHexConfirmationText"/> dependency property.</summary>
    public static readonly DependencyProperty CopyHexConfirmationTextProperty = DependencyProperty.Register(
        nameof(CopyHexConfirmationText), typeof(string), typeof(ColorPicker), new PropertyMetadata("Copied!"));

    /// <summary>Identifies the <see cref="RevertToPreviousColorToolTip"/> dependency property.</summary>
    public static readonly DependencyProperty RevertToPreviousColorToolTipProperty = DependencyProperty.Register(
        nameof(RevertToPreviousColorToolTip), typeof(string), typeof(ColorPicker), new PropertyMetadata("Click to revert to previous color"));

    /// <summary>Identifies the <see cref="PaletteSectionHeaderText"/> dependency property.</summary>
    public static readonly DependencyProperty PaletteSectionHeaderTextProperty = DependencyProperty.Register(
        nameof(PaletteSectionHeaderText), typeof(string), typeof(ColorPicker), new PropertyMetadata("Colors"));

    /// <summary>Identifies the <see cref="RecentColorsSectionHeaderText"/> dependency property.</summary>
    public static readonly DependencyProperty RecentColorsSectionHeaderTextProperty = DependencyProperty.Register(
        nameof(RecentColorsSectionHeaderText), typeof(string), typeof(ColorPicker), new PropertyMetadata("Recent"));

    #endregion

    #region Routed Events

    /// <summary>Identifies the <see cref="ColorChanged"/> routed event.</summary>
    public static readonly RoutedEvent ColorChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ColorChanged),
        RoutingStrategy.Bubble,
        typeof(EventHandler<ColorChangedEventArgs>),
        typeof(ColorPicker));

    /// <summary>
    /// Occurs when the <see cref="Color"/> property changes.
    /// </summary>
    public event EventHandler<ColorChangedEventArgs> ColorChanged
    {
        add => AddHandler(ColorChangedEvent, value);
        remove => RemoveHandler(ColorChangedEvent, value);
    }

    #endregion

    #region CLR Properties

    /// <summary>Gets or sets the currently selected color.</summary>
    [Bindable(true)]
    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>Gets or sets the previous color for comparison.</summary>
    [Bindable(true)]
    public Color? PreviousColor
    {
        get => (Color?)GetValue(PreviousColorProperty);
        set => SetValue(PreviousColorProperty, value);
    }

    /// <summary>Gets or sets whether the alpha channel slider and input are visible.</summary>
    [Bindable(true)]
    public bool IsAlphaEnabled
    {
        get => (bool)GetValue(IsAlphaEnabledProperty);
        set => SetValue(IsAlphaEnabledProperty, value);
    }

    /// <summary>Gets or sets whether the color spectrum is visible.</summary>
    [Bindable(true)]
    public bool IsColorSpectrumVisible
    {
        get => (bool)GetValue(IsColorSpectrumVisibleProperty);
        set => SetValue(IsColorSpectrumVisibleProperty, value);
    }

    /// <summary>Gets or sets whether the value slider is visible.</summary>
    [Bindable(true)]
    public bool IsColorSliderVisible
    {
        get => (bool)GetValue(IsColorSliderVisibleProperty);
        set => SetValue(IsColorSliderVisibleProperty, value);
    }

    /// <summary>Gets or sets whether the hex input is visible.</summary>
    [Bindable(true)]
    public bool IsHexInputVisible
    {
        get => (bool)GetValue(IsHexInputVisibleProperty);
        set => SetValue(IsHexInputVisibleProperty, value);
    }

    /// <summary>Gets or sets whether the channel text inputs are visible.</summary>
    [Bindable(true)]
    public bool IsColorChannelTextInputVisible
    {
        get => (bool)GetValue(IsColorChannelTextInputVisibleProperty);
        set => SetValue(IsColorChannelTextInputVisibleProperty, value);
    }

    /// <summary>Gets or sets whether the color preview is visible.</summary>
    [Bindable(true)]
    public bool IsColorPreviewVisible
    {
        get => (bool)GetValue(IsColorPreviewVisibleProperty);
        set => SetValue(IsColorPreviewVisibleProperty, value);
    }

    /// <summary>Gets or sets whether the color palette is visible.</summary>
    [Bindable(true)]
    public bool IsColorPaletteVisible
    {
        get => (bool)GetValue(IsColorPaletteVisibleProperty);
        set => SetValue(IsColorPaletteVisibleProperty, value);
    }

    /// <summary>Gets or sets a value that has no effect; the color spectrum always renders as a rectangle.</summary>
    [Obsolete("ColorSpectrumShape has no effect. The color spectrum always renders as a rectangle.")]
    [Bindable(true)]
    public ColorSpectrumShape ColorSpectrumShape
    {
        get => (ColorSpectrumShape)GetValue(ColorSpectrumShapeProperty);
        set => SetValue(ColorSpectrumShapeProperty, value);
    }

    /// <summary>Gets or sets the color channel input mode (RGB or HSV).</summary>
    [Bindable(true)]
    public ColorChannelMode ColorChannelMode
    {
        get => (ColorChannelMode)GetValue(ColorChannelModeProperty);
        set => SetValue(ColorChannelModeProperty, value);
    }

    /// <summary>Gets or sets the minimum hue value (0-359).</summary>
    public int MinHue { get => (int)GetValue(MinHueProperty); set => SetValue(MinHueProperty, value); }

    /// <summary>Gets or sets the maximum hue value (0-359).</summary>
    public int MaxHue { get => (int)GetValue(MaxHueProperty); set => SetValue(MaxHueProperty, value); }

    /// <summary>Gets or sets the minimum saturation value (0-100).</summary>
    public int MinSaturation { get => (int)GetValue(MinSaturationProperty); set => SetValue(MinSaturationProperty, value); }

    /// <summary>Gets or sets the maximum saturation value (0-100).</summary>
    public int MaxSaturation { get => (int)GetValue(MaxSaturationProperty); set => SetValue(MaxSaturationProperty, value); }

    /// <summary>Gets or sets the minimum value/brightness (0-100).</summary>
    public int MinValue { get => (int)GetValue(MinValueProperty); set => SetValue(MinValueProperty, value); }

    /// <summary>Gets or sets the maximum value/brightness (0-100).</summary>
    public int MaxValue { get => (int)GetValue(MaxValueProperty); set => SetValue(MaxValueProperty, value); }

    /// <summary>Gets or sets the palette colors to display.</summary>
    [Bindable(true)]
    public IList<Color>? PaletteColors
    {
        get => (IList<Color>?)GetValue(PaletteColorsProperty);
        set => SetValue(PaletteColorsProperty, value);
    }

    /// <summary>Gets or sets the maximum number of recent colors to track.</summary>
    public int MaxRecentColors
    {
        get => (int)GetValue(MaxRecentColorsProperty);
        set => SetValue(MaxRecentColorsProperty, value);
    }

    /// <summary>Gets the collection of recently used colors.</summary>
    public ObservableCollection<Color> RecentColors { get; } = new();

    /// <summary>Gets or sets the label text shown next to the value slider.</summary>
    [Bindable(true)]
    public string ValueSliderLabelText
    {
        get => (string)GetValue(ValueSliderLabelTextProperty);
        set => SetValue(ValueSliderLabelTextProperty, value);
    }

    /// <summary>Gets or sets the label text shown next to the alpha slider.</summary>
    [Bindable(true)]
    public string OpacitySliderLabelText
    {
        get => (string)GetValue(OpacitySliderLabelTextProperty);
        set => SetValue(OpacitySliderLabelTextProperty, value);
    }

    /// <summary>Gets or sets the label text shown next to the hex input.</summary>
    [Bindable(true)]
    public string HexInputLabelText
    {
        get => (string)GetValue(HexInputLabelTextProperty);
        set => SetValue(HexInputLabelTextProperty, value);
    }

    /// <summary>Gets or sets the tooltip shown on the copy-hex button.</summary>
    [Bindable(true)]
    public string CopyHexButtonToolTip
    {
        get => (string)GetValue(CopyHexButtonToolTipProperty);
        set => SetValue(CopyHexButtonToolTipProperty, value);
    }

    /// <summary>Gets or sets the text shown on the copy-hex button after a successful copy.</summary>
    [Bindable(true)]
    public string CopyHexConfirmationText
    {
        get => (string)GetValue(CopyHexConfirmationTextProperty);
        set => SetValue(CopyHexConfirmationTextProperty, value);
    }

    /// <summary>Gets or sets the tooltip shown on the previous-color preview.</summary>
    [Bindable(true)]
    public string RevertToPreviousColorToolTip
    {
        get => (string)GetValue(RevertToPreviousColorToolTipProperty);
        set => SetValue(RevertToPreviousColorToolTipProperty, value);
    }

    /// <summary>Gets or sets the header text shown above the color palette section.</summary>
    [Bindable(true)]
    public string PaletteSectionHeaderText
    {
        get => (string)GetValue(PaletteSectionHeaderTextProperty);
        set => SetValue(PaletteSectionHeaderTextProperty, value);
    }

    /// <summary>Gets or sets the header text shown above the recent colors section.</summary>
    [Bindable(true)]
    public string RecentColorsSectionHeaderText
    {
        get => (string)GetValue(RecentColorsSectionHeaderTextProperty);
        set => SetValue(RecentColorsSectionHeaderTextProperty, value);
    }

    #endregion

    static ColorPicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPicker), new FrameworkPropertyMetadata(typeof(ColorPicker)));
    }

    public ColorPicker()
    {
        // Default palette: common useful colors
        PaletteColors = new List<Color>
        {
            // Row 1: Reds & Pinks
            Color.FromRgb(255, 255, 255), // White
            Color.FromRgb(255, 0, 0),     // Red
            Color.FromRgb(220, 20, 60),   // Crimson
            Color.FromRgb(255, 105, 180), // Hot Pink
            Color.FromRgb(255, 182, 193), // Light Pink

            // Row 2: Oranges & Yellows
            Color.FromRgb(255, 165, 0),   // Orange
            Color.FromRgb(255, 140, 0),   // Dark Orange
            Color.FromRgb(255, 215, 0),   // Gold
            Color.FromRgb(255, 255, 0),   // Yellow
            Color.FromRgb(255, 255, 224), // Light Yellow

            // Row 3: Greens
            Color.FromRgb(0, 255, 0),     // Lime
            Color.FromRgb(0, 128, 0),     // Green
            Color.FromRgb(34, 139, 34),   // Forest Green
            Color.FromRgb(0, 255, 127),   // Spring Green
            Color.FromRgb(144, 238, 144), // Light Green

            // Row 4: Cyans & Blues
            Color.FromRgb(0, 255, 255),   // Cyan
            Color.FromRgb(0, 191, 255),   // Deep Sky Blue
            Color.FromRgb(0, 0, 255),     // Blue
            Color.FromRgb(0, 0, 139),     // Dark Blue
            Color.FromRgb(173, 216, 230), // Light Blue

            // Row 5: Purples
            Color.FromRgb(128, 0, 128),   // Purple
            Color.FromRgb(148, 0, 211),   // Dark Violet
            Color.FromRgb(255, 0, 255),   // Magenta
            Color.FromRgb(186, 85, 211),  // Medium Orchid
            Color.FromRgb(216, 191, 216), // Thistle

            // Row 6: Neutrals
            Color.FromRgb(0, 0, 0),       // Black
            Color.FromRgb(64, 64, 64),    // Dark Gray
            Color.FromRgb(128, 128, 128), // Gray
            Color.FromRgb(192, 192, 192), // Silver
            Color.FromRgb(245, 245, 245), // White Smoke
        };
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Get template parts
        _spectrum = GetTemplateChild(PartColorSpectrum) as ColorSpectrum;
        _valueSlider = GetTemplateChild(PartValueSlider) as ColorSlider;
        _alphaSlider = GetTemplateChild(PartAlphaSlider) as ColorSlider;
        _hexTextBox = GetTemplateChild(PartHexTextBox) as TextBox;
        _colorPreview = GetTemplateChild(PartColorPreview) as Border;
        _previousColorPreview = GetTemplateChild(PartPreviousColorPreview) as Border;
        _redInput = GetTemplateChild(PartRedInput) as NumberBox;
        _greenInput = GetTemplateChild(PartGreenInput) as NumberBox;
        _blueInput = GetTemplateChild(PartBlueInput) as NumberBox;
        _hueInput = GetTemplateChild(PartHueInput) as NumberBox;
        _saturationInput = GetTemplateChild(PartSaturationInput) as NumberBox;
        _valueInput = GetTemplateChild(PartValueInput) as NumberBox;
        _alphaInput = GetTemplateChild(PartAlphaInput) as NumberBox;
        _channelToggle = GetTemplateChild(PartChannelToggle) as ToggleButton;
        _copyHexButton = GetTemplateChild(PartCopyHexButton) as System.Windows.Controls.Button;

        // Wire up spectrum changes
        if (_spectrum != null)
        {
            var hueDesc = DependencyPropertyDescriptor.FromProperty(ColorSpectrum.HueProperty, typeof(ColorSpectrum));
            hueDesc?.AddValueChanged(_spectrum, OnSpectrumHueSatChanged);

            var satDesc = DependencyPropertyDescriptor.FromProperty(ColorSpectrum.SaturationProperty, typeof(ColorSpectrum));
            satDesc?.AddValueChanged(_spectrum, OnSpectrumHueSatChanged);
        }

        // Wire up value slider
        if (_valueSlider != null)
        {
            _valueSlider.ValueChanged += OnValueSliderChanged;
        }

        // Wire up alpha slider
        if (_alphaSlider != null)
        {
            _alphaSlider.ValueChanged += OnAlphaSliderChanged;
        }

        // Wire up hex text box
        if (_hexTextBox != null)
        {
            _hexTextBox.KeyDown += OnHexTextBoxKeyDown;
            _hexTextBox.LostFocus += OnHexTextBoxLostFocus;
        }

        // Wire up RGB inputs
        if (_redInput != null)
        {
            _redInput.ValueChanged += OnRgbInputChanged;
        }

        if (_greenInput != null)
        {
            _greenInput.ValueChanged += OnRgbInputChanged;
        }

        if (_blueInput != null)
        {
            _blueInput.ValueChanged += OnRgbInputChanged;
        }

        // Wire up HSV inputs
        if (_hueInput != null)
        {
            _hueInput.ValueChanged += OnHsvInputChanged;
        }

        if (_saturationInput != null)
        {
            _saturationInput.ValueChanged += OnHsvInputChanged;
        }

        if (_valueInput != null)
        {
            _valueInput.ValueChanged += OnHsvInputChanged;
        }

        // Wire up alpha input
        if (_alphaInput != null)
        {
            _alphaInput.ValueChanged += OnAlphaInputChanged;
        }

        // Wire up channel toggle
        if (_channelToggle != null)
        {
            _channelToggle.Checked += OnChannelToggleChanged;
            _channelToggle.Unchecked += OnChannelToggleChanged;
        }

        // Wire up copy hex button
        if (_copyHexButton != null)
        {
            _copyHexButton.Click += OnCopyHexClick;
        }

        // Wire up previous color preview click to revert
        if (_previousColorPreview != null)
        {
            _previousColorPreview.MouseLeftButtonDown += OnPreviousColorClick;
        }

        // Wire up palette and recent color clicks via bubbling
        AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnPaletteButtonClick));

        // Initialize from current color
        SyncFromColor(Color);

        // Initialize previous color preview
        if (_previousColorPreview != null && PreviousColor is Color prevColor)
        {
#pragma warning disable WPF0041
            _previousColorPreview.Background = new SolidColorBrush(prevColor);
#pragma warning restore WPF0041
        }
    }

    #region Event Handlers

    private void OnSpectrumHueSatChanged(object? sender, EventArgs e)
    {
        if (_isUpdating || _spectrum == null) return;
        _spectrumInitiated = true;
        _isUpdating = true;
        try
        {
            _hue = _spectrum.Hue;
            _saturation = _spectrum.Saturation;
            UpdateColorFromHsv();
        }
        finally
        {
            _isUpdating = false;
            _spectrumInitiated = false;
        }
    }

    private void OnValueSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdating || _valueSlider == null) return;
        _isUpdating = true;
        try
        {
            _value = _valueSlider.Value;
#pragma warning disable WPF0041 // Template children are internal — direct assignment is correct
            if (_spectrum != null)
            {
                _spectrum.Value = _value;
            }
#pragma warning restore WPF0041
            UpdateColorFromHsv();
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnAlphaSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdating || _alphaSlider == null) return;
        _isUpdating = true;
        try
        {
            var color = Color;
            SetCurrentValue(ColorProperty, Color.FromArgb((byte)_alphaSlider.Value, color.R, color.G, color.B));
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnHexTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyHexInput();
            e.Handled = true;
        }
    }

    private void OnHexTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        ApplyHexInput();
    }

    private void OnRgbInputChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            var r = (byte)(_redInput?.Value ?? 0);
            var g = (byte)(_greenInput?.Value ?? 0);
            var b = (byte)(_blueInput?.Value ?? 0);
            var a = IsAlphaEnabled ? (byte)(_alphaInput?.Value ?? 255) : Color.A;
            SetCurrentValue(ColorProperty, Color.FromArgb(a, r, g, b));
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnHsvInputChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            _hue = _hueInput?.Value ?? 0;
            _saturation = _saturationInput?.Value ?? 0;
            _value = _valueInput?.Value ?? 0;
            UpdateColorFromHsv();
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnAlphaInputChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            var color = Color;
            SetCurrentValue(ColorProperty, Color.FromArgb((byte)(_alphaInput?.Value ?? 255), color.R, color.G, color.B));
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnChannelToggleChanged(object sender, RoutedEventArgs e)
    {
        SetCurrentValue(ColorChannelModeProperty, _channelToggle?.IsChecked == true ? (object)ColorChannelMode.Hsv : ColorChannelMode.Rgb);
    }

    private void OnCopyHexClick(object sender, RoutedEventArgs e)
    {
        if (_hexTextBox is null)
        {
            return;
        }

        if (TryCopyToClipboard(_hexTextBox.Text))
        {
            ShowCopyConfirmation();
        }
    }

    /// <summary>
    /// Writes <paramref name="text"/> to the clipboard, retrying briefly if another process is
    /// transiently holding it open (a common, well-documented OLE clipboard condition).
    /// </summary>
    /// <param name="text">The text to copy.</param>
    /// <param name="writer">The clipboard-write delegate; defaults to the system clipboard when <see langword="null"/>.</param>
    /// <param name="maxAttempts">The maximum number of attempts before giving up.</param>
    /// <returns><see langword="true"/> if the write succeeded within <paramref name="maxAttempts"/>; otherwise <see langword="false"/>.</returns>
    public static bool TryCopyToClipboard(string text, Action<string>? writer = null, int maxAttempts = 5)
    {
        writer ??= static t => Clipboard.SetDataObject(t, true);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                writer(text);
                return true;
            }
            catch (COMException) when (attempt < maxAttempts)
            {
                Thread.Sleep(50);
            }
            catch (COMException)
            {
                return false;
            }
        }

        return false;
    }

    internal void ShowCopyConfirmation()
    {
        if (_copyHexButton is null)
        {
            return;
        }

        _copyFeedbackTimer?.Stop();

        _copyHexButtonOriginalToolTip ??= _copyHexButton.ToolTip;
        _copyHexButton.ToolTip = new ToolTip { Content = CopyHexConfirmationText, PlacementTarget = _copyHexButton, IsOpen = true, };

        _copyFeedbackTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.2), };
        _copyFeedbackTimer.Tick -= OnCopyFeedbackElapsed;
        _copyFeedbackTimer.Tick += OnCopyFeedbackElapsed;
        _copyFeedbackTimer.Start();
    }

    private void OnCopyFeedbackElapsed(object? sender, EventArgs e)
    {
        _copyFeedbackTimer?.Stop();

        if (_copyHexButton is null)
        {
            return;
        }

        if (_copyHexButton.ToolTip is ToolTip openToolTip)
        {
            openToolTip.IsOpen = false;
        }

        _copyHexButton.ToolTip = _copyHexButtonOriginalToolTip;
    }

    private void OnPreviousColorClick(object sender, MouseButtonEventArgs e)
    {
        if (PreviousColor.HasValue)
        {
            SetCurrentValue(ColorProperty, PreviousColor.Value);
        }
    }

    private void OnPaletteButtonClick(object sender, RoutedEventArgs e)
    {
        // Find the Button — check both OriginalSource and Source since OriginalSource
        // may be a child element inside the Button's ControlTemplate.
        var button = e.OriginalSource as System.Windows.Controls.Button ?? e.Source as System.Windows.Controls.Button;
        if (button is null)
        {
            return;
        }

        // Walk up the visual tree to confirm this button is inside a palette/recent ItemsControl
        var parent = VisualTreeHelper.GetParent(button);
        while (parent != null)
        {
            if (parent is ItemsControl ic && (ic.Name == PartColorPalette || ic.Name == PartRecentColors))
            {
                // The DataContext is the Color item from PaletteColors / RecentColors
                if (button.DataContext is Color color)
                {
                    SetCurrentValue(ColorProperty, color);
                    AddRecentColor(color);
                }

                return;
            }

            parent = VisualTreeHelper.GetParent(parent);
        }
    }

    #endregion

    #region Color Sync Logic

    /// <summary>
    /// Forces a re-sync of all visual elements from the current <see cref="Color"/> value.
    /// Useful after the template is first applied (e.g., when hosted inside a Popup).
    /// </summary>
    public void RefreshFromCurrentColor()
    {
        _isUpdating = false;
        SyncFromColor(Color);
    }

    private void UpdateColorFromHsv()
    {
        var (r, g, b) = ColorExtensions.FromHsvToRgb((float)_hue, (float)_saturation, (float)_value);
        SetCurrentValue(ColorProperty, Color.FromArgb(Color.A, ClampByte(r), ClampByte(g), ClampByte(b)));
    }

    private void SyncFromColor(Color color)
    {
        var wasUpdating = _isUpdating;
        _isUpdating = true;
        try
        {
            // Update HSV state
            var (h, s, v) = color.ToHsv();

            // Preserve hue when saturation or value is 0 (achromatic)
            if (s > 0.01f && v > 0.01f)
            {
                _hue = h;
            }
            _saturation = s;
            _value = v;

            // Template children are internal to this control with no external bindings.
            // Direct property assignment (SetValue) is correct here — SetCurrentValue does
            // not reliably override template-set property values on child elements.
#pragma warning disable WPF0041

            // Skip spectrum update when the spectrum itself initiated the change
            // to avoid floating-point round-trip jitter (HSV → RGB bytes → HSV).
            if (!_spectrumInitiated && _spectrum != null)
            {
                _spectrum.Hue = _hue;
                _spectrum.Saturation = _saturation;
                _spectrum.Value = _value;
            }

            // Update value slider
            if (_valueSlider != null)
            {
                _valueSlider.Value = _value;

                // Reads the slider's own Minimum/Maximum-coerced value back into the HSV state.
                _value = _valueSlider.Value;

                // Update slider gradient: left = black, right = full saturation color at hue
                var (fr, fg, fb) = ColorExtensions.FromHsvToRgb((float)_hue, (float)_saturation, 100f);
                _valueSlider.LeftColor = Colors.Black;
                _valueSlider.RightColor = Color.FromRgb(ClampByte(fr), ClampByte(fg), ClampByte(fb));
            }

            // Update alpha slider
            if (_alphaSlider != null)
            {
                _alphaSlider.Value = color.A;
                _alphaSlider.LeftColor = Color.FromArgb(0, color.R, color.G, color.B);
                _alphaSlider.RightColor = Color.FromArgb(255, color.R, color.G, color.B);
            }

            // Update hex text box
            if (_hexTextBox != null)
            {
                _hexTextBox.Text = IsAlphaEnabled
                    ? $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}"
                    : $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }

            // PART_ColorPreview: NOT set here — the template binding
            // {Binding Color, RelativeSource={RelativeSource TemplatedParent}}
            // automatically updates the preview brush when Color changes.

            // Update RGB inputs
            if (_redInput != null) { _redInput.Value = color.R; }
            if (_greenInput != null) { _greenInput.Value = color.G; }
            if (_blueInput != null) { _blueInput.Value = color.B; }

            // Update HSV inputs
            if (_hueInput != null) { _hueInput.Value = Math.Round(_hue); }
            if (_saturationInput != null) { _saturationInput.Value = Math.Round(_saturation); }
            if (_valueInput != null) { _valueInput.Value = Math.Round(_value); }

            // Update alpha input
            if (_alphaInput != null) { _alphaInput.Value = color.A; }

#pragma warning restore WPF0041
        }
        finally
        {
            _isUpdating = wasUpdating;
        }
    }

    private void ApplyHexInput()
    {
        if (_hexTextBox == null) return;

        var hex = _hexTextBox.Text?.Trim() ?? string.Empty;
        if (!hex.StartsWith('#'))
        {
            hex = "#" + hex;
        }

        if (TryParseHex(hex, out var color))
        {
            SetCurrentValue(ColorProperty, color);
        }
        else
        {
            // Revert to current color display
            SyncFromColor(Color);
        }
    }

    private bool TryParseHex(string hex, out Color color)
    {
        color = Colors.White;
        hex = hex.TrimStart('#');

        try
        {
            if (hex.Length == 6)
            {
                var r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                var g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                var b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                color = Color.FromRgb(r, g, b);
                return true;
            }

            if (hex.Length == 8 && IsAlphaEnabled)
            {
                var a = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                var r = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                var g = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                var b = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
                color = Color.FromArgb(a, r, g, b);
                return true;
            }
        }
        catch (FormatException)
        {
            // Invalid hex
        }

        return false;
    }

    /// <summary>
    /// Adds a color to the recent colors collection.
    /// </summary>
    internal void AddRecentColor(Color color)
    {
        // Remove if already present
        RecentColors.Remove(color);

        // Insert at beginning
        RecentColors.Insert(0, color);

        // Trim to max
        while (RecentColors.Count > MaxRecentColors)
        {
            RecentColors.RemoveAt(RecentColors.Count - 1);
        }
    }

    #endregion

    #region DP Change Callbacks

    private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ColorPicker picker) return;

        var oldColor = (Color)e.OldValue;
        var newColor = (Color)e.NewValue;

        picker.SyncFromColor(newColor);
        picker.RaiseEvent(new ColorChangedEventArgs(ColorChangedEvent, picker, oldColor, newColor));
    }

    private static void OnIsAlphaEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ColorPicker picker)
        {
            return;
        }

        picker.SyncFromColor(picker.Color);
    }

    private static void OnPreviousColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ColorPicker picker || picker._previousColorPreview is null)
        {
            return;
        }

#pragma warning disable WPF0041
        picker._previousColorPreview.Background = e.NewValue is Color c
            ? new SolidColorBrush(c)
            : new SolidColorBrush(Colors.Transparent);
#pragma warning restore WPF0041
    }

    #endregion

    private static byte ClampByte(int value) => (byte)Math.Clamp(value, 0, 255);
}

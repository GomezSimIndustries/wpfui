// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Extensions;
using Wpf.Ui.UnitTests.TestHelpers;
using ColorPickerControl = Wpf.Ui.Controls.ColorPicker;

namespace Wpf.Ui.UnitTests.Controls;

public class ColorPickerValueRangeTests
{
    /// <summary>Creates a picker carrying the packaged style, as a real host would resolve it.</summary>
    private static ColorPickerControl CreateStyledPicker() =>
        new() { Style = (Style)Application.Current.FindResource(typeof(ColorPickerControl)) };

    [Fact]
    public void ValueSliderAndInput_DefaultToZeroToHundred_WhenRangeIsNotConfigured()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.ApplyTemplate();

            var slider = (Wpf.Ui.Controls.ColorSlider)picker.Template.FindName("PART_ValueSlider", picker);
            var valueInput = (Wpf.Ui.Controls.NumberBox)picker.Template.FindName("PART_ValueInput", picker);

            Assert.Equal(0, slider.Minimum);
            Assert.Equal(100, slider.Maximum);
            Assert.Equal(0, valueInput.Minimum);
            Assert.Equal(100, valueInput.Maximum);
        });
    }

    [Fact]
    public void ValueSliderAndInput_UseConfiguredMinAndMaxValue()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.MinValue = 20;
            picker.MaxValue = 80;
            picker.ApplyTemplate();

            var slider = (Wpf.Ui.Controls.ColorSlider)picker.Template.FindName("PART_ValueSlider", picker);
            var valueInput = (Wpf.Ui.Controls.NumberBox)picker.Template.FindName("PART_ValueInput", picker);

            Assert.Equal(20, slider.Minimum);
            Assert.Equal(80, slider.Maximum);
            Assert.Equal(20, valueInput.Minimum);
            Assert.Equal(80, valueInput.Maximum);
        });
    }

    [Fact]
    public void ValueSliderAndInput_ClampIntoRange_WhenColorIsSetProgrammaticallyAboveMaxValue()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.MaxValue = 80;
            picker.Color = Colors.Black;
            picker.ApplyTemplate();

            picker.Color = Colors.White;

            var slider = (Wpf.Ui.Controls.ColorSlider)picker.Template.FindName("PART_ValueSlider", picker);
            var valueInput = (Wpf.Ui.Controls.NumberBox)picker.Template.FindName("PART_ValueInput", picker);

            Assert.Equal(80, slider.Value);
            Assert.Equal(80, valueInput.Value);
        });
    }

    [Fact]
    public void SpectrumHueChange_UsesRangeClampedValue_AfterProgrammaticColorAboveMaxValue()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.MaxValue = 80;
            picker.Color = Colors.Black;
            picker.ApplyTemplate();

            picker.Color = Colors.White;

            var spectrum = (Wpf.Ui.Controls.ColorSpectrum)picker.Template.FindName("PART_ColorSpectrum", picker);
            spectrum.SetCurrentValue(Wpf.Ui.Controls.ColorSpectrum.HueProperty, 200.0);

            var (_, _, v) = picker.Color.ToHsv();
            Assert.Equal(80f, v, 3);
        });
    }

    [Fact]
    public void TypedValue_ClampsColorIntoRange_WhenAboveMaxValue()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.MaxValue = 80;
            picker.ApplyTemplate();

            var valueInput = (Wpf.Ui.Controls.NumberBox)picker.Template.FindName("PART_ValueInput", picker);

            valueInput.Value = 40;
            var (_, _, midV) = picker.Color.ToHsv();
            Assert.Equal(40f, midV, 3);

            valueInput.Value = 100;
            var (_, _, clampedV) = picker.Color.ToHsv();
            Assert.Equal(80f, clampedV, 3);
        });
    }

    [Fact]
    public void DraggedValue_ClampsColorIntoRange_WhenAboveMaxValue()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.MaxValue = 80;
            picker.ApplyTemplate();

            var slider = (Wpf.Ui.Controls.ColorSlider)picker.Template.FindName("PART_ValueSlider", picker);

            slider.Value = 40;
            var (_, _, midV) = picker.Color.ToHsv();
            Assert.Equal(40f, midV, 3);

            slider.Value = 100;
            var (_, _, clampedV) = picker.Color.ToHsv();
            Assert.Equal(80f, clampedV, 3);
        });
    }

    [Fact]
    public void MinValueGreaterThanMaxValue_DoesNotThrow()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.MinValue = 80;
            picker.MaxValue = 20;

            var exception = Record.Exception(() => picker.ApplyTemplate());

            Assert.Null(exception);
        });
    }

    [Fact]
    public void DefaultRange_RetainsRectangularZeroToHundredBehavior()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.ApplyTemplate();

            picker.Color = Colors.White;

            var slider = (Wpf.Ui.Controls.ColorSlider)picker.Template.FindName("PART_ValueSlider", picker);
            var valueInput = (Wpf.Ui.Controls.NumberBox)picker.Template.FindName("PART_ValueInput", picker);

            Assert.Equal(100, slider.Value);
            Assert.Equal(100, valueInput.Value);
        });
    }

    [Fact]
    public void ColorSpectrumShape_HasNoEffect_SpectrumGeometryIsIdenticalRegardlessOfShape()
    {
        WpfTestHost.Invoke(() =>
        {
            var boxPicker = CreateStyledPicker();
            boxPicker.Width = 280;
            boxPicker.ApplyTemplate();
            boxPicker.Measure(new Size(280, double.PositiveInfinity));
            boxPicker.Arrange(new Rect(new Point(0, 0), boxPicker.DesiredSize));

            var ringPicker = CreateStyledPicker();
            ringPicker.Width = 280;
#pragma warning disable CS0618 // ColorSpectrumShape is a compatibly deprecated no-op; exercised deliberately here.
            ringPicker.ColorSpectrumShape = Wpf.Ui.Controls.ColorSpectrumShape.Ring;
#pragma warning restore CS0618
            ringPicker.ApplyTemplate();
            ringPicker.Measure(new Size(280, double.PositiveInfinity));
            ringPicker.Arrange(new Rect(new Point(0, 0), ringPicker.DesiredSize));

            var boxSpectrum = (Wpf.Ui.Controls.ColorSpectrum)boxPicker.Template.FindName("PART_ColorSpectrum", boxPicker);
            var ringSpectrum = (Wpf.Ui.Controls.ColorSpectrum)ringPicker.Template.FindName("PART_ColorSpectrum", ringPicker);

            Assert.Equal(boxSpectrum.RenderSize, ringSpectrum.RenderSize);
        });
    }
}

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.UnitTests.TestHelpers;
using ColorPickerControl = Wpf.Ui.Controls.ColorPicker;

namespace Wpf.Ui.UnitTests.Controls;

public class ColorPickerAlphaHexRefreshTests
{
    /// <summary>Creates a picker carrying the packaged style, as a real host would resolve it.</summary>
    private static ColorPickerControl CreateStyledPicker() =>
        new() { Style = (Style)Application.Current.FindResource(typeof(ColorPickerControl)) };

    [Fact]
    public void HexText_SwitchesToEightDigits_WhenAlphaIsEnabledWithoutAnotherColorChange()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.ApplyTemplate();
            picker.Color = Color.FromArgb(128, 10, 20, 30);

            var hexBox = (TextBox)picker.Template.FindName("PART_HexTextBox", picker);
            Assert.Equal("#0A141E", hexBox.Text);

            picker.IsAlphaEnabled = true;

            Assert.Equal("#800A141E", hexBox.Text);
        });
    }

    [Fact]
    public void HexText_SwitchesToSixDigits_WhenAlphaIsDisabledWithoutAnotherColorChange()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.IsAlphaEnabled = true;
            picker.ApplyTemplate();
            picker.Color = Color.FromArgb(128, 10, 20, 30);

            var hexBox = (TextBox)picker.Template.FindName("PART_HexTextBox", picker);
            Assert.Equal("#800A141E", hexBox.Text);

            picker.IsAlphaEnabled = false;

            Assert.Equal("#0A141E", hexBox.Text);
        });
    }

    [Fact]
    public void Color_KeepsHiddenAlpha_WhenTogglingAlphaSupportBackAndForth()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.IsAlphaEnabled = true;
            picker.ApplyTemplate();
            picker.Color = Color.FromArgb(128, 10, 20, 30);

            picker.IsAlphaEnabled = false;
            Assert.Equal((byte)128, picker.Color.A);

            picker.IsAlphaEnabled = true;
            Assert.Equal((byte)128, picker.Color.A);

            var hexBox = (TextBox)picker.Template.FindName("PART_HexTextBox", picker);
            Assert.Equal("#800A141E", hexBox.Text);
        });
    }

    [Fact]
    public void HsvChannelEdit_PreservesHiddenAlpha_WhenAlphaSupportIsDisabled()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.IsAlphaEnabled = true;
            picker.ApplyTemplate();
            picker.Color = Color.FromArgb(128, 10, 20, 30);

            picker.IsAlphaEnabled = false;

            var hueInput = (Wpf.Ui.Controls.NumberBox)picker.Template.FindName("PART_HueInput", picker);
            hueInput.Value = 200;

            Assert.Equal((byte)128, picker.Color.A);
        });
    }
}

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.UnitTests.TestHelpers;
using ColorPickerControl = Wpf.Ui.Controls.ColorPicker;

namespace Wpf.Ui.UnitTests.Controls;

public class ColorPickerLocalizableChromeTests
{
    /// <summary>Creates a picker carrying the packaged style, as a real host would resolve it.</summary>
    private static ColorPickerControl CreateStyledPicker() =>
        new() { Style = (Style)Application.Current.FindResource(typeof(ColorPickerControl)) };

    [Fact]
    public void DefaultLocalizableText_MatchesOriginalEnglishChrome()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new ColorPickerControl();

            Assert.Equal("Value", picker.ValueSliderLabelText);
            Assert.Equal("Opacity", picker.OpacitySliderLabelText);
            Assert.Equal("Hex", picker.HexInputLabelText);
            Assert.Equal("Copy hex value", picker.CopyHexButtonToolTip);
            Assert.Equal("Copied!", picker.CopyHexConfirmationText);
            Assert.Equal("Click to revert to previous color", picker.RevertToPreviousColorToolTip);
            Assert.Equal("Colors", picker.PaletteSectionHeaderText);
            Assert.Equal("Recent", picker.RecentColorsSectionHeaderText);
        });
    }

    [Fact]
    public void ValueSliderLabelText_FlowsToTheTemplatedTextBlock()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.ValueSliderLabelText = "Valeur";
            picker.ApplyTemplate();

            var label = (TextBlock)picker.Template.FindName("PART_ValueSliderLabelText", picker);

            Assert.Equal("Valeur", label.Text);
        });
    }

    [Fact]
    public void OpacitySliderLabelText_FlowsToTheTemplatedTextBlock()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.OpacitySliderLabelText = "Opacité";
            picker.ApplyTemplate();

            var label = (TextBlock)picker.Template.FindName("PART_OpacitySliderLabelText", picker);

            Assert.Equal("Opacité", label.Text);
        });
    }

    [Fact]
    public void HexInputLabelText_FlowsToTheTemplatedTextBlock()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.HexInputLabelText = "Hexa";
            picker.ApplyTemplate();

            var label = (TextBlock)picker.Template.FindName("PART_HexInputLabelText", picker);

            Assert.Equal("Hexa", label.Text);
        });
    }

    [Fact]
    public void PaletteSectionHeaderText_FlowsToTheTemplatedTextBlock()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.PaletteSectionHeaderText = "Couleurs";
            picker.ApplyTemplate();

            var label = (TextBlock)picker.Template.FindName("PART_PaletteSectionHeaderText", picker);

            Assert.Equal("Couleurs", label.Text);
        });
    }

    [Fact]
    public void RecentColorsSectionHeaderText_FlowsToTheTemplatedTextBlock()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.RecentColorsSectionHeaderText = "Récents";
            picker.ApplyTemplate();

            var label = (TextBlock)picker.Template.FindName("PART_RecentColorsSectionHeaderText", picker);

            Assert.Equal("Récents", label.Text);
        });
    }

    [Fact]
    public void RevertToPreviousColorToolTip_FlowsToTheTemplatedPreviewBorder()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.RevertToPreviousColorToolTip = "Cliquez pour revenir à la couleur précédente";
            picker.ApplyTemplate();

            var preview = (Border)picker.Template.FindName("PART_PreviousColorPreview", picker);

            Assert.Equal("Cliquez pour revenir à la couleur précédente", preview.ToolTip);
        });
    }

    [Fact]
    public void CopyHexButtonToolTip_FlowsToTheTemplatedCopyButton()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.CopyHexButtonToolTip = "Copier la valeur hexadécimale";
            picker.ApplyTemplate();

            var button = (Button)picker.Template.FindName("PART_CopyHexButton", picker);

            Assert.Equal("Copier la valeur hexadécimale", button.ToolTip);
        });
    }

    [Fact]
    public void CopyHexConfirmationText_ReplacesTheCopyButtonToolTipContent()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = CreateStyledPicker();
            picker.CopyHexConfirmationText = "Copié !";
            picker.ApplyTemplate();

            picker.ShowCopyConfirmation();

            var button = (Button)picker.Template.FindName("PART_CopyHexButton", picker);
            var confirmation = Assert.IsType<ToolTip>(button.ToolTip);
            Assert.Equal("Copié !", confirmation.Content);
        });
    }
}

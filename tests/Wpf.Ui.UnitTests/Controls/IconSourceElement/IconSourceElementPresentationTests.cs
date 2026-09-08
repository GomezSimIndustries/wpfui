// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Wpf.Ui.Converters;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class IconSourceElementPresentationTests
{
    [Fact]
    public void Measure_WithFontIconSource_DoesNotThrowAndProducesNonZeroDesiredSize()
    {
        WpfTestHost.Invoke(() =>
        {
            var element = new IconSourceElement { IconSource = new FontIconSource { Glyph = "" } };

            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.True(element.DesiredSize.Width > 0);
        });
    }

    [Fact]
    public void Measure_WithNullSource_DoesNotThrow()
    {
        WpfTestHost.Invoke(() =>
        {
            var element = new IconSourceElement();

            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            Assert.Equal(new Size(0, 0), element.DesiredSize);
        });
    }

    [Fact]
    public void IconSource_ReplacedAfterMeasure_PresentsTheNewSourcesGlyph()
    {
        WpfTestHost.Invoke(() =>
        {
            var element = new IconSourceElement { IconSource = new FontIconSource { Glyph = "" } };
            element.Measure(new Size(100, 100));

            element.IconSource = new FontIconSource { Glyph = "" };

            Assert.Equal("", PresentedFontIcon(element).Glyph);
        });
    }

    [Fact]
    public void IconSource_SharedAcrossTwoHosts_EachHostGetsItsOwnVisualInstance()
    {
        WpfTestHost.Invoke(() =>
        {
            var source = new FontIconSource { Glyph = "" };
            var hostA = new IconSourceElement { IconSource = source };
            var hostB = new IconSourceElement { IconSource = source };

            hostA.Measure(new Size(100, 100));
            hostB.Measure(new Size(100, 100));

            Assert.NotSame(PresentedFontIcon(hostA), PresentedFontIcon(hostB));
        });
    }

    [Fact]
    public void FontIconSource_GlyphChangedAfterMeasure_UpdatesThePresentedGlyph()
    {
        WpfTestHost.Invoke(() =>
        {
            var source = new FontIconSource { Glyph = "" };
            var element = new IconSourceElement { IconSource = source };
            element.Measure(new Size(100, 100));

            source.Glyph = "";

            Assert.Equal("", PresentedFontIcon(element).Glyph);
        });
    }

    [Fact]
    public void FontIconSource_ForegroundChangedAfterMeasure_UpdatesThePresentedForeground()
    {
        WpfTestHost.Invoke(() =>
        {
            var source = new FontIconSource { Glyph = "" };
            var element = new IconSourceElement { IconSource = source };
            element.Measure(new Size(100, 100));

            source.Foreground = Brushes.Red;

            Assert.Equal(Brushes.Red, PresentedFontIcon(element).Foreground);
        });
    }

    [Fact]
    public void SymbolIconSource_FilledChangedAfterMeasure_UpdatesThePresentedFontFamily()
    {
        WpfTestHost.Invoke(() =>
        {
            var source = new SymbolIconSource { Symbol = SymbolRegular.Home24 };
            var element = new IconSourceElement { IconSource = source };
            element.Measure(new Size(100, 100));

            source.Filled = true;

            var presented = (SymbolIcon)PresentedFontIcon(element);
            Assert.True(presented.Filled);
        });
    }

    [Fact]
    public void FontIconSource_MutatedAfterSourceReplaced_NoLongerRefreshesTheHost()
    {
        WpfTestHost.Invoke(() =>
        {
            var originalSource = new FontIconSource { Glyph = "" };
            var element = new IconSourceElement { IconSource = originalSource };
            element.Measure(new Size(100, 100));

            element.IconSource = new FontIconSource { Glyph = "" };
            var presentedAfterReplace = PresentedFontIcon(element);

            originalSource.Glyph = "";

            Assert.Same(presentedAfterReplace, PresentedFontIcon(element));
            Assert.Equal("", PresentedFontIcon(element).Glyph);
        });
    }

    [Fact]
    public void Coerce_WithIconSourceElement_StillProducesTheIconElement()
    {
        WpfTestHost.Invoke(() =>
        {
            var sourceElement = new IconSourceElement { IconSource = new SymbolIconSource { Symbol = SymbolRegular.Settings24 } };

            var coerced = IconElement.Coerce(new DependencyObject(), sourceElement);

            Assert.IsType<SymbolIcon>(coerced);
        });
    }

    [Fact]
    public void IconSourceElementConverter_ConvertsToTheIconElement()
    {
        WpfTestHost.Invoke(() =>
        {
            var converter = new IconSourceElementConverter();
            var sourceElement = new IconSourceElement { IconSource = new FontIconSource { Glyph = "" } };

            var converted = converter.Convert(sourceElement, typeof(IconElement), null, CultureInfo.InvariantCulture);

            Assert.IsType<FontIcon>(converted);
        });
    }

    private static FontIcon PresentedFontIcon(IconSourceElement element)
    {
        var layoutRoot = (Grid)VisualTreeHelper.GetChild(element, 0);
        return (FontIcon)VisualTreeHelper.GetChild(layoutRoot, 0);
    }
}

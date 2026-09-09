// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class ResponsiveGridBreakpointContractTests
{
    [Theory]
    [InlineData(255.0, Breakpoint.Xs)]
    [InlineData(256.0, Breakpoint.Sm)]
    [InlineData(257.0, Breakpoint.Sm)]
    [InlineData(511.0, Breakpoint.Sm)]
    [InlineData(512.0, Breakpoint.Md)]
    [InlineData(513.0, Breakpoint.Md)]
    [InlineData(767.0, Breakpoint.Md)]
    [InlineData(768.0, Breakpoint.Lg)]
    [InlineData(769.0, Breakpoint.Lg)]
    public void DefaultReferenceWidth_MeasureAtWidth_SelectsBreakpointFromDefaultThresholds(double width, Breakpoint expected)
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid();

            grid.Measure(new Size(width, 1000));

            Assert.Equal(expected, grid.CurrentBreakpoint);
        });
    }

    [Fact]
    public void DefaultReferenceWidth_MatchesDocumentedConstant()
    {
        Assert.Equal(1024.0, ResponsiveGrid.DefaultReferenceWidth);
    }

    [Fact]
    public void WidthChangingOnSameInstance_RecalculatesBreakpointRatherThanCaching()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid();

            grid.Measure(new Size(100, 1000));
            Assert.Equal(Breakpoint.Xs, grid.CurrentBreakpoint);

            grid.Measure(new Size(900, 1000));
            Assert.Equal(Breakpoint.Lg, grid.CurrentBreakpoint);

            grid.Measure(new Size(100, 1000));
            Assert.Equal(Breakpoint.Xs, grid.CurrentBreakpoint);
        });
    }

    [Theory]
    [InlineData(124.0, Breakpoint.Xs)]
    [InlineData(125.0, Breakpoint.Sm)]
    [InlineData(250.0, Breakpoint.Md)]
    [InlineData(375.0, Breakpoint.Lg)]
    public void ExplicitReferenceWidth_ScalesThresholdsAgainstThatValueInsteadOfDefault(double width, Breakpoint expected)
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { ReferenceWidth = 500 };

            grid.Measure(new Size(width, 1000));

            Assert.Equal(expected, grid.CurrentBreakpoint);
        });
    }

    [Fact]
    public void MutatingLgThresholdOnLiveInstance_ChangesBreakpointOnNextMeasure()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid();

            grid.Measure(new Size(768, 1000));
            Assert.Equal(Breakpoint.Lg, grid.CurrentBreakpoint);

            grid.LgThreshold = 0.9;
            grid.Measure(new Size(768, 1000));

            Assert.Equal(Breakpoint.Md, grid.CurrentBreakpoint);
        });
    }

    [Fact]
    public void MutatingReferenceWidthOnLiveInstance_ChangesBreakpointOnNextMeasure()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid();

            grid.Measure(new Size(1024, 1000));
            Assert.Equal(Breakpoint.Lg, grid.CurrentBreakpoint);

            grid.ReferenceWidth = 2048;
            grid.Measure(new Size(1024, 1000));

            Assert.Equal(Breakpoint.Md, grid.CurrentBreakpoint);
        });
    }

    [Fact]
    public void ZeroReferenceWidth_ExplicitlySet_ReproducesPreFixAlwaysLargeBehavior()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { ReferenceWidth = 0 };

            grid.Measure(new Size(1, 1000));
            Assert.Equal(Breakpoint.Lg, grid.CurrentBreakpoint);

            grid.Measure(new Size(50000, 1000));
            Assert.Equal(Breakpoint.Lg, grid.CurrentBreakpoint);
        });
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void ReferenceWidth_InvalidValue_CoercesToZero(double invalidReferenceWidth)
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { ReferenceWidth = invalidReferenceWidth };

            Assert.Equal(0.0, grid.ReferenceWidth);
        });
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    public void LgThreshold_InvalidValue_ClampsToUnitRange(double invalidThreshold)
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { LgThreshold = invalidThreshold };

            Assert.InRange(grid.LgThreshold, 0.0, 1.0);
        });
    }

    [Fact]
    public void DefaultReferenceWidth_SpanFallsBackToMdWhenLgUnset_ArrangesChildAtMdSpanWidth()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 12, Gutter = 0 };
            var child = new Border { Height = 20 };
            ResponsiveGrid.SetXs(child, 12);
            ResponsiveGrid.SetMd(child, 6);
            grid.Children.Add(child);

            grid.Measure(new Size(768, 1000));
            Assert.Equal(Breakpoint.Lg, grid.CurrentBreakpoint);

            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(384, child.RenderSize.Width);
        });
    }

    [Fact]
    public void DefaultReferenceWidth_TwoChildrenAtMdFallbackSpan_WrapToSecondRow()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 12, Gutter = 0 };
            var first = new Border { Height = 20 };
            var second = new Border { Height = 30 };
            ResponsiveGrid.SetXs(first, 12);
            ResponsiveGrid.SetMd(first, 6);
            ResponsiveGrid.SetXs(second, 12);
            ResponsiveGrid.SetMd(second, 8);
            grid.Children.Add(first);
            grid.Children.Add(second);

            grid.Measure(new Size(768, 1000));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(new Vector(0, 0), VisualTreeHelper.GetOffset(first));
            Assert.Equal(new Vector(0, 20), VisualTreeHelper.GetOffset(second));
        });
    }

    [Theory]
    [InlineData(1600, 3)]
    [InlineData(1200, 3)]
    [InlineData(1199, 6)]
    [InlineData(800, 6)]
    [InlineData(799, 9)]
    [InlineData(400, 9)]
    [InlineData(399, 12)]
    public void ExplicitReferenceWidth_FixedSpanNarrowsThroughEveryBreakpoint(double gridWidth, int expectedSpanColumns)
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 12, Gutter = 0, ReferenceWidth = 1600 };
            var child = new Border();
            ResponsiveGrid.SetXs(child, 12);
            ResponsiveGrid.SetSm(child, 9);
            ResponsiveGrid.SetMd(child, 6);
            ResponsiveGrid.SetLg(child, 3);
            grid.Children.Add(child);

            grid.Measure(new Size(gridWidth, double.PositiveInfinity));
            grid.Arrange(new Rect(0, 0, gridWidth, grid.DesiredSize.Height));

            Assert.Equal(gridWidth / 12 * expectedSpanColumns, child.ActualWidth, 1);
        });
    }

    [Fact]
    public void SmUnset_FallsBackToXsAtTheSmBreakpointRatherThanMd()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 12, Gutter = 0, ReferenceWidth = 1600 };
            var child = new Border();
            ResponsiveGrid.SetXs(child, 12);
            ResponsiveGrid.SetMd(child, 6);
            grid.Children.Add(child);

            const double smZoneWidth = 600;
            grid.Measure(new Size(smZoneWidth, double.PositiveInfinity));
            grid.Arrange(new Rect(0, 0, smZoneWidth, grid.DesiredSize.Height));

            Assert.Equal(600d, child.ActualWidth, 1);
        });
    }

    [Fact]
    public void SmSet_OverridesTheXsFallbackAtTheSmBreakpoint()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 12, Gutter = 0, ReferenceWidth = 1600 };
            var child = new Border();
            ResponsiveGrid.SetXs(child, 12);
            ResponsiveGrid.SetSm(child, 6);
            ResponsiveGrid.SetMd(child, 6);
            grid.Children.Add(child);

            const double smZoneWidth = 600;
            grid.Measure(new Size(smZoneWidth, double.PositiveInfinity));
            grid.Arrange(new Rect(0, 0, smZoneWidth, grid.DesiredSize.Height));

            Assert.Equal(300d, child.ActualWidth, 1);
        });
    }
}

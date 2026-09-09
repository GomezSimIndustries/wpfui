// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class ResponsiveGridMeasureArrangeSafetyTests
{
    [Fact]
    public void InfiniteWidth_SingleVisibleChild_ReportsChildDesiredSizeInsteadOfZero()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid();
            var child = new Border { Width = 160, Height = 30 };
            grid.Children.Add(child);

            grid.Measure(new Size(double.PositiveInfinity, 1000));

            Assert.Equal(160, grid.DesiredSize.Width);
            Assert.Equal(30, grid.DesiredSize.Height);
        });
    }

    [Fact]
    public void InfiniteWidth_MultipleVisibleChildren_SumsWidthsWithGutterAndReportsTallestHeight()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Gutter = 10 };
            var first = new Border { Width = 100, Height = 30 };
            var second = new Border { Width = 50, Height = 40 };
            grid.Children.Add(first);
            grid.Children.Add(second);

            grid.Measure(new Size(double.PositiveInfinity, 1000));

            Assert.Equal(160, grid.DesiredSize.Width);
            Assert.Equal(40, grid.DesiredSize.Height);
        });
    }

    [Fact]
    public void InfiniteWidth_CollapsedAndHiddenChildren_ExcludedFromDesiredSize()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid();
            var visible = new Border { Width = 100, Height = 30 };
            var collapsed = new Border { Width = 200, Height = 30, Visibility = Visibility.Collapsed };
            var hiddenAtXs = new Border { Width = 300, Height = 30 };
            ResponsiveGrid.SetHidden(hiddenAtXs, Breakpoint.Xs);
            grid.Children.Add(visible);
            grid.Children.Add(collapsed);
            grid.Children.Add(hiddenAtXs);

            grid.Measure(new Size(double.PositiveInfinity, 1000));

            Assert.Equal(100, grid.DesiredSize.Width);
            Assert.Equal(30, grid.DesiredSize.Height);
            Assert.Equal(new Size(0, 0), collapsed.DesiredSize);
            Assert.Equal(new Size(0, 0), hiddenAtXs.DesiredSize);
        });
    }

    [Fact]
    public void InfiniteWidth_NoVisibleChildren_ReportsZeroSizeWithoutThrowing()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid();

            grid.Measure(new Size(double.PositiveInfinity, 1000));

            Assert.Equal(new Size(0, 0), grid.DesiredSize);
        });
    }

    [Theory]
    [InlineData(100.0)]
    [InlineData(176.0)]
    [InlineData(300.0)]
    public void NarrowWidthRelativeToAggregateGutters_DoesNotThrowAndProducesNonNegativeSize(double width)
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 12, Gutter = 16 };
            var child = new Border { Height = 30 };
            ResponsiveGrid.SetXs(child, 1);
            grid.Children.Add(child);

            grid.Measure(new Size(width, 1000));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.True(grid.DesiredSize.Width >= 0);
            Assert.True(grid.DesiredSize.Height >= 0);
            Assert.False(double.IsNaN(grid.DesiredSize.Width));
            Assert.False(double.IsNaN(grid.DesiredSize.Height));
        });
    }

    [Fact]
    public void FiniteWidth_MultipleSpansWrapToSecondRow_ArrangesChildrenAtComputedOffsets()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 2, Gutter = 0 };
            var first = new Border { Height = 30 };
            var second = new Border { Height = 50 };
            var third = new Border { Height = 20 };
            ResponsiveGrid.SetXs(first, 1);
            ResponsiveGrid.SetXs(second, 1);
            ResponsiveGrid.SetXs(third, 1);
            grid.Children.Add(first);
            grid.Children.Add(second);
            grid.Children.Add(third);

            grid.Measure(new Size(200, 1000));
            Assert.Equal(new Size(200, 70), grid.DesiredSize);

            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(new Vector(0, 0), VisualTreeHelper.GetOffset(first));
            Assert.Equal(new Vector(100, 0), VisualTreeHelper.GetOffset(second));
            Assert.Equal(new Vector(0, 50), VisualTreeHelper.GetOffset(third));
        });
    }

    [Fact]
    public void UniformRowHeight_MultipleRows_NormalizesEveryRowToTallestRowHeight()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 2, Gutter = 0, UniformRowHeight = true };
            var first = new Border { Height = 30 };
            var second = new Border { Height = 50 };
            var third = new Border { Height = 20 };
            ResponsiveGrid.SetXs(first, 1);
            ResponsiveGrid.SetXs(second, 1);
            ResponsiveGrid.SetXs(third, 1);
            grid.Children.Add(first);
            grid.Children.Add(second);
            grid.Children.Add(third);

            grid.Measure(new Size(200, 1000));

            Assert.Equal(new Size(200, 100), grid.DesiredSize);

            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(new Vector(0, 50), VisualTreeHelper.GetOffset(third));
        });
    }

    [Fact]
    public void FiniteWidth_CollapsedChild_ArrangedToZeroRectAndExcludedFromRowLayout()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 2, Gutter = 0 };
            var collapsed = new Border { Height = 30, Visibility = Visibility.Collapsed };
            var visible = new Border { Height = 40 };
            ResponsiveGrid.SetXs(collapsed, 1);
            ResponsiveGrid.SetXs(visible, 1);
            grid.Children.Add(collapsed);
            grid.Children.Add(visible);

            grid.Measure(new Size(200, 1000));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(new Vector(0, 0), VisualTreeHelper.GetOffset(visible));
            Assert.Equal(40, grid.DesiredSize.Height);
        });
    }

    [Fact]
    public void FiniteWidth_HiddenAtCurrentBreakpoint_ArrangedToZeroRectAndExcludedFromRowLayout()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 2, Gutter = 0, ReferenceWidth = 200 };
            var hidden = new Border { Height = 30 };
            var visible = new Border { Height = 40 };
            ResponsiveGrid.SetXs(hidden, 1);
            ResponsiveGrid.SetXs(visible, 1);
            ResponsiveGrid.SetHidden(hidden, Breakpoint.Lg);
            grid.Children.Add(hidden);
            grid.Children.Add(visible);

            grid.Measure(new Size(200, 1000));

            Assert.Equal(Breakpoint.Lg, grid.CurrentBreakpoint);
            Assert.Equal(new Size(0, 0), hidden.DesiredSize);

            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(new Vector(0, 0), VisualTreeHelper.GetOffset(visible));
            Assert.Equal(40, grid.DesiredSize.Height);
        });
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void Gutter_InvalidValue_CoercesToZero(double invalidGutter)
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Gutter = invalidGutter };

            Assert.Equal(0.0, grid.Gutter);
        });
    }

    [Fact]
    public void Gutter_ValidPositiveValue_IsNotCoerced()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Gutter = 16.0 };

            Assert.Equal(16.0, grid.Gutter);
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void NonPositiveColumns_MeasureResolvesTheColumnCountFromTheWidthWithoutThrowing(int columns)
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = columns };
            var child = new Border { Width = 50, Height = 50 };
            grid.Children.Add(child);

            grid.Measure(new Size(200, 1000));

            Assert.Equal(new Size(200, 50), grid.DesiredSize);
        });
    }

    [Fact]
    public void NegativeAttachedSpan_ClampedToMinimumSpanWithoutThrowing()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 4, Gutter = 0 };
            var child = new Border { Height = 25 };
            ResponsiveGrid.SetXs(child, -5);
            grid.Children.Add(child);

            grid.Measure(new Size(200, 1000));

            Assert.Equal(25, grid.DesiredSize.Height);
            Assert.False(double.IsNaN(grid.DesiredSize.Width));
        });
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    [InlineData(0)]
    [InlineData(-50)]
    public void ResolveColumnCount_WithoutAUsableWidth_FallsBackToOneColumn(double width)
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 0, MinColumnWidth = 320 };

            Assert.Equal(1, grid.ResolveColumnCount(width));
        });
    }

    [Fact]
    public void AutoFit_RowSpacing_SeparatesRowsAsChildrenWrap()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 0, MinColumnWidth = 320, ColumnSpacing = 10, RowSpacing = 10 };
            var children = new Border[4];
            for (int index = 0; index < children.Length; index++)
            {
                children[index] = new Border { Height = 50 };
                grid.Children.Add(children[index]);
            }

            grid.Measure(new Size(1000, 1000));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(new Vector(0, 0), VisualTreeHelper.GetOffset(children[0]));
            Assert.Equal(new Vector(0, 60), VisualTreeHelper.GetOffset(children[3]));
        });
    }
}

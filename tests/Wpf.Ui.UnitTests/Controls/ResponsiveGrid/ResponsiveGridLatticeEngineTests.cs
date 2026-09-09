// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class ResponsiveGridLatticeEngineTests
{
    [Fact]
    public void ArrangeAtADifferentWidthThanMeasure_ResolvesBreakpointAndSpanFromFinalSize()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 12, Gutter = 0, ReferenceWidth = 1000 };
            var child = new Border { MinWidth = 100, MinHeight = 20 };
            ResponsiveGrid.SetXs(child, 12);
            ResponsiveGrid.SetLg(child, 3);
            grid.Children.Add(child);

            grid.Measure(new Size(double.PositiveInfinity, 1000));
            Assert.Equal(Breakpoint.Xs, grid.CurrentBreakpoint);

            grid.Arrange(new Rect(0, 0, 800, 20));

            Assert.Equal(Breakpoint.Lg, grid.CurrentBreakpoint);
            Assert.Equal(200, child.RenderSize.Width);
        });
    }

    [Fact]
    public void ItemVerticalAlignmentChanged_InvalidatesMeasure()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid();

            grid.Measure(new Size(400, 1000));
            Assert.True(grid.IsMeasureValid);

            grid.ItemVerticalAlignment = VerticalAlignment.Stretch;

            Assert.False(grid.IsMeasureValid);
        });
    }

    [Fact]
    public void ColumnsAtZero_ResolvesTheColumnCountFromTheAvailableWidth()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 0, Gutter = 0 };
            var first = new Border { Height = 30 };
            var second = new Border { Height = 40 };
            var third = new Border { Height = 50 };
            grid.Children.Add(first);
            grid.Children.Add(second);
            grid.Children.Add(third);

            grid.Measure(new Size(200, 1000));

            Assert.Equal(new Size(200, 120), grid.DesiredSize);

            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(new Vector(0, 0), VisualTreeHelper.GetOffset(first));
            Assert.Equal(new Vector(0, 30), VisualTreeHelper.GetOffset(second));
            Assert.Equal(new Vector(0, 70), VisualTreeHelper.GetOffset(third));
        });
    }

    [Fact]
    public void FixedMode_RowSpacing_SeparatesRowsAndIsNotDiscardedInFavourOfGutter()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 1, Gutter = 4, RowSpacing = 12 };
            var first = new Border { Height = 30 };
            var second = new Border { Height = 40 };
            ResponsiveGrid.SetXs(first, 1);
            ResponsiveGrid.SetXs(second, 1);
            grid.Children.Add(first);
            grid.Children.Add(second);

            grid.Measure(new Size(200, 1000));

            Assert.Equal(82, grid.DesiredSize.Height);

            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(new Vector(0, 42), VisualTreeHelper.GetOffset(second));
        });
    }

    [Fact]
    public void UnsetSpacings_FallBackToGutterOnBothAxes()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 2, Gutter = 10 };
            var first = new Border { Height = 30 };
            var second = new Border { Height = 40 };
            var third = new Border { Height = 20 };
            ResponsiveGrid.SetXs(first, 1);
            ResponsiveGrid.SetXs(second, 1);
            ResponsiveGrid.SetXs(third, 1);
            grid.Children.Add(first);
            grid.Children.Add(second);
            grid.Children.Add(third);

            Assert.True(double.IsNaN(grid.ColumnSpacing));
            Assert.True(double.IsNaN(grid.RowSpacing));

            grid.Measure(new Size(210, 1000));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(new Vector(110, 0), VisualTreeHelper.GetOffset(second));
            Assert.Equal(new Vector(0, 50), VisualTreeHelper.GetOffset(third));
            Assert.Equal(70, grid.DesiredSize.Height);
        });
    }

    [Fact]
    public void ColumnSpacingSet_TakesPrecedenceOverGutterOnTheHorizontalAxis()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 2, Gutter = 40, ColumnSpacing = 20 };
            var first = new Border { Height = 30 };
            var second = new Border { Height = 30 };
            ResponsiveGrid.SetXs(first, 1);
            ResponsiveGrid.SetXs(second, 1);
            grid.Children.Add(first);
            grid.Children.Add(second);

            grid.Measure(new Size(220, 1000));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(new Vector(120, 0), VisualTreeHelper.GetOffset(second));
            Assert.Equal(100, second.RenderSize.Width);
        });
    }

    [Fact]
    public void PartialRowFollowedByAWiderSpan_StartsTheNextRowAtColumnZero()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 4, Gutter = 10 };
            var first = new Border { Height = 30 };
            var second = new Border { Height = 30 };
            var third = new Border { Height = 30 };
            ResponsiveGrid.SetXs(first, 1);
            ResponsiveGrid.SetXs(second, 3);
            ResponsiveGrid.SetXs(third, 2);
            grid.Children.Add(first);
            grid.Children.Add(second);
            grid.Children.Add(third);

            grid.Measure(new Size(430, 1000));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(new Vector(0, 0), VisualTreeHelper.GetOffset(first));
            Assert.Equal(new Vector(110, 0), VisualTreeHelper.GetOffset(second));
            Assert.Equal(new Vector(0, 40), VisualTreeHelper.GetOffset(third));
            Assert.Equal(210, third.RenderSize.Width);
        });
    }

    [Fact]
    public void AutoFit_MinColumnWidth_DropsTheColumnCountAsTheWidthNarrows()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 0, MinColumnWidth = 100, ColumnSpacing = 0 };

            Assert.Equal(4, grid.ResolveColumnCount(400));
            Assert.Equal(2, grid.ResolveColumnCount(250));
            Assert.Equal(1, grid.ResolveColumnCount(60));
        });
    }

    [Fact]
    public void AutoFit_MaxColumns_CapsTheResolvedCount()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid
            {
                Columns = 0,
                MinColumnWidth = 100,
                ColumnSpacing = 0,
                MaxColumns = 2,
            };

            Assert.Equal(2, grid.ResolveColumnCount(1000));
        });
    }

    [Fact]
    public void AutoFit_ColumnSpanAttachedProperty_TakesConsecutiveColumns()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid
            {
                Columns = 0,
                MinColumnWidth = 100,
                ColumnSpacing = 0,
                RowSpacing = 0,
            };
            var first = new Border { Height = 30 };
            var second = new Border { Height = 30 };
            var spanning = new Border { Height = 40 };
            ResponsiveGrid.SetColumnSpan(spanning, 2);
            grid.Children.Add(first);
            grid.Children.Add(second);
            grid.Children.Add(spanning);

            grid.Measure(new Size(200, 1000));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(new Vector(0, 0), VisualTreeHelper.GetOffset(first));
            Assert.Equal(new Vector(100, 0), VisualTreeHelper.GetOffset(second));
            Assert.Equal(new Vector(0, 30), VisualTreeHelper.GetOffset(spanning));
            Assert.Equal(200, spanning.RenderSize.Width);
        });
    }

    [Fact]
    public void AutoFit_SpanWiderThanTheResolvedCount_ClampsToAFullRow()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid
            {
                Columns = 0,
                MinColumnWidth = 100,
                ColumnSpacing = 0,
            };
            var child = new Border { Height = 30 };
            ResponsiveGrid.SetColumnSpan(child, 9);
            grid.Children.Add(child);

            grid.Measure(new Size(200, 1000));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(200, child.RenderSize.Width);
        });
    }

    [Fact]
    public void AutoFit_UniformRowHeight_NormalisesEveryRowToTheTallestRow()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid
            {
                Columns = 0,
                MinColumnWidth = 100,
                ColumnSpacing = 0,
                RowSpacing = 0,
                UniformRowHeight = true,
            };
            var first = new Border { Height = 30 };
            var second = new Border { Height = 50 };
            var third = new Border { Height = 20 };
            grid.Children.Add(first);
            grid.Children.Add(second);
            grid.Children.Add(third);

            grid.Measure(new Size(200, 1000));

            Assert.Equal(100, grid.DesiredSize.Height);

            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(new Vector(0, 50), VisualTreeHelper.GetOffset(third));
        });
    }

    [Theory]
    [InlineData(VerticalAlignment.Top, 0d, 20d)]
    [InlineData(VerticalAlignment.Center, 15d, 20d)]
    [InlineData(VerticalAlignment.Bottom, 30d, 20d)]
    [InlineData(VerticalAlignment.Stretch, 0d, 50d)]
    public void AutoFit_ItemVerticalAlignment_PlacesTheShorterChildInsideTheRow(
        VerticalAlignment alignment,
        double expectedOffsetY,
        double expectedHeight)
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid
            {
                Columns = 0,
                MinColumnWidth = 100,
                ColumnSpacing = 0,
                RowSpacing = 0,
                ItemVerticalAlignment = alignment,
            };
            var shorter = new Border { MinHeight = 20 };
            var taller = new Border { MinHeight = 50 };
            grid.Children.Add(shorter);
            grid.Children.Add(taller);

            grid.Measure(new Size(200, 1000));
            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(new Vector(0, expectedOffsetY), VisualTreeHelper.GetOffset(shorter));
            Assert.Equal(expectedHeight, shorter.RenderSize.Height);
        });
    }

    [Fact]
    public void AutoFit_CollapsedAndHiddenChildren_TakeNoCellAndAreArrangedToAnEmptyRect()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid
            {
                Columns = 0,
                MinColumnWidth = 100,
                ColumnSpacing = 0,
                RowSpacing = 0,
                ReferenceWidth = 200,
            };
            var collapsed = new Border { Height = 30, Visibility = Visibility.Collapsed };
            var hidden = new Border { Height = 30 };
            var visible = new Border { Height = 40 };
            ResponsiveGrid.SetHidden(hidden, Breakpoint.Lg);
            grid.Children.Add(collapsed);
            grid.Children.Add(hidden);
            grid.Children.Add(visible);

            grid.Measure(new Size(200, 1000));

            Assert.Equal(Breakpoint.Lg, grid.CurrentBreakpoint);
            Assert.Equal(new Size(200, 40), grid.DesiredSize);

            grid.Arrange(new Rect(0, 0, grid.DesiredSize.Width, grid.DesiredSize.Height));

            Assert.Equal(new Vector(0, 0), VisualTreeHelper.GetOffset(visible));
            Assert.Equal(new Size(0, 0), collapsed.DesiredSize);
            Assert.Equal(new Size(0, 0), hidden.DesiredSize);
        });
    }

    [Fact]
    public void PreviouslyArrangedChildHiddenAtTheNewBreakpoint_IsMovedBackToTheOrigin()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 2, Gutter = 0, ReferenceWidth = 200 };
            var first = new Border { Height = 30 };
            var second = new Border { Height = 30 };
            ResponsiveGrid.SetXs(first, 1);
            ResponsiveGrid.SetXs(second, 1);
            ResponsiveGrid.SetHidden(second, Breakpoint.Xs);
            grid.Children.Add(first);
            grid.Children.Add(second);

            grid.Measure(new Size(200, 1000));
            grid.Arrange(new Rect(0, 0, 200, 30));
            Assert.Equal(new Vector(100, 0), VisualTreeHelper.GetOffset(second));

            grid.Measure(new Size(20, 1000));
            grid.Arrange(new Rect(0, 0, 20, 30));

            Assert.Equal(Breakpoint.Xs, grid.CurrentBreakpoint);
            Assert.Equal(new Vector(0, 0), VisualTreeHelper.GetOffset(second));
        });
    }

    [Fact]
    public void FixedMode_WidthNarrowerThanTheColumnsRequire_KeepsAPositiveColumnWidth()
    {
        WpfTestHost.Invoke(() =>
        {
            var grid = new ResponsiveGrid { Columns = 4, Gutter = 20 };
            var child = new Border { Height = 30 };
            ResponsiveGrid.SetXs(child, 1);
            grid.Children.Add(child);

            grid.Measure(new Size(40, 1000));

            Assert.Equal(30, grid.DesiredSize.Height);

            grid.Arrange(new Rect(0, 0, 40, 30));

            Assert.Equal(1, child.RenderSize.Width);
        });
    }
}

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;

namespace Wpf.Ui.Controls;

/// <summary>
/// A responsive grid panel that automatically adjusts layout based on container width.
/// Similar to Bootstrap's grid system, but with percentage-based breakpoints.
/// </summary>
/// <example>
/// <code lang="xml">
/// &lt;ui:ResponsiveGrid Columns="12" Gutter="16"&gt;
///     &lt;!-- Full width on Xs/Sm, half on Md, third on Lg --&gt;
///     &lt;Border ui:ResponsiveGrid.Xs="12"
///             ui:ResponsiveGrid.Md="6"
///             ui:ResponsiveGrid.Lg="4" /&gt;
/// &lt;/ui:ResponsiveGrid&gt;
/// </code>
/// </example>
public class ResponsiveGrid : Panel
{
    /// <summary>Identifies the <see cref="ItemVerticalAlignment"/> dependency property.</summary>
    public static readonly DependencyProperty ItemVerticalAlignmentProperty = DependencyProperty.Register(
        nameof(ItemVerticalAlignment),
        typeof(VerticalAlignment),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(VerticalAlignment.Top, FrameworkPropertyMetadataOptions.AffectsArrange)
    );

    /// <summary>Identifies the <see cref="UniformRowHeight"/> dependency property.</summary>
    public static readonly DependencyProperty UniformRowHeightProperty = DependencyProperty.Register(
        nameof(UniformRowHeight),
        typeof(bool),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure)
    );

    /// <summary>Identifies the <see cref="Columns"/> dependency property.</summary>
    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
        nameof(Columns),
        typeof(int),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(12, FrameworkPropertyMetadataOptions.AffectsMeasure)
    );

    /// <summary>Identifies the <see cref="Gutter"/> dependency property.</summary>
    public static readonly DependencyProperty GutterProperty = DependencyProperty.Register(
        nameof(Gutter),
        typeof(double),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure, null, CoerceGutter)
    );

    /// <summary>Identifies the <see cref="SmThreshold"/> dependency property.</summary>
    public static readonly DependencyProperty SmThresholdProperty = DependencyProperty.Register(
        nameof(SmThreshold),
        typeof(double),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(0.25, FrameworkPropertyMetadataOptions.AffectsMeasure, null, CoerceThreshold)
    );

    /// <summary>Identifies the <see cref="MdThreshold"/> dependency property.</summary>
    public static readonly DependencyProperty MdThresholdProperty = DependencyProperty.Register(
        nameof(MdThreshold),
        typeof(double),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(0.50, FrameworkPropertyMetadataOptions.AffectsMeasure, null, CoerceThreshold)
    );

    /// <summary>Identifies the <see cref="LgThreshold"/> dependency property.</summary>
    public static readonly DependencyProperty LgThresholdProperty = DependencyProperty.Register(
        nameof(LgThreshold),
        typeof(double),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(0.75, FrameworkPropertyMetadataOptions.AffectsMeasure, null, CoerceThreshold)
    );

    /// <summary>Identifies the <see cref="ReferenceWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ReferenceWidthProperty = DependencyProperty.Register(
        nameof(ReferenceWidth),
        typeof(double),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure)
    );

    private static readonly DependencyPropertyKey CurrentBreakpointPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(CurrentBreakpoint),
        typeof(Breakpoint),
        typeof(ResponsiveGrid),
        new PropertyMetadata(Breakpoint.Xs)
    );

    /// <summary>Identifies the <see cref="CurrentBreakpoint"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentBreakpointProperty = CurrentBreakpointPropertyKey.DependencyProperty;

    /// <summary>Identifies the Xs attached property (column span at extra-small breakpoint).</summary>
    public static readonly DependencyProperty XsProperty = DependencyProperty.RegisterAttached(
        "Xs",
        typeof(int),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(12, FrameworkPropertyMetadataOptions.AffectsParentMeasure)
    );

    /// <summary>Identifies the Sm attached property (column span at small breakpoint).</summary>
    public static readonly DependencyProperty SmProperty = DependencyProperty.RegisterAttached(
        "Sm",
        typeof(int?),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsParentMeasure)
    );

    /// <summary>Identifies the Md attached property (column span at medium breakpoint).</summary>
    public static readonly DependencyProperty MdProperty = DependencyProperty.RegisterAttached(
        "Md",
        typeof(int?),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsParentMeasure)
    );

    /// <summary>Identifies the Lg attached property (column span at large breakpoint).</summary>
    public static readonly DependencyProperty LgProperty = DependencyProperty.RegisterAttached(
        "Lg",
        typeof(int?),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsParentMeasure)
    );

    /// <summary>Identifies the Hidden attached property (breakpoints at which to hide the element).</summary>
    public static readonly DependencyProperty HiddenProperty = DependencyProperty.RegisterAttached(
        "Hidden",
        typeof(Breakpoint?),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsParentMeasure)
    );

    /// <summary>Identifies the HiddenBreakpoints attached property (multiple breakpoints at which to hide).</summary>
    public static readonly DependencyProperty HiddenBreakpointsProperty = DependencyProperty.RegisterAttached(
        "HiddenBreakpoints",
        typeof(Breakpoint[]),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsParentMeasure)
    );

    /// <summary>
    /// Gets or sets how child elements are vertically aligned within their row
    /// when the row height exceeds the child's desired height.
    /// Default is <see cref="VerticalAlignment.Top"/>.
    /// </summary>
    [Bindable(true)]
    [Category("Layout")]
    public VerticalAlignment ItemVerticalAlignment
    {
        get => (VerticalAlignment)GetValue(ItemVerticalAlignmentProperty);
        set => SetValue(ItemVerticalAlignmentProperty, value);
    }

    /// <summary>
    /// Gets or sets whether all rows should have the same height, determined by the tallest row.
    /// Default is <see langword="false"/>.
    /// </summary>
    [Bindable(true)]
    [Category("Layout")]
    public bool UniformRowHeight
    {
        get => (bool)GetValue(UniformRowHeightProperty);
        set => SetValue(UniformRowHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the number of columns in the grid. Default is 12.
    /// </summary>
    [Bindable(true)]
    [Category("Layout")]
    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>
    /// Gets or sets the spacing between grid items in pixels. Default is 0.
    /// </summary>
    [Bindable(true)]
    [Category("Layout")]
    public double Gutter
    {
        get => (double)GetValue(GutterProperty);
        set => SetValue(GutterProperty, value);
    }

    /// <summary>
    /// Gets or sets the threshold (0.0 to 1.0) at which the Sm breakpoint activates.
    /// Default is 0.25 (25% of reference width).
    /// </summary>
    [Bindable(true)]
    [Category("Layout")]
    public double SmThreshold
    {
        get => (double)GetValue(SmThresholdProperty);
        set => SetValue(SmThresholdProperty, value);
    }

    /// <summary>
    /// Gets or sets the threshold (0.0 to 1.0) at which the Md breakpoint activates.
    /// Default is 0.50 (50% of reference width).
    /// </summary>
    [Bindable(true)]
    [Category("Layout")]
    public double MdThreshold
    {
        get => (double)GetValue(MdThresholdProperty);
        set => SetValue(MdThresholdProperty, value);
    }

    /// <summary>
    /// Gets or sets the threshold (0.0 to 1.0) at which the Lg breakpoint activates.
    /// Default is 0.75 (75% of reference width).
    /// </summary>
    [Bindable(true)]
    [Category("Layout")]
    public double LgThreshold
    {
        get => (double)GetValue(LgThresholdProperty);
        set => SetValue(LgThresholdProperty, value);
    }

    /// <summary>
    /// Gets or sets the reference width used for breakpoint calculations.
    /// If 0 (default), the panel's available width is used directly without percentage calculation.
    /// If set, breakpoints are calculated as percentages of this value.
    /// </summary>
    [Bindable(true)]
    [Category("Layout")]
    public double ReferenceWidth
    {
        get => (double)GetValue(ReferenceWidthProperty);
        set => SetValue(ReferenceWidthProperty, value);
    }

    /// <summary>
    /// Gets the current active breakpoint based on the container width.
    /// </summary>
    [Browsable(false)]
    public Breakpoint CurrentBreakpoint => (Breakpoint)GetValue(CurrentBreakpointProperty);

    /// <summary>Gets the column span for the Xs (extra-small) breakpoint.</summary>
    public static int GetXs(DependencyObject element) => (int)element.GetValue(XsProperty);

    /// <summary>Sets the column span for the Xs (extra-small) breakpoint.</summary>
    public static void SetXs(DependencyObject element, int value) => element.SetValue(XsProperty, value);

    /// <summary>Gets the column span for the Sm (small) breakpoint.</summary>
    public static int? GetSm(DependencyObject element) => (int?)element.GetValue(SmProperty);

    /// <summary>Sets the column span for the Sm (small) breakpoint.</summary>
    public static void SetSm(DependencyObject element, int? value) => element.SetValue(SmProperty, value);

    /// <summary>Gets the column span for the Md (medium) breakpoint.</summary>
    public static int? GetMd(DependencyObject element) => (int?)element.GetValue(MdProperty);

    /// <summary>Sets the column span for the Md (medium) breakpoint.</summary>
    public static void SetMd(DependencyObject element, int? value) => element.SetValue(MdProperty, value);

    /// <summary>Gets the column span for the Lg (large) breakpoint.</summary>
    public static int? GetLg(DependencyObject element) => (int?)element.GetValue(LgProperty);

    /// <summary>Sets the column span for the Lg (large) breakpoint.</summary>
    public static void SetLg(DependencyObject element, int? value) => element.SetValue(LgProperty, value);

    /// <summary>Gets the single breakpoint at which to hide the element.</summary>
    public static Breakpoint? GetHidden(DependencyObject element) => (Breakpoint?)element.GetValue(HiddenProperty);

    /// <summary>Sets the single breakpoint at which to hide the element.</summary>
    public static void SetHidden(DependencyObject element, Breakpoint? value) => element.SetValue(HiddenProperty, value);

    /// <summary>Gets the breakpoints at which to hide the element.</summary>
    public static Breakpoint[]? GetHiddenBreakpoints(DependencyObject element) => (Breakpoint[]?)element.GetValue(HiddenBreakpointsProperty);

    /// <summary>Sets the breakpoints at which to hide the element.</summary>
    public static void SetHiddenBreakpoints(DependencyObject element, Breakpoint[]? value) => element.SetValue(HiddenBreakpointsProperty, value);

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        bool isWidthUnbounded = double.IsPositiveInfinity(availableSize.Width);
        double width = isWidthUnbounded ? 0 : availableSize.Width;

        // Calculate breakpoint based on width
        Breakpoint breakpoint = CalculateBreakpoint(width);
        SetValue(CurrentBreakpointPropertyKey, breakpoint);

        if (Columns <= 0)
        {
            return new Size(0, 0);
        }

        if (isWidthUnbounded)
        {
            return MeasureUnbounded(breakpoint);
        }

        if (width <= 0)
        {
            return new Size(0, 0);
        }

        double columnWidth = Math.Max(0, (width - (Gutter * (Columns - 1))) / Columns);
        double currentX = 0;
        double currentY = 0;
        double rowHeight = 0;
        int rowCount = 0;
        double maxRowHeight = 0;

        foreach (UIElement child in InternalChildren)
        {
            if (child is null)
            {
                continue;
            }

            // Check if hidden at current breakpoint or collapsed via Visibility
            if (IsHiddenAtBreakpoint(child, breakpoint) || child.Visibility == Visibility.Collapsed)
            {
                child.Measure(new Size(0, 0));
                continue;
            }

            int span = GetEffectiveSpan(child, breakpoint);
            span = Math.Clamp(span, 1, Columns);

            double itemWidth = (columnWidth * span) + (Gutter * (span - 1));

            // Check if item fits on current row (small tolerance for floating point)
            if (currentX + itemWidth > width + 0.5)
            {
                maxRowHeight = Math.Max(maxRowHeight, rowHeight);
                rowCount++;
                currentX = 0;
                currentY += rowHeight + Gutter;
                rowHeight = 0;
            }

            child.Measure(new Size(itemWidth, double.PositiveInfinity));

            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            currentX += itemWidth + Gutter;
        }

        if (rowHeight > 0)
        {
            maxRowHeight = Math.Max(maxRowHeight, rowHeight);
            rowCount++;
        }

        double totalHeight;

        if (UniformRowHeight && rowCount > 0)
        {
            totalHeight = (maxRowHeight * rowCount) + (Gutter * (rowCount - 1));
        }
        else
        {
            totalHeight = currentY + rowHeight;
        }

        return new Size(width, totalHeight);
    }

    /// <summary>Measures every visible child at its natural size and lays them out in a single row.</summary>
    private Size MeasureUnbounded(Breakpoint breakpoint)
    {
        double currentX = 0;
        double maxHeight = 0;
        bool measuredAny = false;

        foreach (UIElement child in InternalChildren)
        {
            if (child is null)
            {
                continue;
            }

            if (IsHiddenAtBreakpoint(child, breakpoint) || child.Visibility == Visibility.Collapsed)
            {
                child.Measure(new Size(0, 0));
                continue;
            }

            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            if (measuredAny)
            {
                currentX += Gutter;
            }

            currentX += child.DesiredSize.Width;
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
            measuredAny = true;
        }

        return new Size(currentX, maxHeight);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        double width = double.IsPositiveInfinity(finalSize.Width) ? 0 : finalSize.Width;
        Breakpoint breakpoint = CurrentBreakpoint;

        if (Columns <= 0 || width <= 0)
        {
            return finalSize;
        }

        double columnWidth = Math.Max(0, (width - (Gutter * (Columns - 1))) / Columns);
        double currentX = 0;
        double currentY = 0;
        double rowHeight = 0;

        // First pass: calculate row heights
        var rowHeights = new List<double>();
        double tempX = 0;
        double tempRowHeight = 0;

        foreach (UIElement child in InternalChildren)
        {
            if (child is null || IsHiddenAtBreakpoint(child, breakpoint) || child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            int span = GetEffectiveSpan(child, breakpoint);
            span = Math.Clamp(span, 1, Columns);

            double itemWidth = (columnWidth * span) + (Gutter * (span - 1));

            if (tempX + itemWidth > width + 0.5)
            {
                rowHeights.Add(tempRowHeight);
                tempX = 0;
                tempRowHeight = 0;
            }

            tempRowHeight = Math.Max(tempRowHeight, child.DesiredSize.Height);
            tempX += itemWidth + Gutter;
        }

        if (tempRowHeight > 0)
        {
            rowHeights.Add(tempRowHeight);
        }

        // Normalize all rows to the tallest row height when uniform mode is enabled
        if (UniformRowHeight && rowHeights.Count > 0)
        {
            double maxH = 0;

            foreach (double h in rowHeights)
            {
                maxH = Math.Max(maxH, h);
            }

            for (int i = 0; i < rowHeights.Count; i++)
            {
                rowHeights[i] = maxH;
            }
        }

        // Second pass: arrange children
        int rowIndex = 0;
        foreach (UIElement child in InternalChildren)
        {
            if (child is null)
            {
                continue;
            }

            if (IsHiddenAtBreakpoint(child, breakpoint) || child.Visibility == Visibility.Collapsed)
            {
                child.Arrange(new Rect(0, 0, 0, 0));
                continue;
            }

            int span = GetEffectiveSpan(child, breakpoint);
            span = Math.Clamp(span, 1, Columns);

            double itemWidth = (columnWidth * span) + (Gutter * (span - 1));

            if (currentX + itemWidth > width + 0.5)
            {
                currentX = 0;
                currentY += rowHeight + Gutter;
                rowIndex++;
                rowHeight = rowIndex < rowHeights.Count ? rowHeights[rowIndex] : 0;
            }
            else if (rowIndex < rowHeights.Count)
            {
                rowHeight = rowHeights[rowIndex];
            }

            double childHeight = child.DesiredSize.Height;
            double arrangeY = currentY;
            double arrangeHeight = childHeight;

            switch (ItemVerticalAlignment)
            {
                case VerticalAlignment.Top:
                    // arrangeY and arrangeHeight already correct
                    break;
                case VerticalAlignment.Center:
                    arrangeY = currentY + ((rowHeight - childHeight) / 2);
                    break;
                case VerticalAlignment.Bottom:
                    arrangeY = currentY + rowHeight - childHeight;
                    break;
                case VerticalAlignment.Stretch:
                    arrangeHeight = rowHeight;

                    // Re-measure with the actual row height so the child layouts
                    // its content to fill the available space instead of using
                    // the natural height from the infinite-height measure pass.
                    child.Measure(new Size(itemWidth, arrangeHeight));
                    break;
            }

            child.Arrange(new Rect(currentX, arrangeY, itemWidth, arrangeHeight));
            currentX += itemWidth + Gutter;
        }

        return finalSize;
    }

    private static object CoerceThreshold(DependencyObject d, object? baseValue)
    {
        double value = baseValue is double dbl ? dbl : 0.0;
        return Math.Clamp(value, 0.0, 1.0);
    }

    private static object CoerceGutter(DependencyObject d, object? baseValue)
    {
        double value = baseValue is double dbl ? dbl : 0.0;
        return double.IsNaN(value) || value < 0 ? 0.0 : value;
    }

    private Breakpoint CalculateBreakpoint(double width)
    {
        double referenceWidth = ReferenceWidth > 0 ? ReferenceWidth : width;

        if (referenceWidth <= 0)
        {
            return Breakpoint.Xs;
        }

        double ratio = width / referenceWidth;

        if (ratio >= LgThreshold)
        {
            return Breakpoint.Lg;
        }

        if (ratio >= MdThreshold)
        {
            return Breakpoint.Md;
        }

        if (ratio >= SmThreshold)
        {
            return Breakpoint.Sm;
        }

        return Breakpoint.Xs;
    }

    private static int GetEffectiveSpan(UIElement element, Breakpoint breakpoint)
    {
        // Get span for current breakpoint, falling back to smaller breakpoints
        return breakpoint switch
        {
            Breakpoint.Lg => GetLg(element) ?? GetMd(element) ?? GetSm(element) ?? GetXs(element),
            Breakpoint.Md => GetMd(element) ?? GetSm(element) ?? GetXs(element),
            Breakpoint.Sm => GetSm(element) ?? GetXs(element),
            _ => GetXs(element),
        };
    }

    private static bool IsHiddenAtBreakpoint(UIElement element, Breakpoint breakpoint)
    {
        // Check single Hidden property
        Breakpoint? hidden = GetHidden(element);
        if (hidden == breakpoint)
        {
            return true;
        }

        // Check HiddenBreakpoints array
        Breakpoint[]? hiddenBreakpoints = GetHiddenBreakpoints(element);
        if (hiddenBreakpoints is not null && Array.IndexOf(hiddenBreakpoints, breakpoint) >= 0)
        {
            return true;
        }

        return false;
    }
}

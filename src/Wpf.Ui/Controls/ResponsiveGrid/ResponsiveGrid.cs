// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;

namespace Wpf.Ui.Controls;

/// <summary>
/// A responsive grid panel that places children on a column lattice, either across a fixed number of
/// columns or across as many columns of at least <see cref="MinColumnWidth"/> as the width allows.
/// </summary>
/// <remarks>
/// <see cref="Columns"/> selects the mode: a value above zero is a fixed grid of that many columns and reads the
/// <c>Xs</c>, <c>Sm</c>, <c>Md</c> and <c>Lg</c> attached spans; zero or less resolves the count from the available
/// width and reads the <c>ColumnSpan</c> attached property. The active <see cref="Breakpoint"/> is the largest one
/// whose threshold ratio (<see cref="SmThreshold"/>, <see cref="MdThreshold"/> or <see cref="LgThreshold"/>) the
/// current width-to-<see cref="ReferenceWidth"/> ratio meets or exceeds; a ratio below every threshold resolves to
/// <see cref="Breakpoint.Xs"/>. <see cref="ReferenceWidth"/> defaults to <see cref="DefaultReferenceWidth"/>
/// (1024 device-independent units). With the default thresholds, that places the boundaries at 256, 512 and 768
/// units of measured width.
/// </remarks>
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
    /// <summary>The reference width, in device-independent units, breakpoints are measured against when <see cref="ReferenceWidth"/> is left unset.</summary>
    public const double DefaultReferenceWidth = 1024d;

    /// <summary>Identifies the <see cref="ItemVerticalAlignment"/> dependency property.</summary>
    public static readonly DependencyProperty ItemVerticalAlignmentProperty = DependencyProperty.Register(
        nameof(ItemVerticalAlignment),
        typeof(VerticalAlignment),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(VerticalAlignment.Top, FrameworkPropertyMetadataOptions.AffectsMeasure)
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

    /// <summary>Identifies the <see cref="MinColumnWidth"/> dependency property.</summary>
    public static readonly DependencyProperty MinColumnWidthProperty = DependencyProperty.Register(
        nameof(MinColumnWidth),
        typeof(double),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(320d, FrameworkPropertyMetadataOptions.AffectsMeasure, null, CoerceMinColumnWidth)
    );

    /// <summary>Identifies the <see cref="MaxColumns"/> dependency property.</summary>
    public static readonly DependencyProperty MaxColumnsProperty = DependencyProperty.Register(
        nameof(MaxColumns),
        typeof(int),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure)
    );

    /// <summary>Identifies the <see cref="ColumnSpacing"/> dependency property.</summary>
    public static readonly DependencyProperty ColumnSpacingProperty = DependencyProperty.Register(
        nameof(ColumnSpacing),
        typeof(double),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure, null, CoerceSpacing)
    );

    /// <summary>Identifies the <see cref="RowSpacing"/> dependency property.</summary>
    public static readonly DependencyProperty RowSpacingProperty = DependencyProperty.Register(
        nameof(RowSpacing),
        typeof(double),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure, null, CoerceSpacing)
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
        new FrameworkPropertyMetadata(DefaultReferenceWidth, FrameworkPropertyMetadataOptions.AffectsMeasure, null, CoerceReferenceWidth)
    );

    private static readonly DependencyPropertyKey CurrentBreakpointPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(CurrentBreakpoint),
        typeof(Breakpoint),
        typeof(ResponsiveGrid),
        new PropertyMetadata(Breakpoint.Xs)
    );

    /// <summary>Identifies the <see cref="CurrentBreakpoint"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentBreakpointProperty = CurrentBreakpointPropertyKey.DependencyProperty;

    /// <summary>Identifies the ColumnSpan attached property (column span in auto-fit mode).</summary>
    public static readonly DependencyProperty ColumnSpanProperty = DependencyProperty.RegisterAttached(
        "ColumnSpan",
        typeof(int),
        typeof(ResponsiveGrid),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsParentMeasure)
    );

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
    /// <remarks><see cref="VerticalAlignment.Stretch"/> re-measures the child at the row height.</remarks>
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
    /// <remarks>Zero or less resolves the column count from the available width and <see cref="MinColumnWidth"/>.</remarks>
    [Bindable(true)]
    [Category("Layout")]
    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>
    /// Gets or sets the narrowest width a column may take before the resolved count drops. Default is 320.
    /// </summary>
    /// <remarks>
    /// Applies when <see cref="Columns"/> is zero or less. Values below one are treated as one; negative,
    /// infinite and <see cref="double.NaN"/> values are coerced to 0.0.
    /// </remarks>
    [Bindable(true)]
    [Category("Layout")]
    public double MinColumnWidth
    {
        get => (double)GetValue(MinColumnWidthProperty);
        set => SetValue(MinColumnWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the highest column count the panel resolves to when <see cref="Columns"/> is zero or less. Default is 0.
    /// </summary>
    /// <remarks>Zero or less leaves the count bounded only by <see cref="MinColumnWidth"/>.</remarks>
    [Bindable(true)]
    [Category("Layout")]
    public int MaxColumns
    {
        get => (int)GetValue(MaxColumnsProperty);
        set => SetValue(MaxColumnsProperty, value);
    }

    /// <summary>
    /// Gets or sets the gap between adjacent columns. Default is <see cref="double.NaN"/>.
    /// </summary>
    /// <remarks><see cref="double.NaN"/> uses <see cref="Gutter"/>. Negative and infinite values are coerced to 0.0.</remarks>
    [Bindable(true)]
    [Category("Layout")]
    public double ColumnSpacing
    {
        get => (double)GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets the gap between adjacent rows. Default is <see cref="double.NaN"/>.
    /// </summary>
    /// <remarks><see cref="double.NaN"/> uses <see cref="Gutter"/>. Negative and infinite values are coerced to 0.0.</remarks>
    [Bindable(true)]
    [Category("Layout")]
    public double RowSpacing
    {
        get => (double)GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets the gap used on an axis whose own spacing property is <see cref="double.NaN"/>. Default is 0.
    /// </summary>
    /// <remarks>Negative, infinite and <see cref="double.NaN"/> values are coerced to 0.0.</remarks>
    [Bindable(true)]
    [Category("Layout")]
    public double Gutter
    {
        get => (double)GetValue(GutterProperty);
        set => SetValue(GutterProperty, value);
    }

    /// <summary>
    /// Gets or sets the ratio (0.0 to 1.0) of <see cref="ReferenceWidth"/> at or above which the Sm breakpoint activates.
    /// Default is 0.25.
    /// </summary>
    /// <remarks>Values outside 0.0-1.0, and <see cref="double.NaN"/>, are coerced to 0.0.</remarks>
    [Bindable(true)]
    [Category("Layout")]
    public double SmThreshold
    {
        get => (double)GetValue(SmThresholdProperty);
        set => SetValue(SmThresholdProperty, value);
    }

    /// <summary>
    /// Gets or sets the ratio (0.0 to 1.0) of <see cref="ReferenceWidth"/> at or above which the Md breakpoint activates.
    /// Default is 0.50.
    /// </summary>
    /// <remarks>Values outside 0.0-1.0, and <see cref="double.NaN"/>, are coerced to 0.0.</remarks>
    [Bindable(true)]
    [Category("Layout")]
    public double MdThreshold
    {
        get => (double)GetValue(MdThresholdProperty);
        set => SetValue(MdThresholdProperty, value);
    }

    /// <summary>
    /// Gets or sets the ratio (0.0 to 1.0) of <see cref="ReferenceWidth"/> at or above which the Lg breakpoint activates.
    /// Default is 0.75.
    /// </summary>
    /// <remarks>
    /// Values outside 0.0-1.0, and <see cref="double.NaN"/>, are coerced to 0.0. Setting this to 0.0 makes every
    /// positive container width resolve to <see cref="Breakpoint.Lg"/>.
    /// </remarks>
    [Bindable(true)]
    [Category("Layout")]
    public double LgThreshold
    {
        get => (double)GetValue(LgThresholdProperty);
        set => SetValue(LgThresholdProperty, value);
    }

    /// <summary>
    /// Gets or sets the reference width, in device-independent units, that <see cref="SmThreshold"/>,
    /// <see cref="MdThreshold"/> and <see cref="LgThreshold"/> are fractions of. Defaults to
    /// <see cref="DefaultReferenceWidth"/>.
    /// </summary>
    /// <remarks>
    /// Setting this to 0.0 compares the container's measured width to itself, so every positive width resolves
    /// to <see cref="Breakpoint.Lg"/>. Negative values and <see cref="double.NaN"/> are coerced to 0.0.
    /// </remarks>
    [Bindable(true)]
    [Category("Layout")]
    public double ReferenceWidth
    {
        get => (double)GetValue(ReferenceWidthProperty);
        set => SetValue(ReferenceWidthProperty, value);
    }

    /// <summary>
    /// Gets the breakpoint the most recent layout pass resolved from the width that pass received.
    /// </summary>
    [Browsable(false)]
    public Breakpoint CurrentBreakpoint => (Breakpoint)GetValue(CurrentBreakpointProperty);

    /// <summary>Gets the number of columns the element occupies when <see cref="Columns"/> is zero or less.</summary>
    /// <param name="element">The element to read the span from.</param>
    /// <returns>The column span, before clamping to the resolved column count.</returns>
    public static int GetColumnSpan(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (int)element.GetValue(ColumnSpanProperty);
    }

    /// <summary>Sets the number of columns the element occupies when <see cref="Columns"/> is zero or less.</summary>
    /// <param name="element">The element to set the span on.</param>
    /// <param name="value">The column span.</param>
    public static void SetColumnSpan(DependencyObject element, int value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(ColumnSpanProperty, value);
    }

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

    /// <summary>Resolves the column count the panel uses for a given width when <see cref="Columns"/> is zero or less.</summary>
    /// <param name="availableWidth">The width available to the panel.</param>
    /// <returns>The column count, never below one.</returns>
    public int ResolveColumnCount(double availableWidth) => ResolveColumnCount(availableWidth, ResolveColumnSpacing());

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        double width = availableSize.Width;

        if (double.IsNaN(width) || double.IsInfinity(width))
        {
            Breakpoint unboundedBreakpoint = CalculateBreakpoint(0);
            SetValue(CurrentBreakpointPropertyKey, unboundedBreakpoint);

            return MeasureUnbounded(unboundedBreakpoint);
        }

        width = Math.Max(0, width);

        Breakpoint breakpoint = CalculateBreakpoint(width);
        SetValue(CurrentBreakpointPropertyKey, breakpoint);

        return LayoutPass(width, breakpoint, arrange: false);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        double width = finalSize.Width;

        if (double.IsNaN(width) || double.IsInfinity(width))
        {
            ArrangeAllToEmptyRect();

            return finalSize;
        }

        width = Math.Max(0, width);

        Breakpoint breakpoint = CalculateBreakpoint(width);
        SetValue(CurrentBreakpointPropertyKey, breakpoint);

        _ = LayoutPass(width, breakpoint, arrange: true);

        return finalSize;
    }

    private static object CoerceThreshold(DependencyObject d, object? baseValue)
    {
        double value = baseValue is double dbl ? dbl : 0.0;
        return double.IsNaN(value) ? 0.0 : Math.Clamp(value, 0.0, 1.0);
    }

    private static object CoerceGutter(DependencyObject d, object? baseValue)
    {
        double value = baseValue is double dbl ? dbl : 0.0;
        return double.IsNaN(value) || double.IsInfinity(value) || value < 0 ? 0.0 : value;
    }

    private static object CoerceSpacing(DependencyObject d, object? baseValue)
    {
        double value = baseValue is double dbl ? dbl : double.NaN;

        if (double.IsNaN(value))
        {
            return double.NaN;
        }

        return double.IsInfinity(value) || value < 0 ? 0.0 : value;
    }

    private static object CoerceMinColumnWidth(DependencyObject d, object? baseValue)
    {
        double value = baseValue is double dbl ? dbl : 0.0;
        return double.IsNaN(value) || double.IsInfinity(value) || value < 0 ? 0.0 : value;
    }

    private static object CoerceReferenceWidth(DependencyObject d, object? baseValue)
    {
        double value = baseValue is double dbl ? dbl : 0.0;
        return double.IsNaN(value) || value < 0 ? 0.0 : value;
    }

    private static int GetEffectiveSpan(UIElement element, Breakpoint breakpoint)
    {
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
        Breakpoint? hidden = GetHidden(element);
        if (hidden == breakpoint)
        {
            return true;
        }

        Breakpoint[]? hiddenBreakpoints = GetHiddenBreakpoints(element);
        if (hiddenBreakpoints is not null && Array.IndexOf(hiddenBreakpoints, breakpoint) >= 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>Measures every visible child at its natural size and lays them out in a single row.</summary>
    private Size MeasureUnbounded(Breakpoint breakpoint)
    {
        double columnSpacing = ResolveColumnSpacing();
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
                currentX += columnSpacing;
            }

            currentX += child.DesiredSize.Width;
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
            measuredAny = true;
        }

        return new Size(currentX, maxHeight);
    }

    /// <summary>Places every child on the column lattice, measuring each one and optionally arranging it.</summary>
    private Size LayoutPass(double width, Breakpoint breakpoint, bool arrange)
    {
        bool isFixed = Columns > 0;
        double columnSpacing = ResolveColumnSpacing();
        double rowSpacing = ResolveRowSpacing();
        double reportedWidth = width;
        int columns;
        double columnWidth;

        if (isFixed)
        {
            columns = Columns;
            columnWidth = (width - (columnSpacing * (columns - 1))) / columns;

            if (columnWidth <= 0)
            {
                columnWidth = 1;
                reportedWidth = columns + (columnSpacing * (columns - 1));
            }
        }
        else
        {
            columns = ResolveColumnCount(width, columnSpacing);
            columnWidth = (width - (columnSpacing * (columns - 1))) / columns;

            if (columnWidth <= 0)
            {
                columns = 1;
                columnWidth = width;
            }
        }

        List<CellPlacement> placements = [];
        List<double> rowHeights = [];
        List<UIElement> excluded = [];
        int column = 0;
        int row = 0;

        foreach (UIElement child in InternalChildren)
        {
            if (child is null)
            {
                continue;
            }

            if (IsHiddenAtBreakpoint(child, breakpoint) || child.Visibility == Visibility.Collapsed)
            {
                child.Measure(new Size(0, 0));
                excluded.Add(child);
                continue;
            }

            int span = isFixed ? GetEffectiveSpan(child, breakpoint) : GetColumnSpan(child);
            span = Math.Clamp(span, 1, columns);

            if (column > 0 && column + span > columns)
            {
                column = 0;
                row++;
            }

            double cellWidth = (columnWidth * span) + (columnSpacing * (span - 1));
            child.Measure(new Size(cellWidth, double.PositiveInfinity));

            while (rowHeights.Count <= row)
            {
                rowHeights.Add(0);
            }

            rowHeights[row] = Math.Max(rowHeights[row], child.DesiredSize.Height);
            placements.Add(new CellPlacement(child, column, span, row));

            column += span;

            if (column >= columns)
            {
                column = 0;
                row++;
            }
        }

        if (UniformRowHeight && rowHeights.Count > 0)
        {
            double tallest = 0;

            foreach (double height in rowHeights)
            {
                tallest = Math.Max(tallest, height);
            }

            for (int index = 0; index < rowHeights.Count; index++)
            {
                rowHeights[index] = tallest;
            }
        }

        double[] rowOffsets = new double[rowHeights.Count];
        double offset = 0;

        for (int index = 0; index < rowHeights.Count; index++)
        {
            rowOffsets[index] = offset;
            offset += rowHeights[index] + rowSpacing;
        }

        double totalHeight = rowHeights.Count == 0 ? 0 : rowOffsets[^1] + rowHeights[^1];

        if (arrange)
        {
            ArrangePlacements(placements, rowHeights, rowOffsets, columnWidth, columnSpacing);

            foreach (UIElement child in excluded)
            {
                child.Arrange(default);
            }
        }

        return new Size(reportedWidth, totalHeight);
    }

    private void ArrangePlacements(
        List<CellPlacement> placements,
        List<double> rowHeights,
        double[] rowOffsets,
        double columnWidth,
        double columnSpacing)
    {
        foreach (CellPlacement placement in placements)
        {
            double cellWidth = (columnWidth * placement.Span) + (columnSpacing * (placement.Span - 1));
            double x = placement.Column * (columnWidth + columnSpacing);
            double rowHeight = rowHeights[placement.Row];
            double y = rowOffsets[placement.Row];
            double childHeight = placement.Child.DesiredSize.Height;
            double arrangeHeight = childHeight;

            switch (ItemVerticalAlignment)
            {
                case VerticalAlignment.Center:
                    y += (rowHeight - childHeight) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    y += rowHeight - childHeight;
                    break;
                case VerticalAlignment.Stretch:
                    arrangeHeight = rowHeight;
                    placement.Child.Measure(new Size(cellWidth, arrangeHeight));
                    break;
            }

            placement.Child.Arrange(new Rect(x, y, cellWidth, arrangeHeight));
        }
    }

    private int ResolveColumnCount(double availableWidth, double columnSpacing)
    {
        if (double.IsNaN(availableWidth) || double.IsInfinity(availableWidth) || availableWidth <= 0)
        {
            return 1;
        }

        double minimum = Math.Max(1, MinColumnWidth);
        int columns = Math.Max(1, (int)Math.Floor((availableWidth + columnSpacing) / (minimum + columnSpacing)));

        if (MaxColumns > 0)
        {
            columns = Math.Min(columns, MaxColumns);
        }

        return columns;
    }

    private double ResolveColumnSpacing() => double.IsNaN(ColumnSpacing) ? Gutter : ColumnSpacing;

    private double ResolveRowSpacing() => double.IsNaN(RowSpacing) ? Gutter : RowSpacing;

    private void ArrangeAllToEmptyRect()
    {
        foreach (UIElement child in InternalChildren)
        {
            child?.Arrange(default);
        }
    }

    /// <summary>Resolves the breakpoint whose threshold ratio the current width-to-<see cref="ReferenceWidth"/> ratio meets, checking Lg first.</summary>
    /// <remarks>
    /// If <see cref="SmThreshold"/>, <see cref="MdThreshold"/> and <see cref="LgThreshold"/> are not set in ascending order,
    /// this still checks Lg, then Md, then Sm; the largest breakpoint whose threshold the ratio meets wins.
    /// </remarks>
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

    private readonly record struct CellPlacement(UIElement Child, int Column, int Span, int Row);
}

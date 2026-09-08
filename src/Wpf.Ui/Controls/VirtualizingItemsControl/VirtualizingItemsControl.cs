// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

/* Based on VirtualizingWrapPanel created by S. Bäumlisberger licensed under MIT license.
   https://github.com/sbaeumlisberger/VirtualizingWrapPanel
   Copyright (C) S. Bäumlisberger
   All Rights Reserved. */

using System.Windows.Controls;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Virtualized <see cref="ItemsControl"/>.
/// <para>Based on <see href="https://github.com/sbaeumlisberger/VirtualizingWrapPanel"/>.</para>
/// </summary>
public class VirtualizingItemsControl : ItemsControl
{
    /// <summary>Identifies the <see cref="CacheLengthUnit"/> dependency property.</summary>
    public static readonly DependencyProperty CacheLengthUnitProperty = VirtualizingPanel.CacheLengthUnitProperty.AddOwner(
        typeof(VirtualizingItemsControl),
        new FrameworkPropertyMetadata(
            VirtualizationCacheLengthUnit.Page,
            FrameworkPropertyMetadataOptions.None,
            null,
            CoerceCacheLengthUnit)
    );

    /// <summary>
    /// Gets or sets the cache length unit.
    /// </summary>
    public VirtualizationCacheLengthUnit CacheLengthUnit
    {
        get => (VirtualizationCacheLengthUnit)GetValue(CacheLengthUnitProperty);
        set => SetValue(CacheLengthUnitProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VirtualizingItemsControl"/> class.
    /// </summary>
    public VirtualizingItemsControl()
    {
        VirtualizingPanel.SetCacheLength(this, new VirtualizationCacheLength(1));
        VirtualizingPanel.SetIsVirtualizingWhenGrouping(this, true);
    }

    private static object CoerceCacheLengthUnit(DependencyObject d, object baseValue) => baseValue;
}

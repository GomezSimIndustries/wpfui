// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Data;

namespace Wpf.Ui.Converters;

/// <summary>Computes the collapsible content's slide offset for one axis of an <see cref="Expander" />.</summary>
internal class ExpanderContentOffsetConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is not double completeValue)
        {
            return 0.0;
        }

        if (values[1] is not double factor)
        {
            return 0.0;
        }

        if (values[2] is not ExpandDirection direction)
        {
            return 0.0;
        }

        bool isXAxis = "X".Equals(parameter as string, StringComparison.Ordinal);

        return (isXAxis, direction) switch
        {
            (true, ExpandDirection.Right) => -factor * completeValue,
            (true, ExpandDirection.Left) => factor * completeValue,
            (false, ExpandDirection.Down) => -factor * completeValue,
            (false, ExpandDirection.Up) => factor * completeValue,
            _ => 0.0,
        };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

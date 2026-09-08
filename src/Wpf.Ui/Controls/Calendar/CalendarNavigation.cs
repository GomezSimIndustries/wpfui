// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Provides attached properties that localize the accessible names of the previous/next
/// navigation buttons rendered by the default <see cref="System.Windows.Controls.Calendar"/> template.
/// </summary>
public static class CalendarNavigation
{
    /// <summary>Identifies the PreviousButtonName attached property.</summary>
    public static readonly DependencyProperty PreviousButtonNameProperty = DependencyProperty.RegisterAttached(
        "PreviousButtonName",
        typeof(string),
        typeof(CalendarNavigation),
        new PropertyMetadata("Previous")
    );

    /// <summary>Identifies the NextButtonName attached property.</summary>
    public static readonly DependencyProperty NextButtonNameProperty = DependencyProperty.RegisterAttached(
        "NextButtonName",
        typeof(string),
        typeof(CalendarNavigation),
        new PropertyMetadata("Next")
    );

    /// <summary>Gets the accessible name announced for the previous-page navigation button.</summary>
    public static string GetPreviousButtonName(DependencyObject element)
    {
        return (string)element.GetValue(PreviousButtonNameProperty);
    }

    /// <summary>Sets the accessible name announced for the previous-page navigation button.</summary>
    public static void SetPreviousButtonName(DependencyObject element, string value)
    {
        element.SetValue(PreviousButtonNameProperty, value);
    }

    /// <summary>Gets the accessible name announced for the next-page navigation button.</summary>
    public static string GetNextButtonName(DependencyObject element)
    {
        return (string)element.GetValue(NextButtonNameProperty);
    }

    /// <summary>Sets the accessible name announced for the next-page navigation button.</summary>
    public static void SetNextButtonName(DependencyObject element, string value)
    {
        element.SetValue(NextButtonNameProperty, value);
    }
}

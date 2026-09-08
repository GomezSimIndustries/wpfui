// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Globalization;
using Wpf.Ui.UnitTests.TestHelpers;
using CalendarDatePickerControl = Wpf.Ui.Controls.CalendarDatePicker;

namespace Wpf.Ui.UnitTests.Controls;

public class CalendarDatePickerCultureDefaultsTests
{
    [Fact]
    public void FirstDayOfWeek_WithNoExplicitValue_ResolvesFromTheConstructingCultureEachTime()
    {
        DayOfWeek enUsDefault = WpfTestHost.Invoke(
            () => WithCulture("en-US", () => new CalendarDatePickerControl().FirstDayOfWeek)
        );
        DayOfWeek frFrDefault = WpfTestHost.Invoke(
            () => WithCulture("fr-FR", () => new CalendarDatePickerControl().FirstDayOfWeek)
        );

        Assert.Equal(DayOfWeek.Sunday, enUsDefault);
        Assert.Equal(DayOfWeek.Monday, frFrDefault);
    }

    [Fact]
    public void FirstDayOfWeek_SetExplicitly_IsNotReplacedByTheCultureDefault()
    {
        DayOfWeek explicitValue = WpfTestHost.Invoke(
            () => WithCulture(
                "fr-FR",
                () => new CalendarDatePickerControl { FirstDayOfWeek = DayOfWeek.Wednesday }.FirstDayOfWeek
            )
        );

        Assert.Equal(DayOfWeek.Wednesday, explicitValue);
    }

    [Fact]
    public void FirstDayOfWeek_SetExplicitlyToTheAmbientCultureDefault_SurvivesAFurtherCultureChange()
    {
        (DayOfWeek beforeChange, DayOfWeek afterChange) = WpfTestHost.Invoke(() =>
        {
            CultureInfo previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                var picker = new CalendarDatePickerControl { FirstDayOfWeek = DayOfWeek.Monday };
                DayOfWeek before = picker.FirstDayOfWeek;

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                DayOfWeek after = picker.FirstDayOfWeek;

                return (before, after);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        });

        Assert.Equal(DayOfWeek.Monday, beforeChange);
        Assert.Equal(DayOfWeek.Monday, afterChange);
    }

    private static T WithCulture<T>(string cultureName, Func<T> func)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
        try
        {
            return func();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}

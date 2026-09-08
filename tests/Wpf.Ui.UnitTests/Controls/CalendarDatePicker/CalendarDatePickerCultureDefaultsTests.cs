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
        DayOfWeek? enUsDefault = null;
        DayOfWeek? frFrDefault = null;

        StaThread.Run(() =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            enUsDefault = new CalendarDatePickerControl().FirstDayOfWeek;
        });

        StaThread.Run(() =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            frFrDefault = new CalendarDatePickerControl().FirstDayOfWeek;
        });

        Assert.Equal(DayOfWeek.Sunday, enUsDefault);
        Assert.Equal(DayOfWeek.Monday, frFrDefault);
    }

    [Fact]
    public void FirstDayOfWeek_SetExplicitly_IsNotReplacedByTheCultureDefault()
    {
        DayOfWeek? explicitValue = null;

        StaThread.Run(() =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var picker = new CalendarDatePickerControl { FirstDayOfWeek = DayOfWeek.Wednesday };
            explicitValue = picker.FirstDayOfWeek;
        });

        Assert.Equal(DayOfWeek.Wednesday, explicitValue);
    }

    [Fact]
    public void FirstDayOfWeek_SetExplicitlyToTheAmbientCultureDefault_SurvivesAFurtherCultureChange()
    {
        DayOfWeek? beforeChange = null;
        DayOfWeek? afterChange = null;

        StaThread.Run(() =>
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var picker = new CalendarDatePickerControl { FirstDayOfWeek = DayOfWeek.Monday };
            beforeChange = picker.FirstDayOfWeek;

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            afterChange = picker.FirstDayOfWeek;
        });

        Assert.Equal(DayOfWeek.Monday, beforeChange);
        Assert.Equal(DayOfWeek.Monday, afterChange);
    }
}

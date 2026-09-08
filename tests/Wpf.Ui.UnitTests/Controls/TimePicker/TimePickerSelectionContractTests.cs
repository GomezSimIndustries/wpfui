// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Threading;
using Wpf.Ui.UnitTests.TestHelpers;
using ClockIdentifierValue = Wpf.Ui.Controls.ClockIdentifier;
using TimePickerControl = Wpf.Ui.Controls.TimePicker;

namespace Wpf.Ui.UnitTests.Controls;

public class TimePickerSelectionContractTests
{
    [Fact]
    public void SettingSelectedTime_MirrorsIntoTime()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new TimePickerControl();

            picker.SelectedTime = new TimeSpan(13, 45, 0);

            Assert.Equal(new TimeSpan(13, 45, 0), picker.Time);
        });
    }

    [Fact]
    public void SettingTime_MirrorsIntoSelectedTime()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new TimePickerControl();

            picker.Time = new TimeSpan(6, 30, 0);

            Assert.Equal(new TimeSpan(6, 30, 0), picker.SelectedTime);
        });
    }

    [Fact]
    public void ClearingSelectedTime_ResetsTimeToMidnight()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new TimePickerControl { SelectedTime = new TimeSpan(13, 45, 0) };

            picker.SelectedTime = null;

            Assert.Null(picker.SelectedTime);
            Assert.Equal(TimeSpan.Zero, picker.Time);
        });
    }

    [Fact]
    public void SelectedTime_BeforeMidnight_ClampsToMidnight()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new TimePickerControl();

            picker.SelectedTime = TimeSpan.FromMinutes(-30);

            Assert.Equal(TimeSpan.Zero, picker.SelectedTime);
        });
    }

    [Fact]
    public void SelectedTime_AtOrPastADay_ClampsToTheLastMinuteOfTheDay()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new TimePickerControl();

            picker.SelectedTime = TimeSpan.FromDays(1);

            Assert.Equal(new TimeSpan(23, 59, 0), picker.SelectedTime);
        });
    }

    [Fact]
    public void SelectedTime_DropsSecondsAndFractions()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new TimePickerControl();

            picker.SelectedTime = new TimeSpan(0, 8, 30, 25, 400);

            Assert.Equal(new TimeSpan(8, 30, 0), picker.SelectedTime);
        });
    }

    [Fact]
    public void Time_PastADay_ClampsToTheLastMinuteOfTheDay()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new TimePickerControl();

            picker.Time = TimeSpan.FromHours(30);

            Assert.Equal(new TimeSpan(23, 59, 0), picker.Time);
            Assert.Equal(new TimeSpan(23, 59, 0), picker.SelectedTime);
        });
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(60, 59)]
    [InlineData(15, 15)]
    public void MinuteIncrement_CoercesIntoTheSupportedRange(int assigned, int expected)
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new TimePickerControl();

            picker.MinuteIncrement = assigned;

            Assert.Equal(expected, picker.MinuteIncrement);
        });
    }

    [Fact]
    public void ClockIdentifier_DefaultsToTwelveHourForATwelveHourCulture()
    {
        WpfTestHost.Invoke(() =>
        {
            WithCulture(
                "en-US",
                () =>
                {
                    var picker = new TimePickerControl();

                    Assert.Equal(ClockIdentifierValue.Clock12Hour, picker.ClockIdentifier);
                }
            );
        });
    }

    [Fact]
    public void ClockIdentifier_DefaultsToTwentyFourHourForATwentyFourHourCulture()
    {
        WpfTestHost.Invoke(() =>
        {
            WithCulture(
                "de-DE",
                () =>
                {
                    var picker = new TimePickerControl();

                    Assert.Equal(ClockIdentifierValue.Clock24Hour, picker.ClockIdentifier);
                }
            );
        });
    }

    [Fact]
    public void ClockIdentifier_AssignedByTheHost_OutranksTheCultureDefault()
    {
        WpfTestHost.Invoke(() =>
        {
            WithCulture(
                "de-DE",
                () =>
                {
                    var picker = new TimePickerControl
                    {
                        ClockIdentifier = ClockIdentifierValue.Clock12Hour,
                    };

                    Assert.Equal(ClockIdentifierValue.Clock12Hour, picker.ClockIdentifier);
                }
            );
        });
    }

    [Fact]
    public void DisplayText_WithNothingSelected_ShowsThePlaceholdersAndTheMorningDesignator()
    {
        WpfTestHost.Invoke(() =>
        {
            WithCulture(
                "en-US",
                () =>
                {
                    var picker = new TimePickerControl();

                    Assert.Equal("hour", picker.HourText);
                    Assert.Equal("minute", picker.MinuteText);
                    Assert.Equal("AM", picker.DesignatorText);
                }
            );
        });
    }

    [Fact]
    public void DisplayText_FollowsTheReplacedPlaceholders()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new TimePickerControl
            {
                HourPlaceholderText = "Stunde",
                MinutePlaceholderText = "Minute",
            };

            Assert.Equal("Stunde", picker.HourText);
            Assert.Equal("Minute", picker.MinuteText);
        });
    }

    [Theory]
    [InlineData(0, 30, "12", "30", "AM")]
    [InlineData(9, 5, "9", "05", "AM")]
    [InlineData(12, 0, "12", "00", "PM")]
    [InlineData(13, 45, "1", "45", "PM")]
    public void DisplayText_InTwelveHourMode_ShowsTheClockHourAndDesignator(
        int hour,
        int minute,
        string expectedHour,
        string expectedMinute,
        string expectedDesignator
    )
    {
        WpfTestHost.Invoke(() =>
        {
            WithCulture(
                "en-US",
                () =>
                {
                    var picker = new TimePickerControl
                    {
                        ClockIdentifier = ClockIdentifierValue.Clock12Hour,
                        SelectedTime = new TimeSpan(hour, minute, 0),
                    };

                    Assert.Equal(expectedHour, picker.HourText);
                    Assert.Equal(expectedMinute, picker.MinuteText);
                    Assert.Equal(expectedDesignator, picker.DesignatorText);
                }
            );
        });
    }

    [Theory]
    [InlineData(0, 30, "00", "30")]
    [InlineData(9, 5, "09", "05")]
    [InlineData(23, 59, "23", "59")]
    public void DisplayText_InTwentyFourHourMode_PadsTheHourAndMinute(
        int hour,
        int minute,
        string expectedHour,
        string expectedMinute
    )
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new TimePickerControl
            {
                ClockIdentifier = ClockIdentifierValue.Clock24Hour,
                SelectedTime = new TimeSpan(hour, minute, 0),
            };

            Assert.Equal(expectedHour, picker.HourText);
            Assert.Equal(expectedMinute, picker.MinuteText);
        });
    }

    [Fact]
    public void SwitchingClockIdentifier_RewritesTheDisplayedHour()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new TimePickerControl
            {
                ClockIdentifier = ClockIdentifierValue.Clock24Hour,
                SelectedTime = new TimeSpan(13, 45, 0),
            };

            Assert.Equal("13", picker.HourText);

            picker.ClockIdentifier = ClockIdentifierValue.Clock12Hour;

            Assert.Equal("1", picker.HourText);
        });
    }

    [Fact]
    public void HourItems_InTwelveHourMode_RunFromTwelveThroughEleven()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new TimePickerControl
            {
                ClockIdentifier = ClockIdentifierValue.Clock12Hour,
            };

            Assert.Equal(12, picker.HourItems.Count);
            Assert.Equal("12", picker.HourItems[0]);
            Assert.Equal("1", picker.HourItems[1]);
            Assert.Equal("11", picker.HourItems[11]);
        });
    }

    [Fact]
    public void HourItems_InTwentyFourHourMode_RunFromMidnightThroughTwentyThree()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new TimePickerControl
            {
                ClockIdentifier = ClockIdentifierValue.Clock24Hour,
            };

            Assert.Equal(24, picker.HourItems.Count);
            Assert.Equal("00", picker.HourItems[0]);
            Assert.Equal("23", picker.HourItems[23]);
        });
    }

    [Fact]
    public void MinuteItems_FollowTheMinuteIncrement()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new TimePickerControl { MinuteIncrement = 15 };

            Assert.Equal(new[] { "00", "15", "30", "45" }, picker.MinuteItems);
        });
    }

    [Fact]
    public void MinuteItems_AtTheDefaultIncrement_CoverEveryMinute()
    {
        WpfTestHost.Invoke(() =>
        {
            var picker = new TimePickerControl();

            Assert.Equal(60, picker.MinuteItems.Count);
            Assert.Equal("59", picker.MinuteItems[59]);
        });
    }

    [Fact]
    public void DesignatorItems_ComeFromTheCulture()
    {
        WpfTestHost.Invoke(() =>
        {
            WithCulture(
                "en-US",
                () =>
                {
                    var picker = new TimePickerControl();

                    Assert.Equal(new[] { "AM", "PM" }, picker.DesignatorItems);
                }
            );
        });
    }

    private static void WithCulture(string name, Action action)
    {
        CultureInfo previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new CultureInfo(name);

        try
        {
            action();
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}

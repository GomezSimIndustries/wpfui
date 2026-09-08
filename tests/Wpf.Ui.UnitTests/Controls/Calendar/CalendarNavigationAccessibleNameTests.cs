// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Wpf.Ui.UnitTests.TestHelpers;
using CalendarControl = System.Windows.Controls.Calendar;
using CalendarNavigation = Wpf.Ui.Controls.CalendarNavigation;

namespace Wpf.Ui.UnitTests.Controls;

public class CalendarNavigationAccessibleNameTests
{
    [Fact]
    public void DefaultNavigationButtonNames_MatchOriginalEnglishChrome()
    {
        WpfTestHost.Invoke(() =>
        {
            var (previous, next) = CreateNavigationButtons(CreateStyledCalendar());

            PumpUntil(() => AutomationProperties.GetName(previous).Length > 0);

            Assert.Equal("Previous", AutomationProperties.GetName(previous));
            Assert.Equal("Next", AutomationProperties.GetName(next));
        });
    }

    [Fact]
    public void SettingCalendarNavigationNames_FlowsToTheNavigationButtonsAutomationName()
    {
        WpfTestHost.Invoke(() =>
        {
            var calendar = CreateStyledCalendar();
            CalendarNavigation.SetPreviousButtonName(calendar, "Précédent");
            CalendarNavigation.SetNextButtonName(calendar, "Suivant");

            var (previous, next) = CreateNavigationButtons(calendar);

            PumpUntil(() => AutomationProperties.GetName(previous).Length > 0);

            Assert.Equal("Précédent", AutomationProperties.GetName(previous));
            Assert.Equal("Suivant", AutomationProperties.GetName(next));
        });
    }

    [Fact]
    public void NextButton_MovesTheDisplayDateForwardOneMonth_WhenFlowDirectionIsRightToLeft()
    {
        WpfTestHost.Invoke(() =>
        {
            var calendar = CreateStyledCalendar();
            calendar.FlowDirection = FlowDirection.RightToLeft;
            calendar.DisplayDate = new DateTime(2026, 6, 1);

            var (_, next) = CreateNavigationButtons(calendar);
            Click(next);
            PumpUntil(() => calendar.DisplayDate != new DateTime(2026, 6, 1));

            Assert.Equal(new DateTime(2026, 7, 1), calendar.DisplayDate);
        });
    }

    [Fact]
    public void PreviousButton_MovesTheDisplayDateBackwardOneMonth_WhenFlowDirectionIsRightToLeft()
    {
        WpfTestHost.Invoke(() =>
        {
            var calendar = CreateStyledCalendar();
            calendar.FlowDirection = FlowDirection.RightToLeft;
            calendar.DisplayDate = new DateTime(2026, 6, 1);

            var (previous, _) = CreateNavigationButtons(calendar);
            Click(previous);
            PumpUntil(() => calendar.DisplayDate != new DateTime(2026, 6, 1));

            Assert.Equal(new DateTime(2026, 5, 1), calendar.DisplayDate);
        });
    }

    private static CalendarControl CreateStyledCalendar() =>
        new() { Style = (Style)Application.Current.FindResource(typeof(CalendarControl)) };

    private static (Button Previous, Button Next) CreateNavigationButtons(CalendarControl calendar)
    {
        calendar.ApplyTemplate();
        var calendarItem = (CalendarItem)calendar.Template.FindName("PART_CalendarItem", calendar);
        calendarItem.ApplyTemplate();

        var previous = (Button)calendarItem.Template.FindName("PART_PreviousButton", calendarItem);
        var next = (Button)calendarItem.Template.FindName("PART_NextButton", calendarItem);

        return (previous, next);
    }

    private static void Click(Button button)
    {
        var peer = (IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(button)!;
        peer.Invoke();
    }

    private static void PumpUntil(Func<bool> condition)
    {
        const int MaxIterations = 2000;

        for (var i = 0; i < MaxIterations && !condition(); i++)
        {
            var frame = new DispatcherFrame();
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                new Action(() => frame.Continue = false)
            );
            Dispatcher.PushFrame(frame);
        }
    }
}

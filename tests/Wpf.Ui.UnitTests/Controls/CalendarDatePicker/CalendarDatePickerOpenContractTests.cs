// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Wpf.Ui.UnitTests.TestHelpers;
using CalendarDatePickerControl = Wpf.Ui.Controls.CalendarDatePicker;

namespace Wpf.Ui.UnitTests.Controls;

public class CalendarDatePickerOpenContractTests
{
    [Fact]
    public void SettingIsCalendarOpen_BeforeAnyClick_OpensThePopup()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, picker) = CreateHostedPicker();

            try
            {
                picker.SetCurrentValue(CalendarDatePickerControl.IsCalendarOpenProperty, true);
                PumpUntil(() => picker.PopupOpenedCount > 0);

                Assert.True(picker.IsCalendarOpen);
                Assert.Equal(1, picker.PopupOpenedCount);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Clicking_OpensAndClosesRepeatedly_InitializingThePopupOnlyOnce()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, picker) = CreateHostedPicker();

            try
            {
                Click(picker);
                PumpUntil(() => picker.PopupOpenedCount == 1);
                Assert.True(picker.IsCalendarOpen);

                Click(picker);
                Assert.False(picker.IsCalendarOpen);

                Click(picker);
                PumpUntil(() => picker.PopupOpenedCount == 2);
                Assert.True(picker.IsCalendarOpen);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Opening_WithASelectedDate_MovesKeyboardFocusIntoTheCalendar()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, picker) = CreateHostedPicker();

            try
            {
                picker.Date = new DateTime(2026, 6, 15);
                picker.SetCurrentValue(CalendarDatePickerControl.IsCalendarOpenProperty, true);
                PumpUntil(() => picker.PopupOpenedCount > 0);

                var calendar = (Calendar)picker.LastOpenedCalendarChild!;
                PumpUntil(() => calendar.IsKeyboardFocusWithin);

                Assert.True(calendar.IsKeyboardFocusWithin);
                Assert.NotSame(calendar, Keyboard.FocusedElement);
                Assert.Equal(picker.Date, calendar.SelectedDate);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Opening_WithNoSelectedDate_MovesKeyboardFocusIntoTheCalendar()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, picker) = CreateHostedPicker();

            try
            {
                picker.SetCurrentValue(CalendarDatePickerControl.IsCalendarOpenProperty, true);
                PumpUntil(() => picker.PopupOpenedCount > 0);

                var calendar = (Calendar)picker.LastOpenedCalendarChild!;
                PumpUntil(() => calendar.IsKeyboardFocusWithin);

                Assert.True(calendar.IsKeyboardFocusWithin);
                Assert.NotSame(calendar, Keyboard.FocusedElement);
                Assert.Null(calendar.SelectedDate);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void PressingEscape_WhileOpen_ClosesThePopup()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, picker) = CreateHostedPicker();

            try
            {
                picker.SetCurrentValue(CalendarDatePickerControl.IsCalendarOpenProperty, true);
                PumpUntil(() => picker.PopupOpenedCount > 0);

                var calendar = (Calendar)picker.LastOpenedCalendarChild!;
                RaisePreviewKeyDown(calendar, Key.Escape);

                Assert.False(picker.IsCalendarOpen);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SelectingADateOnTheCalendar_ClosesThePopupAndUpdatesDate()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, picker) = CreateHostedPicker();

            try
            {
                picker.SetCurrentValue(CalendarDatePickerControl.IsCalendarOpenProperty, true);
                PumpUntil(() => picker.PopupOpenedCount > 0);

                var calendar = (Calendar)picker.LastOpenedCalendarChild!;
                var selected = new DateTime(2026, 3, 4);
                calendar.SelectedDate = selected;

                Assert.False(picker.IsCalendarOpen);
                Assert.Equal(selected, picker.Date);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Unloading_WhilePopupOpen_ClosesThePopup()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, picker) = CreateHostedPicker();

            try
            {
                picker.SetCurrentValue(CalendarDatePickerControl.IsCalendarOpenProperty, true);
                PumpUntil(() => picker.PopupOpenedCount > 0);

                picker.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

                Assert.False(picker.IsCalendarOpen);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static void RaisePreviewKeyDown(UIElement element, Key key)
    {
        var device = InputManager.Current.PrimaryKeyboardDevice;
        element.RaiseEvent(
            new KeyEventArgs(device, PresentationSource.FromVisual(element), 0, key)
            {
                RoutedEvent = Keyboard.PreviewKeyDownEvent,
            }
        );
    }

    private static void Click(TestableCalendarDatePicker picker)
    {
        var before = picker.ClickInvocations;
        var peer = (IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(picker)!;
        peer.Invoke();
        PumpUntil(() => picker.ClickInvocations > before);
    }

    private static void PumpUntil(Func<bool> condition)
    {
        const int MaxIterations = 2000;

        for (var i = 0; i < MaxIterations && !condition(); i++)
        {
            var frame = new DispatcherFrame();
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => frame.Continue = false)
            );
            Dispatcher.PushFrame(frame);
        }

        if (!condition())
        {
            throw new InvalidOperationException(
                "Dispatcher pump exceeded its iteration bound without satisfying the condition."
            );
        }
    }

    private static (Window Window, TestableCalendarDatePicker Picker) CreateHostedPicker()
    {
        var picker = new TestableCalendarDatePicker();

        var window = new Window
        {
            Content = picker,
            Width = 200,
            Height = 80,
            Left = -32000,
            Top = -32000,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
        };
        window.Show();
        window.Activate();

        return (window, picker);
    }

    private sealed class TestableCalendarDatePicker : CalendarDatePickerControl
    {
        public int PopupOpenedCount { get; private set; }

        public UIElement? LastOpenedCalendarChild { get; private set; }

        public int ClickInvocations { get; private set; }

        protected override void OnPopupOpened(object? sender, EventArgs e)
        {
            base.OnPopupOpened(sender, e);
            PopupOpenedCount++;
            LastOpenedCalendarChild = (sender as Popup)?.Child;

            if (sender is Popup popup)
            {
                WindowActivationGuard.TrackPopupOpened(popup);
            }
        }

        protected override void OnClick()
        {
            base.OnClick();
            ClickInvocations++;
        }
    }
}

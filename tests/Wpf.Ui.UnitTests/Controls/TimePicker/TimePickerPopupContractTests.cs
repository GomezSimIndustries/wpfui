// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using Wpf.Ui.UnitTests.TestHelpers;
using ClockIdentifierValue = Wpf.Ui.Controls.ClockIdentifier;
using TimePickerControl = Wpf.Ui.Controls.TimePicker;

namespace Wpf.Ui.UnitTests.Controls;

public class TimePickerPopupContractTests
{
    [Fact]
    public void Clicking_OpensAndClosesThePopup()
    {
        WpfTestHost.Invoke(() =>
        {
            (Window window, TestableTimePicker picker) = CreateHostedPicker();

            try
            {
                Popup popup = Part<Popup>(picker, "PART_Popup");

                picker.PerformClick();
                PumpUntil(() => popup.IsOpen);

                picker.PerformClick();
                PumpUntil(() => !popup.IsOpen);

                Assert.False(popup.IsOpen);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SettingIsTimePickerOpen_BeforeAnyClick_OpensThePopup()
    {
        WpfTestHost.Invoke(() =>
        {
            (Window window, TestableTimePicker picker) = CreateHostedPicker();

            try
            {
                Popup popup = Part<Popup>(picker, "PART_Popup");

                picker.SetCurrentValue(TimePickerControl.IsTimePickerOpenProperty, true);
                PumpUntil(() => popup.IsOpen);

                Assert.True(picker.IsTimePickerOpen);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ClosingThePopupFromOutside_ClearsIsTimePickerOpen()
    {
        WpfTestHost.Invoke(() =>
        {
            (Window window, TestableTimePicker picker) = CreateHostedPicker();

            try
            {
                Popup popup = Open(picker);

                popup.SetCurrentValue(Popup.IsOpenProperty, false);
                PumpUntil(() => !picker.IsTimePickerOpen);

                Assert.False(picker.IsTimePickerOpen);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Opening_WithASelection_PreselectsEachColumn()
    {
        WpfTestHost.Invoke(() =>
        {
            (Window window, TestableTimePicker picker) = CreateHostedPicker();

            try
            {
                picker.ClockIdentifier = ClockIdentifierValue.Clock12Hour;
                picker.SelectedTime = new TimeSpan(13, 45, 0);

                Popup popup = Open(picker);

                Assert.Equal(1, Part<Selector>(picker, "PART_HourSelector").SelectedIndex);
                Assert.Equal(45, Part<Selector>(picker, "PART_MinuteSelector").SelectedIndex);
                Assert.Equal(1, Part<Selector>(picker, "PART_DesignatorSelector").SelectedIndex);
                Assert.True(popup.IsOpen);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Opening_WithNoSelection_StartsAtTheFirstEntryOfEachColumn()
    {
        WpfTestHost.Invoke(() =>
        {
            (Window window, TestableTimePicker picker) = CreateHostedPicker();

            try
            {
                picker.ClockIdentifier = ClockIdentifierValue.Clock12Hour;

                _ = Open(picker);

                Assert.Equal(0, Part<Selector>(picker, "PART_HourSelector").SelectedIndex);
                Assert.Equal(0, Part<Selector>(picker, "PART_MinuteSelector").SelectedIndex);
                Assert.Equal(0, Part<Selector>(picker, "PART_DesignatorSelector").SelectedIndex);
                Assert.Null(picker.SelectedTime);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Opening_MovesKeyboardFocusIntoTheHourColumn()
    {
        WpfTestHost.Invoke(() =>
        {
            (Window window, TestableTimePicker picker) = CreateHostedPicker();

            try
            {
                _ = Open(picker);

                Selector hours = Part<Selector>(picker, "PART_HourSelector");
                PumpUntil(() => hours.IsKeyboardFocusWithin);

                Assert.True(hours.IsKeyboardFocusWithin);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Theory]
    [InlineData(3, 30, 1, 15, 30)]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(0, 0, 1, 12, 0)]
    [InlineData(11, 59, 1, 23, 59)]
    public void Accepting_InTwelveHourMode_CommitsTheColumnsAsATimeOfDay(
        int hourIndex,
        int minuteIndex,
        int designatorIndex,
        int expectedHour,
        int expectedMinute
    )
    {
        WpfTestHost.Invoke(() =>
        {
            (Window window, TestableTimePicker picker) = CreateHostedPicker();

            try
            {
                picker.ClockIdentifier = ClockIdentifierValue.Clock12Hour;

                Popup popup = Open(picker);

                Part<Selector>(picker, "PART_HourSelector").SelectedIndex = hourIndex;
                Part<Selector>(picker, "PART_MinuteSelector").SelectedIndex = minuteIndex;
                Part<Selector>(picker, "PART_DesignatorSelector").SelectedIndex = designatorIndex;

                Part<ButtonBase>(picker, "PART_AcceptButton")
                    .RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                PumpUntil(() => !popup.IsOpen);

                Assert.Equal(new TimeSpan(expectedHour, expectedMinute, 0), picker.SelectedTime);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Accepting_InTwentyFourHourMode_CommitsTheHourColumnDirectly()
    {
        WpfTestHost.Invoke(() =>
        {
            (Window window, TestableTimePicker picker) = CreateHostedPicker();

            try
            {
                picker.ClockIdentifier = ClockIdentifierValue.Clock24Hour;

                Popup popup = Open(picker);

                Part<Selector>(picker, "PART_HourSelector").SelectedIndex = 23;
                Part<Selector>(picker, "PART_MinuteSelector").SelectedIndex = 5;

                Part<ButtonBase>(picker, "PART_AcceptButton")
                    .RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                PumpUntil(() => !popup.IsOpen);

                Assert.Equal(new TimeSpan(23, 5, 0), picker.SelectedTime);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void PressingEnter_CommitsTheColumnsAndClosesThePopup()
    {
        WpfTestHost.Invoke(() =>
        {
            (Window window, TestableTimePicker picker) = CreateHostedPicker();

            try
            {
                picker.ClockIdentifier = ClockIdentifierValue.Clock24Hour;

                Popup popup = Open(picker);

                Part<Selector>(picker, "PART_HourSelector").SelectedIndex = 7;
                Part<Selector>(picker, "PART_MinuteSelector").SelectedIndex = 20;

                RaisePreviewKeyDown(popup.Child, Key.Enter);
                PumpUntil(() => !popup.IsOpen);

                Assert.Equal(new TimeSpan(7, 20, 0), picker.SelectedTime);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void PressingEscape_ClosesThePopupAndKeepsTheEarlierSelection()
    {
        WpfTestHost.Invoke(() =>
        {
            (Window window, TestableTimePicker picker) = CreateHostedPicker();

            try
            {
                picker.ClockIdentifier = ClockIdentifierValue.Clock24Hour;
                picker.SelectedTime = new TimeSpan(6, 15, 0);

                Popup popup = Open(picker);

                Part<Selector>(picker, "PART_HourSelector").SelectedIndex = 7;
                Part<Selector>(picker, "PART_MinuteSelector").SelectedIndex = 20;

                RaisePreviewKeyDown(popup.Child, Key.Escape);
                PumpUntil(() => !popup.IsOpen);

                Assert.Equal(new TimeSpan(6, 15, 0), picker.SelectedTime);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Dismissing_ClosesThePopupAndKeepsTheEarlierSelection()
    {
        WpfTestHost.Invoke(() =>
        {
            (Window window, TestableTimePicker picker) = CreateHostedPicker();

            try
            {
                picker.ClockIdentifier = ClockIdentifierValue.Clock24Hour;
                picker.SelectedTime = new TimeSpan(6, 15, 0);

                Popup popup = Open(picker);

                Part<Selector>(picker, "PART_HourSelector").SelectedIndex = 7;

                Part<ButtonBase>(picker, "PART_DismissButton")
                    .RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                PumpUntil(() => !popup.IsOpen);

                Assert.Equal(new TimeSpan(6, 15, 0), picker.SelectedTime);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void Unloading_WhileOpen_ClosesThePopup()
    {
        WpfTestHost.Invoke(() =>
        {
            (Window window, TestableTimePicker picker) = CreateHostedPicker();

            try
            {
                Popup popup = Open(picker);

                picker.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
                PumpUntil(() => !popup.IsOpen);

                Assert.False(popup.IsOpen);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void MinuteSelector_OffersOnlyTheIncrementGrid()
    {
        WpfTestHost.Invoke(() =>
        {
            (Window window, TestableTimePicker picker) = CreateHostedPicker();

            try
            {
                picker.MinuteIncrement = 15;

                _ = Open(picker);

                Selector minutes = Part<Selector>(picker, "PART_MinuteSelector");

                Assert.Equal(4, minutes.Items.Count);
                Assert.Equal("45", minutes.Items[3]);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ReopeningAfterACommit_PreselectsTheCommittedTime()
    {
        WpfTestHost.Invoke(() =>
        {
            (Window window, TestableTimePicker picker) = CreateHostedPicker();

            try
            {
                picker.ClockIdentifier = ClockIdentifierValue.Clock24Hour;

                Popup popup = Open(picker);

                Part<Selector>(picker, "PART_HourSelector").SelectedIndex = 18;
                Part<Selector>(picker, "PART_MinuteSelector").SelectedIndex = 40;

                Part<ButtonBase>(picker, "PART_AcceptButton")
                    .RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                PumpUntil(() => !popup.IsOpen);

                _ = Open(picker);

                Assert.Equal(18, Part<Selector>(picker, "PART_HourSelector").SelectedIndex);
                Assert.Equal(40, Part<Selector>(picker, "PART_MinuteSelector").SelectedIndex);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static Popup Open(TestableTimePicker picker)
    {
        Popup popup = Part<Popup>(picker, "PART_Popup");

        picker.PerformClick();
        PumpUntil(() => popup.IsOpen);
        WindowActivationGuard.AssertPopupCompliant(popup);

        return popup;
    }

    private static T Part<T>(Control control, string name)
        where T : class
    {
        return control.Template.FindName(name, control) as T
            ?? throw new InvalidOperationException($"Template part '{name}' is missing.");
    }

    private static void RaisePreviewKeyDown(UIElement element, Key key)
    {
        KeyboardDevice device = InputManager.Current.PrimaryKeyboardDevice;

        element.RaiseEvent(
            new KeyEventArgs(device, PresentationSource.FromVisual(element), 0, key)
            {
                RoutedEvent = Keyboard.PreviewKeyDownEvent,
            }
        );
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

    private static (Window Window, TestableTimePicker Picker) CreateHostedPicker()
    {
        var picker = new TestableTimePicker();

        var window = new Window
        {
            Content = picker,
            Width = 300,
            Height = 120,
            Left = -32000,
            Top = -32000,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
        };
        window.Show();
        window.Activate();
        _ = picker.ApplyTemplate();

        return (window, picker);
    }

    private sealed class TestableTimePicker : TimePickerControl
    {
        public TestableTimePicker()
        {
            SetResourceReference(StyleProperty, typeof(TimePickerControl));
        }

        public void PerformClick()
        {
            OnClick();
        }
    }
}

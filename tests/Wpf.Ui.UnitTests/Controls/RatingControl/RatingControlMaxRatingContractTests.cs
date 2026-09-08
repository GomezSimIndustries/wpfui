// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Linq;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class RatingControlMaxRatingContractTests
{
    [Fact]
    public void Default_RendersFiveStars()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, rating) = CreateHostedRatingControl();

            try
            {
                Assert.Equal(5, StarsPanel(rating).Children.Count);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void MaxRating_NonDefault_RendersThatManyStars(int maxRating)
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, rating) = CreateHostedRatingControl();

            try
            {
                rating.MaxRating = maxRating;

                Assert.Equal(maxRating, StarsPanel(rating).Children.Count);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void MaxRating_SetToZeroOrNegative_CoercesToOne(int requested)
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, rating) = CreateHostedRatingControl();

            try
            {
                rating.MaxRating = requested;

                Assert.Equal(1, rating.MaxRating);
                Assert.Single(StarsPanel(rating).Children);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ReducingMaxRatingBelowTheCurrentValue_ClampsValueDownToTheNewMaximum()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, rating) = CreateHostedRatingControl();

            try
            {
                rating.MaxRating = 10;
                rating.Value = 8;

                rating.MaxRating = 3;

                Assert.Equal(3.0D, rating.Value);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SettingValueAboveMaxRating_ClampsToMaxRating()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, rating) = CreateHostedRatingControl();

            try
            {
                rating.MaxRating = 3;
                rating.Value = 99;

                Assert.Equal(3.0D, rating.Value);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void OnKeyUp_AtTheMaximum_WithANonDefaultMaxRating_DoesNotExceedIt()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, rating) = CreateHostedRatingControl();

            try
            {
                rating.MaxRating = 3;
                rating.Value = 3;

                RaiseKeyUp(rating, Key.Right);
                RaiseKeyUp(rating, Key.Up);

                Assert.Equal(3.0D, rating.Value);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void OnKeyUp_AtTheMinimum_DoesNotGoBelowZero()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, rating) = CreateHostedRatingControl();

            try
            {
                rating.MaxRating = 3;
                rating.Value = 0;

                RaiseKeyUp(rating, Key.Left);
                RaiseKeyUp(rating, Key.Down);

                Assert.Equal(0.0D, rating.Value);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void OnKeyUp_HalfStarEnabled_WithANonDefaultMaxRating_ReachesEveryHalfStep()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, rating) = CreateHostedRatingControl();

            try
            {
                rating.MaxRating = 3;
                rating.Value = 0;
                rating.HalfStarEnabled = true;

                for (var step = 0; step < 20; step++)
                {
                    RaiseKeyUp(rating, Key.Right);
                }

                Assert.Equal(3.0D, rating.Value);

                for (var step = 0; step < 20; step++)
                {
                    RaiseKeyUp(rating, Key.Left);
                }

                Assert.Equal(0.0D, rating.Value);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void OnKeyUp_HalfStarDisabled_WithANonDefaultMaxRating_StepsByWholeStars()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, rating) = CreateHostedRatingControl();

            try
            {
                rating.MaxRating = 3;
                rating.Value = 0;
                rating.HalfStarEnabled = false;

                RaiseKeyUp(rating, Key.Right);

                Assert.Equal(1.0D, rating.Value);

                for (var step = 0; step < 10; step++)
                {
                    RaiseKeyUp(rating, Key.Right);
                }

                Assert.Equal(3.0D, rating.Value);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ValueChanged_RaisedWithTheControlAsSender_WhenTheValueSettles()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, rating) = CreateHostedRatingControl();

            try
            {
                rating.MaxRating = 3;

                object? raisedSender = null;
                var raisedCount = 0;
                rating.ValueChanged += (sender, _) =>
                {
                    raisedSender = sender;
                    raisedCount++;
                };

                rating.Value = 2;

                Assert.Equal(1, raisedCount);
                Assert.Same(rating, raisedSender);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void SettingAHalfStepValue_WithANonDefaultMaxRating_RendersTheFinalStarHalfFilledAndTheRestFilled()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, rating) = CreateHostedRatingControl();

            try
            {
                rating.MaxRating = 3;
                rating.Value = 2.5D;

                var stars = StarsPanel(rating).Children.Cast<SymbolIcon>().ToList();

                Assert.Equal(3, stars.Count);
                Assert.True(stars[0].Filled);
                Assert.True(stars[1].Filled);
                Assert.False(stars[2].Filled);
                Assert.Equal(SymbolRegular.StarHalf28, stars[2].Symbol);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void RaisingMaxRating_WhileAStarIsHalfFilled_PreservesTheHalfFillOnTheOriginalStar()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, rating) = CreateHostedRatingControl();

            try
            {
                rating.MaxRating = 3;
                rating.Value = 1.5D;

                rating.MaxRating = 10;

                var stars = StarsPanel(rating).Children.Cast<SymbolIcon>().ToList();

                Assert.Equal(10, stars.Count);
                Assert.True(stars[0].Filled);
                Assert.False(stars[1].Filled);
                Assert.Equal(SymbolRegular.StarHalf28, stars[1].Symbol);
                Assert.False(stars[2].Filled);
                Assert.Equal(SymbolRegular.Star28, stars[2].Symbol);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void AutomationPeer_ExposesMaxRatingAsTheRangeMaximum()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, rating) = CreateHostedRatingControl();

            try
            {
                rating.MaxRating = 7;

                var peer = UIElementAutomationPeer.CreatePeerForElement(rating);
                var rangeValueProvider = (IRangeValueProvider)peer!.GetPattern(PatternInterface.RangeValue)!;

                Assert.Equal(7.0D, rangeValueProvider.Maximum);
                Assert.Equal(0.0D, rangeValueProvider.Minimum);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void AutomationPeer_SetValue_UpdatesTheControlValueWithinTheMaxRatingBounds()
    {
        WpfTestHost.Invoke(() =>
        {
            var (window, rating) = CreateHostedRatingControl();

            try
            {
                rating.MaxRating = 3;

                var peer = UIElementAutomationPeer.CreatePeerForElement(rating);
                var rangeValueProvider = (IRangeValueProvider)peer!.GetPattern(PatternInterface.RangeValue)!;

                rangeValueProvider.SetValue(2.5D);

                Assert.Equal(2.5D, rating.Value);
                Assert.Equal(2.5D, rangeValueProvider.Value);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static Grid StarsPanel(RatingControl rating) =>
        (Grid)rating.Template.FindName("PART_StarsContainer", rating);

    private static void RaiseKeyUp(UIElement element, Key key)
    {
        var device = InputManager.Current.PrimaryKeyboardDevice;
        element.RaiseEvent(new KeyEventArgs(device, PresentationSource.FromVisual(element), 0, key) { RoutedEvent = Keyboard.KeyUpEvent });
    }

    private static (Window Window, RatingControl Rating) CreateHostedRatingControl(double width = 200)
    {
        var rating = new RatingControl();

        var window = new Window
        {
            Content = rating,
            Width = width,
            Height = 80,
            Left = -5000,
            Top = -5000,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
        };
        window.Show();
        window.UpdateLayout();

        return (window, rating);
    }
}

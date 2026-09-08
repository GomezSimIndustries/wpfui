// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class NavigationViewAutoSuggestBoxAndBreadcrumbBarSwapTests
{
    [Fact]
    public void SwappingAutoSuggestBox_StopsThePreviousBoxFromTriggeringNavigation()
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new RecordingNavigationView();
            var first = new AutoSuggestBox();
            var second = new AutoSuggestBox();

            navigationView.AutoSuggestBox = first;
            navigationView.AutoSuggestBox = second;

            first.RaiseEvent(
                new AutoSuggestBoxSuggestionChosenEventArgs(AutoSuggestBox.SuggestionChosenEvent, first)
                {
                    SelectedItem = "target",
                }
            );

            Assert.Equal(0, navigationView.NavigateToMenuItemCallCount);
        });
    }

    [Fact]
    public void SwappingAutoSuggestBox_LeavesTheCurrentBoxTriggeringNavigation()
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new RecordingNavigationView();
            var first = new AutoSuggestBox();
            var second = new AutoSuggestBox();

            navigationView.AutoSuggestBox = first;
            navigationView.AutoSuggestBox = second;

            second.RaiseEvent(
                new AutoSuggestBoxSuggestionChosenEventArgs(AutoSuggestBox.SuggestionChosenEvent, second)
                {
                    SelectedItem = "target",
                }
            );

            Assert.True(navigationView.NavigateToMenuItemCallCount > 0);
        });
    }

    [Fact]
    public void SwappingBreadcrumbBar_StopsThePreviousBarFromTriggeringNavigation()
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new RecordingNavigationView();
            var first = new BreadcrumbBar();
            var second = new BreadcrumbBar();

            navigationView.BreadcrumbBar = first;
            navigationView.BreadcrumbBar = second;

            first.RaiseEvent(
                new BreadcrumbBarItemClickedEventArgs(BreadcrumbBar.ItemClickedEvent, first, "target", 0)
            );

            Assert.Equal(0, navigationView.BreadcrumbBarItemClickedCount);
        });
    }

    [Fact]
    public void SwappingBreadcrumbBar_LeavesTheCurrentBarTriggeringNavigation()
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new RecordingNavigationView();
            var first = new BreadcrumbBar();
            var second = new BreadcrumbBar();

            navigationView.BreadcrumbBar = first;
            navigationView.BreadcrumbBar = second;

            second.RaiseEvent(
                new BreadcrumbBarItemClickedEventArgs(BreadcrumbBar.ItemClickedEvent, second, "target", 0)
            );

            Assert.Equal(1, navigationView.BreadcrumbBarItemClickedCount);
        });
    }

    private sealed class RecordingNavigationView : NavigationView
    {
        public int NavigateToMenuItemCallCount { get; private set; }

        public int BreadcrumbBarItemClickedCount { get; private set; }

        protected override bool NavigateToMenuItemFromAutoSuggestBox(IEnumerable list, string selectedSuggestBoxItem)
        {
            NavigateToMenuItemCallCount++;
            return false;
        }

        protected override void BreadcrumbBarOnItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs e)
        {
            BreadcrumbBarItemClickedCount++;
        }
    }
}

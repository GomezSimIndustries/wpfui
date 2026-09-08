// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections;
using System.Collections.Generic;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class NavigationViewAutoSuggestBoxSuggestionSynchronizationTests
{
    [Fact]
    public void AddingAMenuItemAfterAttachment_MakesItReachableByQuerySubmit()
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new RecordingNavigationView();
            var box = new AutoSuggestBox();
            navigationView.AutoSuggestBox = box;

            navigationView.MenuItems.Add(BuildItem("Telemetry", "telemetry"));

            SubmitQuery(box, "Telemetry");

            Assert.Equal(new[] { "Telemetry" }, navigationView.NavigatedContents);
        });
    }

    [Fact]
    public void RemovingAMenuItem_StopsItFromMatchingAQuerySubmit()
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new RecordingNavigationView();
            var box = new AutoSuggestBox();
            navigationView.AutoSuggestBox = box;

            var item = BuildItem("Telemetry", "telemetry");
            navigationView.MenuItems.Add(item);
            navigationView.MenuItems.Remove(item);

            SubmitQuery(box, "Telemetry");

            Assert.Empty(navigationView.NavigatedContents);
        });
    }

    [Fact]
    public void ReplacingAMenuItem_MatchesTheRenamedContentAndNotTheOldContent()
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new RecordingNavigationView();
            var box = new AutoSuggestBox();
            navigationView.AutoSuggestBox = box;

            navigationView.MenuItems.Add(BuildItem("Telemetry", "telemetry"));
            navigationView.MenuItems[0] = BuildItem("Diagnostics", "diagnostics");

            SubmitQuery(box, "Telemetry");
            Assert.Empty(navigationView.NavigatedContents);

            SubmitQuery(box, "Diagnostics");
            Assert.Equal(new[] { "Diagnostics" }, navigationView.NavigatedContents);
        });
    }

    [Fact]
    public void RepeatedMenuChanges_NeverProduceDuplicateMatchesForTheSameQuery()
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new RecordingNavigationView();
            var box = new AutoSuggestBox();
            navigationView.AutoSuggestBox = box;

            for (var i = 0; i < 3; i++)
            {
                var churnedItem = BuildItem("Telemetry", $"telemetry-{i}");
                navigationView.MenuItems.Add(churnedItem);
                navigationView.MenuItems.Remove(churnedItem);
            }

            navigationView.MenuItems.Add(BuildItem("Telemetry", "telemetry-final"));

            SubmitQuery(box, "Telemetry");

            Assert.Equal(new[] { "Telemetry" }, navigationView.NavigatedContents);
        });
    }

    private static NavigationViewItem BuildItem(string content, string tag) =>
        new()
        {
            Content = content,
            TargetPageTag = tag,
            TargetPageType = typeof(Page),
        };

    private static void SubmitQuery(AutoSuggestBox box, string queryText) =>
        box.RaiseEvent(
            new AutoSuggestBoxQuerySubmittedEventArgs(AutoSuggestBox.QuerySubmittedEvent, box)
            {
                QueryText = queryText,
            }
        );

    private sealed class RecordingNavigationView : NavigationView
    {
        public List<string> NavigatedContents { get; } = [];

        protected override bool NavigateToMenuItemFromAutoSuggestBox(
            IEnumerable list,
            string selectedSuggestBoxItem
        )
        {
            var matched = base.NavigateToMenuItemFromAutoSuggestBox(list, selectedSuggestBoxItem);

            if (matched)
            {
                NavigatedContents.Add(selectedSuggestBoxItem);
            }

            return matched;
        }
    }
}

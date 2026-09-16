// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class NavigationViewHistoryTransactionTests
{
    [Fact]
    public void CanceledRequest_DoesNotResumeAfterSelectingTheCurrentItem()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationView navigationView = CreateAppliedNavigationView();
            AddItems(navigationView, out NavigationViewItem itemA, out NavigationViewItem itemB, out _);
            NavigateAndPump(navigationView, "a");
            object? origin = navigationView.CurrentContent;
            Func<bool>? resume = null;
            bool cancel = true;
            navigationView.Navigating += (_, e) =>
            {
                if (cancel)
                {
                    e.Cancel = true;
                    resume = e.ResumeNavigation;
                }
            };

            Assert.False(navigationView.Navigate(itemB.Id));
            Assert.False(navigationView.Navigate(itemA.Id));
            cancel = false;
            Assert.False(resume!());
            Assert.Same(origin, navigationView.CurrentContent);
        });
    }

    [Fact]
    public void CanceledRequest_ResumesExactItemAndPayloadOnce()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationView navigationView = CreateAppliedNavigationView();
            AddItems(navigationView, out _, out NavigationViewItem itemB, out _);
            NavigateAndPump(navigationView, "a");
            var payload = new object();
            Func<bool>? resume = null;
            bool cancel = true;
            navigationView.Navigating += (_, e) =>
            {
                if (cancel)
                {
                    e.Cancel = true;
                    resume = e.ResumeNavigation;
                    Assert.Same(payload, e.TargetDataContext);
                }
            };

            Assert.False(navigationView.Navigate(itemB.Id, payload));
            cancel = false;
            Assert.NotNull(resume);
            Assert.True(resume());
            PumpDispatcher();

            Assert.Same(itemB, navigationView.SelectedItem);
            Assert.Same(payload, ((Page)navigationView.CurrentContent!).DataContext);
            Assert.False(resume());
        });
    }

    [Fact]
    public void CanceledHistoryRequests_ResumeBackAndForwardWithoutLosingEntries()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationView navigationView = CreateAppliedNavigationView();
            AddItems(navigationView, out _, out NavigationViewItem itemB, out NavigationViewItem itemC);
            NavigateAndPump(navigationView, "a");
            NavigateAndPump(navigationView, "b");
            NavigateAndPump(navigationView, "c");
            Func<bool>? resume = null;
            bool cancel = true;
            navigationView.Navigating += (_, e) =>
            {
                if (cancel)
                {
                    e.Cancel = true;
                    resume = e.ResumeNavigation;
                }
            };

            Assert.False(navigationView.GoBack());
            cancel = false;
            Assert.True(resume!());
            PumpDispatcher();
            Assert.Same(itemB, navigationView.SelectedItem);
            Assert.True(navigationView.CanGoForward);

            cancel = true;
            Assert.False(navigationView.GoForward());
            cancel = false;
            Assert.True(resume!());
            PumpDispatcher();
            Assert.Same(itemC, navigationView.SelectedItem);
            Assert.False(navigationView.CanGoForward);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanceledHistoryRequest_RejectsChangedTargetContext(bool backwards)
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationView navigationView = CreateAppliedNavigationView();
            AddItems(navigationView, out NavigationViewItem itemA, out NavigationViewItem itemB, out _);
            NavigateAndPump(navigationView, "a");
            NavigateAndPump(navigationView, "b");
            if (!backwards)
            {
                Assert.True(navigationView.GoBack());
                PumpDispatcher();
            }

            object? origin = navigationView.CurrentContent;
            Func<bool>? resume = null;
            navigationView.Navigating += (_, e) => { e.Cancel = true; resume = e.ResumeNavigation; };
            Assert.False(backwards ? navigationView.GoBack() : navigationView.GoForward());
            (backwards ? itemA : itemB).TargetDataContext = new object();
            Assert.False(resume!());
            Assert.Same(origin, navigationView.CurrentContent);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanceledRequest_DoesNotResumeAfterTargetRemovalOrNewNavigation(bool removeTarget)
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationView navigationView = CreateAppliedNavigationView();
            AddItems(navigationView, out _, out NavigationViewItem itemB, out NavigationViewItem itemC);
            NavigateAndPump(navigationView, "a");
            Func<bool>? resume = null;
            bool cancel = true;
            navigationView.Navigating += (_, e) =>
            {
                if (cancel)
                {
                    e.Cancel = true;
                    resume = e.ResumeNavigation;
                }
            };

            Assert.False(navigationView.Navigate(itemB.Id));
            cancel = false;
            if (removeTarget)
            {
                navigationView.MenuItems.Remove(itemB);
            }
            else
            {
                NavigateAndPump(navigationView, "c");
            }

            Assert.False(resume!());
            if (!removeTarget)
            {
                Assert.Same(itemC, navigationView.SelectedItem);
            }
        });
    }

    [Fact]
    public void GoBack_WhenNavigatingIsCanceled_LeavesJournalAndForwardStackUnchanged()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationView navigationView = CreateAppliedNavigationView();
            AddItems(navigationView, out _, out _, out _);
            NavigateAndPump(navigationView, "a");
            NavigateAndPump(navigationView, "b");
            NavigateAndPump(navigationView, "c");

            object? contentBeforeAttempt = navigationView.CurrentContent;
            bool cancelNavigation = true;
            navigationView.Navigating += (_, e) => e.Cancel = cancelNavigation;

            Assert.False(navigationView.GoBack());
            PumpDispatcher();

            Assert.False(navigationView.CanGoForward);
            Assert.True(navigationView.CanGoBack);
            Assert.Same(contentBeforeAttempt, navigationView.CurrentContent);

            cancelNavigation = false;
            Assert.True(navigationView.GoBack());
            PumpDispatcher();

            Assert.True(navigationView.CanGoForward);
            Assert.NotSame(contentBeforeAttempt, navigationView.CurrentContent);
        });
    }

    [Fact]
    public void GoForward_WhenNavigatingIsCanceled_LeavesForwardStackUnchanged()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationView navigationView = CreateAppliedNavigationView();
            AddItems(navigationView, out _, out _, out _);
            NavigateAndPump(navigationView, "a");
            NavigateAndPump(navigationView, "b");
            NavigateAndPump(navigationView, "c");

            Assert.True(navigationView.GoBack());
            PumpDispatcher();

            object? contentBeforeAttempt = navigationView.CurrentContent;
            bool cancelNavigation = true;
            navigationView.Navigating += (_, e) => e.Cancel = cancelNavigation;

            Assert.False(navigationView.GoForward());
            PumpDispatcher();

            Assert.True(navigationView.CanGoForward);
            Assert.Same(contentBeforeAttempt, navigationView.CurrentContent);

            cancelNavigation = false;
            Assert.True(navigationView.GoForward());
            PumpDispatcher();

            Assert.False(navigationView.CanGoForward);
            Assert.NotSame(contentBeforeAttempt, navigationView.CurrentContent);
        });
    }

    [Fact]
    public void GoBack_WhenTheTargetItemIsRemovedDuringTheRequest_ReturnsFalseWithoutThrowing()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationView navigationView = CreateAppliedNavigationView();
            AddItems(navigationView, out NavigationViewItem itemA, out _, out _);
            NavigateAndPump(navigationView, "a");
            NavigateAndPump(navigationView, "b");

            navigationView.BackRequested += (_, _) => navigationView.MenuItems.Remove(itemA);

            Assert.False(navigationView.GoBack());
            PumpDispatcher();

            Assert.False(navigationView.CanGoBack);
            Assert.False(navigationView.CanGoForward);
        });
    }

    [Fact]
    public void GoForward_WhenTheStackedItemWasRemoved_SkipsItAndReachesTheNextLiveEntry()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationView navigationView = CreateAppliedNavigationView();
            AddItems(navigationView, out _, out NavigationViewItem itemB, out NavigationViewItem itemC);
            NavigateAndPump(navigationView, "a");
            NavigateAndPump(navigationView, "b");
            NavigateAndPump(navigationView, "c");

            Assert.True(navigationView.GoBack());
            PumpDispatcher();
            Assert.True(navigationView.GoBack());
            PumpDispatcher();

            navigationView.MenuItems.Remove(itemB);

            Assert.True(navigationView.GoForward());
            PumpDispatcher();

            Assert.Equal(itemC, navigationView.SelectedItem);
            Assert.False(navigationView.CanGoForward);
        });
    }

    private static void NavigateAndPump(NavigationView navigationView, string tag)
    {
        Assert.True(navigationView.Navigate(tag));
        PumpDispatcher();
    }

    private static void AddItems(
        NavigationView navigationView,
        out NavigationViewItem itemA,
        out NavigationViewItem itemB,
        out NavigationViewItem itemC)
    {
        itemA = CreateItem("a", "A");
        itemB = CreateItem("b", "B");
        itemC = CreateItem("c", "C");
        navigationView.MenuItems.Add(itemA);
        navigationView.MenuItems.Add(itemB);
        navigationView.MenuItems.Add(itemC);
    }

    private static void PumpDispatcher() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);

    private static NavigationViewItem CreateItem(string tag, string content) =>
        new()
        {
            TargetPageTag = tag,
            Content = content,
            TargetPageType = typeof(Page),
        };

    private static NavigationView CreateAppliedNavigationView()
    {
        NavigationView navigationView = new() { Template = BuildTemplate() };
        navigationView.ApplyTemplate();
        return navigationView;
    }

    private static ControlTemplate BuildTemplate()
    {
        const string xaml = """
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                              xmlns:controls="clr-namespace:Wpf.Ui.Controls;assembly=Wpf.Ui"
                              TargetType="{x:Type controls:NavigationView}">
                <Grid>
                    <Button x:Name="PART_ToggleButton" />
                    <Button x:Name="PART_BackButton" />
                    <ItemsControl x:Name="PART_MenuItemsItemsControl" />
                    <ItemsControl x:Name="PART_FooterMenuItemsItemsControl" />
                    <controls:NavigationViewContentPresenter x:Name="PART_NavigationViewContentPresenter" />
                </Grid>
            </ControlTemplate>
            """;

        return (ControlTemplate)XamlReader.Parse(xaml);
    }
}

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

public class NavigationViewPageAssociationEvictionTests
{
    [Fact]
    public void RemovingMenuItem_StopsRoutingHeaderContentToTheRemovedItem()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationView navigationView = CreateAppliedNavigationView();
            NavigationViewItem item = CreateItem("device-1", "Device 1");
            navigationView.MenuItems.Add(item);

            Assert.True(navigationView.Navigate("device-1"));
            PumpDispatcher();
            Page page = Assert.IsType<Page>(navigationView.CurrentContent);

            NavigationView.SetHeaderContent(page, "Before removal");
            Assert.Equal("Before removal", item.Content);

            navigationView.MenuItems.Remove(item);
            NavigationView.SetHeaderContent(page, "After removal");

            Assert.Equal("Before removal", item.Content);
        });
    }

    [Fact]
    public void ReplacingMenuItem_StopsRoutingHeaderContentToTheReplacedItem()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationView navigationView = CreateAppliedNavigationView();
            NavigationViewItem item = CreateItem("device-1", "Device 1");
            navigationView.MenuItems.Add(item);

            Assert.True(navigationView.Navigate("device-1"));
            PumpDispatcher();
            Page page = Assert.IsType<Page>(navigationView.CurrentContent);

            NavigationView.SetHeaderContent(page, "Before replace");
            Assert.Equal("Before replace", item.Content);

            navigationView.MenuItems[0] = CreateItem("device-1-b", "Device 1 (reconnected)");
            NavigationView.SetHeaderContent(page, "After replace");

            Assert.Equal("Before replace", item.Content);
        });
    }

    [Fact]
    public void ResettingMenuItems_StopsRoutingHeaderContentToAClearedItem()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationView navigationView = CreateAppliedNavigationView();
            NavigationViewItem item = CreateItem("device-1", "Device 1");
            navigationView.MenuItems.Add(item);

            Assert.True(navigationView.Navigate("device-1"));
            PumpDispatcher();
            Page page = Assert.IsType<Page>(navigationView.CurrentContent);

            NavigationView.SetHeaderContent(page, "Before reset");
            Assert.Equal("Before reset", item.Content);

            navigationView.MenuItems.Clear();
            NavigationView.SetHeaderContent(page, "After reset");

            Assert.Equal("Before reset", item.Content);
        });
    }

    [Fact]
    public void RemovingMenuItem_PreservesHeaderRoutingForALiveItemSharingItsPageType()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationView navigationView = CreateAppliedNavigationView();
            NavigationViewItem itemA = CreateItem("device-a", "Device A");
            NavigationViewItem itemB = CreateItem("device-b", "Device B");
            navigationView.MenuItems.Add(itemA);
            navigationView.MenuItems.Add(itemB);

            Assert.True(navigationView.Navigate("device-a"));
            PumpDispatcher();
            Assert.IsType<Page>(navigationView.CurrentContent);

            Assert.True(navigationView.Navigate("device-b"));
            PumpDispatcher();
            Page pageB = Assert.IsType<Page>(navigationView.CurrentContent);

            navigationView.MenuItems.Remove(itemA);

            NavigationView.SetHeaderContent(pageB, "Still routed");

            Assert.Equal("Still routed", itemB.Content);
        });
    }

    [Fact]
    public void ReconnectingTheSameTag_AfterRemoval_NavigatesToTheNewItemsPage()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationView navigationView = CreateAppliedNavigationView();
            NavigationViewItem original = CreateItem("device-1", "Device 1");
            navigationView.MenuItems.Add(original);

            Assert.True(navigationView.Navigate("device-1"));
            PumpDispatcher();
            Page originalPage = Assert.IsType<Page>(navigationView.CurrentContent);

            navigationView.MenuItems.Remove(original);

            NavigationViewItem reconnected = CreateItem("device-1", "Device 1");
            navigationView.MenuItems.Add(reconnected);

            Assert.True(navigationView.Navigate("device-1"));
            PumpDispatcher();
            Page reconnectedPage = Assert.IsType<Page>(navigationView.CurrentContent);

            Assert.NotSame(originalPage, reconnectedPage);

            NavigationView.SetHeaderContent(reconnectedPage, "Reconnected header");

            Assert.Equal("Reconnected header", reconnected.Content);
            Assert.Equal("Device 1", original.Content);
        });
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

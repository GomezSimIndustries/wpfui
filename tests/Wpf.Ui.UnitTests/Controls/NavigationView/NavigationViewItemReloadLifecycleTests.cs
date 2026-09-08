// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class NavigationViewItemReloadLifecycleTests
{
    [Fact]
    public void MutatingMenuItems_AfterUnloadReload_StillUpdatesHasMenuItems()
    {
        WpfTestHost.Invoke(() =>
        {
            var item = new NavigationViewItem();

            item.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            item.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            item.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            item.MenuItems.Add(new NavigationViewItem());

            Assert.True(item.HasMenuItems);
        });
    }

    [Fact]
    public void MutatingExternalMenuItemsSource_AfterUnloadReload_StillSyncsIntoMenuItems()
    {
        WpfTestHost.Invoke(() =>
        {
            var item = new NavigationViewItem();
            var source = new ObservableCollection<object>();
            item.MenuItemsSource = source;

            item.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            item.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            item.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            source.Add("added-after-reload");

            Assert.Contains("added-after-reload", (IEnumerable<object>)item.MenuItems);
        });
    }

    [Fact]
    public void RepeatedUnloadReloadCycles_DoNotAccumulateExternalSourceSubscriptions()
    {
        WpfTestHost.Invoke(() =>
        {
            var item = new NavigationViewItem();
            var source = new ObservableCollection<object>();
            item.MenuItemsSource = source;

            for (var i = 0; i < 4; i++)
            {
                item.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                item.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            }

            item.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            source.Add("added-once");

            Assert.Single(item.MenuItems);
            Assert.Contains("added-once", (IEnumerable<object>)item.MenuItems);
        });
    }
}

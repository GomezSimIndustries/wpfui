// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class NavigationViewNavigationLifecycleFailedTests
{
    [Fact]
    public void Navigate_WhenTheEnteringCallbackThrows_RaisesNavigationLifecycleFailedOnTheView()
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new NavigationView { Template = BuildTemplate() };
            navigationView.ApplyTemplate();

            List<NavigationLifecycleFailedEventArgs> failures = [];
            navigationView.NavigationLifecycleFailed += (_, args) => failures.Add(args);

            _ = navigationView.Navigate(typeof(ThrowingPage));
            PumpDispatcher();

            NavigationLifecycleFailedEventArgs reported = Assert.Single(failures);
            Assert.IsType<ThrowingPage>(reported.Target);
            Assert.True(reported.IsNavigatingTo);
            Assert.Equal("entry failed", reported.Exception.Message);
        });
    }

    private static void PumpDispatcher() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);

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

    private sealed class ThrowingPage : Page, INavigationAware
    {
        public Task OnNavigatedToAsync() =>
            throw new System.InvalidOperationException("entry failed");

        public Task OnNavigatedFromAsync() => Task.CompletedTask;
    }
}

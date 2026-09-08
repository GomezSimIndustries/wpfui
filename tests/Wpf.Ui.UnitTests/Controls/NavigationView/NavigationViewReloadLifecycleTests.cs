// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Markup;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class NavigationViewReloadLifecycleTests
{
    [Fact]
    public void Unload_DetachesTheActualBackButtonClickHandler()
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new CountingNavigationView { Template = BuildTemplate() };
            navigationView.ApplyTemplate();
            var backButton = navigationView.ExposedBackButton!;

            navigationView.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            navigationView.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

            backButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));

            Assert.Equal(0, navigationView.BackButtonClickCount);
        });
    }

    [Fact]
    public void Navigate_ByTag_WorksAgainAfterAnUnloadReloadCycle()
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new NavigationView();
            navigationView.MenuItems.Add(
                new NavigationViewItem
                {
                    TargetPageTag = "home",
                    Content = "Home",
                    TargetPageType = typeof(Page),
                }
            );

            navigationView.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            navigationView.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            navigationView.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            Assert.True(navigationView.Navigate("home"));
        });
    }

    [Fact]
    public void ToggleButtonClick_AfterRepeatedUnloadReloadCycles_FiresExactlyOncePerClick()
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new CountingNavigationView { Template = BuildTemplate() };
            navigationView.ApplyTemplate();

            for (var i = 0; i < 4; i++)
            {
                navigationView.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                navigationView.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            }

            navigationView.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            navigationView.ExposedToggleButton!.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));

            Assert.Equal(1, navigationView.ToggleButtonClickCount);
        });
    }

    [Fact]
    public void OnApplyTemplate_Reapplied_TheRetainedOldToggleButtonNoLongerTogglesWhileTheNewOneDoes()
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new CountingNavigationView { Template = BuildTemplate() };
            navigationView.ApplyTemplate();
            var oldToggleButton = navigationView.ExposedToggleButton!;

            navigationView.Template = BuildTemplate();
            navigationView.ApplyTemplate();
            var newToggleButton = navigationView.ExposedToggleButton!;

            Assert.NotSame(oldToggleButton, newToggleButton);

            oldToggleButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Assert.Equal(0, navigationView.ToggleButtonClickCount);

            newToggleButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Assert.Equal(1, navigationView.ToggleButtonClickCount);
        });
    }

    [Fact]
    public void OnApplyTemplate_Reapplied_TheRetainedOldBackButtonNoLongerNavigatesWhileTheNewOneDoes()
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new CountingNavigationView { Template = BuildTemplate() };
            navigationView.ApplyTemplate();
            var oldBackButton = navigationView.ExposedBackButton!;

            navigationView.Template = BuildTemplate();
            navigationView.ApplyTemplate();
            var newBackButton = navigationView.ExposedBackButton!;

            Assert.NotSame(oldBackButton, newBackButton);

            oldBackButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Assert.Equal(0, navigationView.BackButtonClickCount);

            newBackButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Assert.Equal(1, navigationView.BackButtonClickCount);
        });
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

    private sealed class CountingNavigationView : NavigationView
    {
        public int ToggleButtonClickCount { get; private set; }

        public int BackButtonClickCount { get; private set; }

        public System.Windows.Controls.Button? ExposedBackButton => BackButton;

        public System.Windows.Controls.Button? ExposedToggleButton => ToggleButton;

        protected override void OnToggleButtonClick(object sender, RoutedEventArgs e)
        {
            ToggleButtonClickCount++;
            base.OnToggleButtonClick(sender, e);
        }

        protected override void OnBackButtonClick(object sender, RoutedEventArgs e)
        {
            BackButtonClickCount++;
            base.OnBackButtonClick(sender, e);
        }
    }
}

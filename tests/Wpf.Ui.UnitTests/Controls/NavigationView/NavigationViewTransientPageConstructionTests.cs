// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class NavigationViewTransientPageConstructionTests
{
    [Fact]
    public void Navigate_ToATypeAbsentFromTheMenu_ConstructsThePageOnceAndDeliversTitleParameterAndCallback()
    {
        WpfTestHost.Invoke(() =>
        {
            ExposingNavigationView navigationView = CreateAppliedNavigationView();
            var provider = new CountingPageProvider();
            navigationView.SetPageProviderService(provider);
            var parameter = new object();

            bool navigated = navigationView.Navigate(typeof(CountingParameterAwarePage), parameter);
            PumpDispatcher();

            Assert.True(navigated);
            Assert.Equal(1, provider.ConstructionCount);
            CountingParameterAwarePage page = Assert.Single(provider.CreatedInstances);
            Assert.Same(page, navigationView.CurrentContent);
            Assert.Equal(1, page.NavigatedToCallCount);
            Assert.Same(parameter, page.LastParameter);

            INavigationViewItem? resolvedItem = navigationView.ResolvedItemFor(typeof(CountingParameterAwarePage));
            Assert.NotNull(resolvedItem);
            Assert.Equal(CountingParameterAwarePage.ExpectedTitle, resolvedItem!.Content);
        });
    }

    [Fact]
    public void Navigate_ToAListedItem_ConstructsThePageExactlyOnce()
    {
        WpfTestHost.Invoke(() =>
        {
            ExposingNavigationView navigationView = CreateAppliedNavigationView();
            var provider = new CountingPageProvider();
            navigationView.SetPageProviderService(provider);
            var item = new NavigationViewItem
            {
                TargetPageTag = "listed",
                Content = "Listed",
                TargetPageType = typeof(CountingParameterAwarePage),
            };
            navigationView.MenuItems.Add(item);

            bool navigated = navigationView.Navigate("listed");
            PumpDispatcher();

            Assert.True(navigated);
            Assert.Equal(1, provider.ConstructionCount);
            Assert.Same(Assert.Single(provider.CreatedInstances), navigationView.CurrentContent);
        });
    }

    [Fact]
    public void Navigate_ToACachedItemTwice_ReusesTheFirstInstanceWithoutConstructingAgain()
    {
        WpfTestHost.Invoke(() =>
        {
            ExposingNavigationView navigationView = CreateAppliedNavigationView();
            var provider = new CountingPageProvider();
            navigationView.SetPageProviderService(provider);
            var cachedItem = new NavigationViewItem
            {
                TargetPageTag = "cached",
                Content = "Cached",
                TargetPageType = typeof(CountingParameterAwarePage),
                NavigationCacheMode = NavigationCacheMode.Enabled,
            };
            var otherItem = new NavigationViewItem
            {
                TargetPageTag = "other",
                Content = "Other",
                TargetPageType = typeof(Page),
            };
            navigationView.MenuItems.Add(cachedItem);
            navigationView.MenuItems.Add(otherItem);

            Assert.True(navigationView.Navigate("cached"));
            PumpDispatcher();
            Assert.True(navigationView.Navigate("other"));
            PumpDispatcher();
            Assert.True(navigationView.Navigate("cached"));
            PumpDispatcher();

            Assert.Equal(1, provider.ConstructionCount);
            Assert.Same(Assert.Single(provider.CreatedInstances), navigationView.CurrentContent);
        });
    }

    private static void PumpDispatcher() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);

    private static ExposingNavigationView CreateAppliedNavigationView()
    {
        ExposingNavigationView navigationView = new() { Template = BuildTemplate() };
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

    private sealed class ExposingNavigationView : NavigationView
    {
        public INavigationViewItem? ResolvedItemFor(Type pageType) =>
            PageTypeNavigationViewsDictionary.TryGetValue(pageType, out INavigationViewItem? item) ? item : null;
    }

    private sealed class CountingPageProvider : INavigationViewPageProvider
    {
        public int ConstructionCount { get; private set; }

        public List<CountingParameterAwarePage> CreatedInstances { get; } = [];

        public object? GetPage(Type pageType)
        {
            if (pageType != typeof(CountingParameterAwarePage))
            {
                return new Page();
            }

            ConstructionCount++;
            var page = new CountingParameterAwarePage();
            CreatedInstances.Add(page);
            return page;
        }
    }

    private sealed class CountingParameterAwarePage : Page, INavigationParameterAware
    {
        public const string ExpectedTitle = "Counting Page Title";

        public CountingParameterAwarePage() => Title = ExpectedTitle;

        public int NavigatedToCallCount { get; private set; }

        public object? LastParameter { get; private set; }

        public Task OnNavigatedToAsync(object? parameter)
        {
            NavigatedToCallCount++;
            LastParameter = parameter;
            return Task.CompletedTask;
        }

        public Task OnNavigatedToAsync() => Task.CompletedTask;

        public Task OnNavigatedFromAsync() => Task.CompletedTask;
    }
}

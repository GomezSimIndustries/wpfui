// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Controls;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Controls;

namespace Wpf.Ui.UnitTests.Controls;

public sealed class NavigationWindowContractTests
{
    [Fact]
    public void ExplicitGetNavigation_ReturnsTheSameControlAsThePublicMethod()
    {
        StaThread.Run(() =>
        {
            var window = new FixtureNavigationWindow();
            INavigationWindow navigationWindow = window;

            Assert.Same(window.GetNavigation(), navigationWindow.GetNavigation());
        });
    }

    [Fact]
    public void SetServiceProvider_ResolvesPagesThroughTheProvidedServiceProvider()
    {
        StaThread.Run(() =>
        {
            var window = new FixtureNavigationWindow();
            INavigationWindow navigationWindow = window;
            var probePage = new ProbePage();
            var serviceProvider = new RecordingServiceProvider(probePage);

            navigationWindow.SetServiceProvider(serviceProvider);

            bool replaced = navigationWindow.GetNavigation().ReplaceContent(typeof(ProbePage));

            Assert.True(replaced);
            Assert.Equal(1, serviceProvider.CallCount);
        });
    }

    private sealed class FixtureNavigationWindow : INavigationWindow
    {
        public NavigationView RootNavigation { get; } = new();

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) =>
            RootNavigation.SetPageProviderService(navigationViewPageProvider);

        public void ShowWindow() { }

        public void CloseWindow() { }

        INavigationView INavigationWindow.GetNavigation() => RootNavigation;

        public void SetServiceProvider(IServiceProvider serviceProvider) =>
            RootNavigation.SetServiceProvider(serviceProvider);
    }

    private sealed class ProbePage : Page { }

    private sealed class RecordingServiceProvider(object result) : IServiceProvider
    {
        public int CallCount { get; private set; }

        public object? GetService(Type serviceType)
        {
            CallCount++;
            return result;
        }
    }

    private static class StaThread
    {
        public static void Run(Action action)
        {
            ExceptionDispatchInfo? capturedException = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    capturedException = ExceptionDispatchInfo.Capture(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            capturedException?.Throw();
        }
    }
}

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class NavigationViewContentPresenterLifecycleTests
{
    [Fact]
    public void Navigate_WhileTheLeavingPageCallbackIsPending_HoldsTheEnteringPageCallback()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationViewContentPresenter presenter = CreatePresenter();
            List<string> log = [];
            TaskCompletionSource<bool> leaveGate = CreateGate();
            var first = new RecordingPage(log, "first") { LeaveGate = leaveGate };
            var second = new RecordingPage(log, "second");

            Navigate(presenter, first);
            Navigate(presenter, second);

            Assert.Equal(new[] { "first:to-start", "first:to-end", "first:from-start" }, log);

            leaveGate.SetResult(true);
            PumpDispatcher();

            Assert.Equal(
                new[]
                {
                    "first:to-start",
                    "first:to-end",
                    "first:from-start",
                    "first:from-end",
                    "second:to-start",
                    "second:to-end",
                },
                log
            );
        });
    }

    [Fact]
    public void Navigate_TwiceWhileTheLeavingPageCallbackIsPending_NeverEntersTheSupersededPage()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationViewContentPresenter presenter = CreatePresenter();
            List<string> log = [];
            TaskCompletionSource<bool> leaveGate = CreateGate();
            var first = new RecordingPage(log, "first") { LeaveGate = leaveGate };
            var superseded = new RecordingPage(log, "superseded");
            var last = new RecordingPage(log, "last");

            Navigate(presenter, first);
            Navigate(presenter, superseded);
            Navigate(presenter, last);

            leaveGate.SetResult(true);
            PumpDispatcher();

            Assert.Equal(
                new[]
                {
                    "first:to-start",
                    "first:to-end",
                    "first:from-start",
                    "first:from-end",
                    "last:to-start",
                    "last:to-end",
                },
                log
            );
            Assert.Same(last, presenter.Content);
        });
    }

    [Fact]
    public void Unloaded_RaisedTwiceAfterANavigation_NotifiesTheEnteredPageAboutLeavingOnce()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationViewContentPresenter presenter = CreatePresenter();
            List<string> log = [];
            var page = new RecordingPage(log, "page");

            Navigate(presenter, page);

            presenter.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            PumpDispatcher();
            presenter.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            PumpDispatcher();

            Assert.Equal(
                new[] { "page:to-start", "page:to-end", "page:from-start", "page:from-end" },
                log
            );
        });
    }

    [Fact]
    public void Navigate_WhenTheViewAndItsDataContextAreBothAware_RunsTheViewCallbackBeforeTheDataContextCallback()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationViewContentPresenter presenter = CreatePresenter();
            List<string> log = [];
            TaskCompletionSource<bool> entryGate = CreateGate();
            var viewModel = new RecordingViewModel(log, "viewModel");
            var view = new RecordingPage(log, "view") { EntryGate = entryGate, DataContext = viewModel };

            Navigate(presenter, view);

            Assert.Equal(new[] { "view:to-start" }, log);

            entryGate.SetResult(true);
            PumpDispatcher();

            Assert.Equal(
                new[] { "view:to-start", "view:to-end", "viewModel:to-start", "viewModel:to-end" },
                log
            );
        });
    }

    [Fact]
    public void Navigate_WhenTheEnteringCallbackThrows_ReportsTheFailureAndStillLeavesThePageLater()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationViewContentPresenter presenter = CreatePresenter();
            List<string> log = [];
            List<NavigationLifecycleFailedEventArgs> failures = [];
            presenter.NavigationLifecycleFailed += (_, args) => failures.Add(args);
            var failure = new InvalidOperationException("entry failed");
            var first = new RecordingPage(log, "first") { EntryFailure = failure };
            var second = new RecordingPage(log, "second");

            Navigate(presenter, first);

            NavigationLifecycleFailedEventArgs reported = Assert.Single(failures);
            Assert.Same(first, reported.Target);
            Assert.Same(failure, reported.Exception);
            Assert.True(reported.IsNavigatingTo);

            Navigate(presenter, second);

            Assert.Equal(
                new[]
                {
                    "first:to-start",
                    "first:to-throw",
                    "first:from-start",
                    "first:from-end",
                    "second:to-start",
                    "second:to-end",
                },
                log
            );
        });
    }

    [Fact]
    public void NavigationLifecycleCompletion_WithAPendingLeaveCallback_CompletesOnlyOnceTheCallbackFinishes()
    {
        WpfTestHost.Invoke(() =>
        {
            NavigationViewContentPresenter presenter = CreatePresenter();
            List<string> log = [];
            TaskCompletionSource<bool> leaveGate = CreateGate();
            var first = new RecordingPage(log, "first") { LeaveGate = leaveGate };
            var second = new RecordingPage(log, "second");

            Navigate(presenter, first);
            Assert.True(presenter.NavigationLifecycleCompletion.IsCompleted);

            Navigate(presenter, second);
            Assert.False(presenter.NavigationLifecycleCompletion.IsCompleted);

            leaveGate.SetResult(true);
            PumpDispatcher();

            Assert.True(presenter.NavigationLifecycleCompletion.IsCompleted);
        });
    }

    private static TaskCompletionSource<bool> CreateGate() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static NavigationViewContentPresenter CreatePresenter()
    {
        NavigationViewContentPresenter presenter = new() { TransitionDuration = 0 };
        presenter.BeginInit();
        presenter.EndInit();
        return presenter;
    }

    private static void Navigate(NavigationViewContentPresenter presenter, object content)
    {
        _ = presenter.Navigate(content);
        PumpDispatcher();
    }

    private static void PumpDispatcher() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);

    private sealed class RecordingPage : Page, INavigationAware
    {
        private readonly List<string> _log;
        private readonly string _name;

        public RecordingPage(List<string> log, string name)
        {
            _log = log;
            _name = name;
        }

        public TaskCompletionSource<bool>? EntryGate { get; init; }

        public TaskCompletionSource<bool>? LeaveGate { get; init; }

        public Exception? EntryFailure { get; init; }

        public async Task OnNavigatedToAsync()
        {
            _log.Add($"{_name}:to-start");

            if (EntryGate is not null)
            {
                _ = await EntryGate.Task;
            }

            if (EntryFailure is not null)
            {
                _log.Add($"{_name}:to-throw");
                throw EntryFailure;
            }

            _log.Add($"{_name}:to-end");
        }

        public async Task OnNavigatedFromAsync()
        {
            _log.Add($"{_name}:from-start");

            if (LeaveGate is not null)
            {
                _ = await LeaveGate.Task;
            }

            _log.Add($"{_name}:from-end");
        }
    }

    private sealed class RecordingViewModel : INavigationAware
    {
        private readonly List<string> _log;
        private readonly string _name;

        public RecordingViewModel(List<string> log, string name)
        {
            _log = log;
            _name = name;
        }

        public Task OnNavigatedToAsync()
        {
            _log.Add($"{_name}:to-start");
            _log.Add($"{_name}:to-end");
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync()
        {
            _log.Add($"{_name}:from-start");
            _log.Add($"{_name}:from-end");
            return Task.CompletedTask;
        }
    }
}

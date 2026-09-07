// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

public class SnackbarPresenter : System.Windows.Controls.ContentPresenter
{
    private readonly TimeProvider _timeProvider;

    private long _presentationGeneration;

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "WpfAnalyzers.DependencyProperty",
        "WPF0012:CLR property type should match registered type",
        Justification = "seems harmless"
    )]
    public new Snackbar? Content
    {
        get => (Snackbar?)GetValue(ContentProperty);
        protected set => SetValue(ContentProperty, value);
    }

    public SnackbarPresenter()
        : this(TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SnackbarPresenter"/> class using the given time source for its display and fade delays.</summary>
    /// <param name="timeProvider">The source of the display timeout and fade delays.</param>
    public SnackbarPresenter(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        Unloaded += static (sender, _) =>
        {
            var self = (SnackbarPresenter)sender;
            self.OnUnloaded();
        };
    }

    protected Queue<Snackbar> Queue { get; } = new();

    protected CancellationTokenSource CancellationTokenSource { get; set; } = new();

    protected virtual void OnUnloaded()
    {
        Queue.Clear();

        if (CancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        ImmediatelyHideCurrent();
        ResetCancellationTokenSource();
    }

    private void ImmediatelyHideCurrent()
    {
        if (Content is null)
        {
            return;
        }

        CancellationTokenSource.Cancel();
        _presentationGeneration++;
        ImmediatelyHidSnackbar(Content);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "WpfAnalyzers.DependencyProperty",
        "WPF0041:Set mutable dependency properties using SetCurrentValue",
        Justification = "SetCurrentValue(ContentProperty, ...) will not work"
    )]
    private void ImmediatelyHidSnackbar(Snackbar snackbar)
    {
        snackbar.SetCurrentValue(Snackbar.IsShownProperty, false);
        Content = null;
    }

    protected void ResetCancellationTokenSource()
    {
        CancellationTokenSource.Dispose();
        CancellationTokenSource = new CancellationTokenSource();
    }

    public virtual void AddToQue(Snackbar snackbar)
    {
        Queue.Enqueue(snackbar);

        if (Content is null)
        {
            _ = ShowQueuedSnackbarsAsync(); // TODO: Fix detached process
        }
    }

    public virtual async Task ImmediatelyDisplay(Snackbar snackbar)
    {
        await HideCurrent();
        await ShowSnackbar(snackbar);

        await ShowQueuedSnackbarsAsync();
    }

    public virtual async Task HideCurrent()
    {
        if (Content is null)
        {
            return;
        }

        CancellationTokenSource.Cancel();
        long generation = ++_presentationGeneration;
        await HidSnackbar(Content, generation);

        if (generation == _presentationGeneration)
        {
            ResetCancellationTokenSource();
        }
    }

    private async Task ShowQueuedSnackbarsAsync()
    {
        while (Queue.Count > 0 && !CancellationTokenSource.IsCancellationRequested)
        {
            Snackbar snackbar = Queue.Dequeue();

            await ShowSnackbar(snackbar);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "WpfAnalyzers.DependencyProperty",
        "WPF0041:Set mutable dependency properties using SetCurrentValue",
        Justification = "SetCurrentValue(ContentProperty, ...) will not work"
    )]
    private async Task ShowSnackbar(Snackbar snackbar)
    {
        long generation = ++_presentationGeneration;

        Content = snackbar;

        snackbar.SetCurrentValue(Snackbar.IsShownProperty, true);

        try
        {
            await Task.Delay(snackbar.Timeout, _timeProvider, CancellationTokenSource.Token);
        }
        catch
        {
            return;
        }

        await HidSnackbar(snackbar, generation);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "WpfAnalyzers.DependencyProperty",
        "WPF0041:Set mutable dependency properties using SetCurrentValue",
        Justification = "SetCurrentValue(ContentProperty, ...) will not work"
    )]
    private async Task HidSnackbar(Snackbar snackbar, long generation)
    {
        snackbar.SetCurrentValue(Snackbar.IsShownProperty, false);

        await Task.Delay(TimeSpan.FromMilliseconds(300), _timeProvider);

        if (generation == _presentationGeneration)
        {
            Content = null;
        }
    }

    ~SnackbarPresenter()
    {
        if (!CancellationTokenSource.IsCancellationRequested)
        {
            CancellationTokenSource.Cancel();
        }

        CancellationTokenSource.Dispose();
    }
}

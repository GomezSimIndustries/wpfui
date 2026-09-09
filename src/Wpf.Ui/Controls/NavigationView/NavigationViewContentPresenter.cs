// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

/* Based on Windows UI Library */

using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Animations;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

public class NavigationViewContentPresenter : Frame
{
    /// <summary>
    /// Stores the content that received an entry notification and has not yet been notified about leaving.
    /// </summary>
    private object? _enteredContent;

    private Task _lifecycleCompletion = Task.CompletedTask;

    private int _lifecycleGeneration;

    /// <summary>Identifies the <see cref="TransitionDuration"/> dependency property.</summary>
    public static readonly DependencyProperty TransitionDurationProperty = DependencyProperty.Register(
        nameof(TransitionDuration),
        typeof(int),
        typeof(NavigationViewContentPresenter),
        new FrameworkPropertyMetadata(200)
    );

    /// <summary>Identifies the <see cref="Transition"/> dependency property.</summary>
    public static readonly DependencyProperty TransitionProperty = DependencyProperty.Register(
        nameof(Transition),
        typeof(Transition),
        typeof(NavigationViewContentPresenter),
        new FrameworkPropertyMetadata(Transition.FadeInWithSlide)
    );

    /// <summary>Identifies the <see cref="IsDynamicScrollViewerEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsDynamicScrollViewerEnabledProperty =
        DependencyProperty.Register(
            nameof(IsDynamicScrollViewerEnabled),
            typeof(bool),
            typeof(NavigationViewContentPresenter),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure)
        );

    /// <summary>Identifies the <see cref="ContentScrollViewerVisibility"/> dependency property.</summary>
    public static readonly DependencyProperty ContentScrollViewerVisibilityProperty =
        DependencyProperty.Register(
            nameof(ContentScrollViewerVisibility),
            typeof(ContentScrollViewerVisibility),
            typeof(NavigationViewContentPresenter),
            new FrameworkPropertyMetadata(
                ContentScrollViewerVisibility.Auto,
                FrameworkPropertyMetadataOptions.AffectsMeasure
            )
        );

    [Bindable(true)]
    [Category("Appearance")]
    public int TransitionDuration
    {
        get => (int)GetValue(TransitionDurationProperty);
        set => SetValue(TransitionDurationProperty, value);
    }

    /// <summary>
    /// Gets or sets type of <see cref="NavigationViewContentPresenter"/> transitions during navigation.
    /// </summary>
    public Transition Transition
    {
        get => (Transition)GetValue(TransitionProperty);
        set => SetValue(TransitionProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dynamic scroll viewer is enabled.
    /// </summary>
    public bool IsDynamicScrollViewerEnabled
    {
        get => (bool)GetValue(IsDynamicScrollViewerEnabledProperty);
        protected set => SetValue(IsDynamicScrollViewerEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets the visibility behavior of the internal ScrollViewer.
    /// When set to <see cref="ContentScrollViewerVisibility.Auto"/>, the ScrollViewer visibility
    /// is automatically determined based on the page's ScrollViewer.CanContentScroll property.
    /// When set to <see cref="ContentScrollViewerVisibility.Hidden"/>, pages can provide their own scrolling.
    /// </summary>
    public ContentScrollViewerVisibility ContentScrollViewerVisibility
    {
        get => (ContentScrollViewerVisibility)GetValue(ContentScrollViewerVisibilityProperty);
        set => SetValue(ContentScrollViewerVisibilityProperty, value);
    }

    /// <summary>
    /// Gets or sets the pending navigation parameter to be passed to INavigationParameterAware pages.
    /// This is set before navigation and consumed during the OnNavigated callback.
    /// </summary>
    internal object? PendingNavigationParameter { get; set; }

    /// <summary>Occurs when an <see cref="Wpf.Ui.Abstractions.Controls.INavigationAware"/> callback throws.</summary>
    public event EventHandler<NavigationLifecycleFailedEventArgs>? NavigationLifecycleFailed;

    /// <summary>Gets a task that completes when the queued navigation lifecycle callbacks have run.</summary>
    /// <remarks>Read this property on the thread that owns the presenter; every navigation replaces it.</remarks>
    public Task NavigationLifecycleCompletion => _lifecycleCompletion;

    static NavigationViewContentPresenter()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(NavigationViewContentPresenter),
            new FrameworkPropertyMetadata(typeof(NavigationViewContentPresenter))
        );

        NavigationUIVisibilityProperty.OverrideMetadata(
            typeof(NavigationViewContentPresenter),
            new FrameworkPropertyMetadata(NavigationUIVisibility.Hidden)
        );

        SandboxExternalContentProperty.OverrideMetadata(
            typeof(NavigationViewContentPresenter),
            new FrameworkPropertyMetadata(true)
        );

        JournalOwnershipProperty.OverrideMetadata(
            typeof(NavigationViewContentPresenter),
            new FrameworkPropertyMetadata(JournalOwnership.UsesParentJournal)
        );

        if (
            ScrollViewer.CanContentScrollProperty.GetMetadata(typeof(Page))
            == ScrollViewer.CanContentScrollProperty.DefaultMetadata
        )
        {
            ScrollViewer.CanContentScrollProperty.OverrideMetadata(
                typeof(Page),
                new FrameworkPropertyMetadata(true)
            );
        }
    }

    public NavigationViewContentPresenter()
    {
        Navigating += static (sender, eventArgs) =>
        {
            if (eventArgs.Content is null)
            {
                return;
            }

            var self = (NavigationViewContentPresenter)sender;
            self.OnNavigating(eventArgs);
        };

        Navigated += static (sender, eventArgs) =>
        {
            var self = (NavigationViewContentPresenter)sender;

            if (eventArgs.Content is null)
            {
                return;
            }

            self.OnNavigated(eventArgs);
        };
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        // Notify current content that it's being navigated away from when the presenter is unloaded
        Unloaded += static (sender, _) =>
        {
            if (sender is NavigationViewContentPresenter navigator)
            {
                navigator.EnqueueLifecycleTransition(target: null, parameter: null);
            }
        };
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.ChangedButton is MouseButton.XButton1 or MouseButton.XButton2)
        {
            e.Handled = true;
            return;
        }

        base.OnMouseDown(e);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    protected virtual void OnNavigating(System.Windows.Navigation.NavigatingCancelEventArgs eventArgs) { }

    protected virtual void OnNavigated(NavigationEventArgs eventArgs)
    {
        // Queue the leave notification and then the entry notification for the new content
        if (eventArgs.Content is not null)
        {
            object? parameter = PendingNavigationParameter;
            PendingNavigationParameter = null;

            EnqueueLifecycleTransition(eventArgs.Content, parameter);
        }

        // Apply visual transition effect
        ApplyTransitionEffectToNavigatedPage(eventArgs.Content);

        if (eventArgs.Content is not DependencyObject dependencyObject)
        {
            return;
        }

        // Determine scroll viewer visibility based on ContentScrollViewerVisibility setting.
        // Auto mode uses the page's CanContentScroll property.
        var scrollViewerEnabled = ContentScrollViewerVisibility switch
        {
            ContentScrollViewerVisibility.Visible => true,
            ContentScrollViewerVisibility.Hidden => false,
            _ => ScrollViewer.GetCanContentScroll(dependencyObject),
        };

        SetCurrentValue(IsDynamicScrollViewerEnabledProperty, scrollViewerEnabled);
    }

    private void ApplyTransitionEffectToNavigatedPage(object content)
    {
        if (TransitionDuration < 1)
        {
            return;
        }

        _ = TransitionAnimationProvider.ApplyTransition(content, Transition, TransitionDuration);
    }

    private void EnqueueLifecycleTransition(object? target, object? parameter)
    {
        _lifecycleGeneration++;

        _lifecycleCompletion = RunLifecycleTransitionAsync(
            _lifecycleCompletion,
            _lifecycleGeneration,
            target,
            parameter
        );
    }

    private async Task RunLifecycleTransitionAsync(
        Task precedingTransitions,
        int generation,
        object? target,
        object? parameter
    )
    {
        await precedingTransitions;

        if (_enteredContent is { } leavingContent)
        {
            _enteredContent = null;

            await NotifyINavigationAwareAsync(leavingContent, parameter: null, isNavigatingTo: false);
        }

        // A later navigation replaces the generation, and its transition enters its own content
        if (target is null || generation != _lifecycleGeneration)
        {
            return;
        }

        _enteredContent = target;

        await NotifyINavigationAwareAsync(target, parameter, isNavigatingTo: true);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "ReSharper",
        "SuspiciousTypeConversion.Global",
        Justification = "The library user might make a class inherit from both FrameworkElement and INavigationAware at the same time."
    )]
    private async Task NotifyINavigationAwareAsync(object content, object? parameter, bool isNavigatingTo)
    {
        switch (content)
        {
            // A view is notified before its data context, and the two callbacks do not overlap
            case INavigationAware navigationAwareNavigationContent:
                await InvokeLifecycleCallbackAsync(
                    navigationAwareNavigationContent,
                    parameter,
                    isNavigatingTo
                );
                if (
                    navigationAwareNavigationContent
                        is FrameworkElement { DataContext: INavigationAware viewModel }
                    && !ReferenceEquals(viewModel, navigationAwareNavigationContent)
                )
                {
                    await InvokeLifecycleCallbackAsync(viewModel, parameter, isNavigatingTo);
                }

                break;
            case INavigableView<object> { ViewModel: INavigationAware navigationAwareNavigableViewViewModel }:
                await InvokeLifecycleCallbackAsync(
                    navigationAwareNavigableViewViewModel,
                    parameter,
                    isNavigatingTo
                );
                break;
            case FrameworkElement { DataContext: INavigationAware navigationAwareCurrentContent }:
                await InvokeLifecycleCallbackAsync(navigationAwareCurrentContent, parameter, isNavigatingTo);
                break;
        }
    }

    private async Task InvokeLifecycleCallbackAsync(
        INavigationAware navigationAware,
        object? parameter,
        bool isNavigatingTo
    )
    {
        try
        {
            if (!isNavigatingTo)
            {
                await navigationAware.OnNavigatedFromAsync();

                return;
            }

            // No ConfigureAwait(false) - navigation callbacks need UI thread context
            if (navigationAware is INavigationParameterAware parameterAware)
            {
                await parameterAware.OnNavigatedToAsync(parameter);

                return;
            }

            await navigationAware.OnNavigatedToAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[NavigationViewContentPresenter] INavigationAware callback failed: {ex.Message}"
            );

            NavigationLifecycleFailed?.Invoke(
                this,
                new NavigationLifecycleFailedEventArgs(navigationAware, isNavigatingTo, ex)
            );
        }
    }
}

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
    /// Stores the previous content during navigation to properly notify it about being navigated from.
    /// </summary>
    private object? _previousContent;

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
            if (sender is NavigationViewContentPresenter { Content: not null } navigator)
            {
                NotifyContentAboutNavigatedFrom(navigator.Content);
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

    protected virtual void OnNavigating(System.Windows.Navigation.NavigatingCancelEventArgs eventArgs)
    {
        // Store the current content so we can notify it in OnNavigated (after visual transition)
        if (eventArgs.Navigator is NavigationViewContentPresenter navigator)
        {
            _previousContent = navigator.Content;
        }
    }

    protected virtual void OnNavigated(NavigationEventArgs eventArgs)
    {
        // First: Notify old content about being navigated from
        // This happens AFTER the navigation is complete (page is no longer visible)
        if (_previousContent is not null)
        {
            NotifyContentAboutNavigatedFrom(_previousContent);
            _previousContent = null;
        }

        // Second: Notify new content about being navigated to
        // This happens AFTER navigation is complete (page IS visible now)
        if (eventArgs.Content is not null)
        {
            NotifyContentAboutNavigatedTo(eventArgs.Content);
        }

        // Third: Apply visual transition effect
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

    private void NotifyContentAboutNavigatedTo(object content)
    {
        var parameter = PendingNavigationParameter;
        PendingNavigationParameter = null; // Clear after use

        NotifyINavigationAware(content, parameter, isNavigatingTo: true);
    }

    private static void NotifyContentAboutNavigatedFrom(object content)
    {
        NotifyINavigationAware(content, parameter: null, isNavigatingTo: false);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "ReSharper",
        "SuspiciousTypeConversion.Global",
        Justification = "The library user might make a class inherit from both FrameworkElement and INavigationAware at the same time."
    )]
    private static void NotifyINavigationAware(object content, object? parameter, bool isNavigatingTo)
    {
        // Fire-and-forget async call with exception handling
        static async void PerformNotifyAsync(INavigationAware navigationAware, object? param, bool navigatingTo)
        {
            try
            {
                if (navigatingTo)
                {
                    // For INavigationParameterAware, call the parameterized overload
                    // Note: No ConfigureAwait(false) - navigation callbacks need UI thread context
                    if (navigationAware is INavigationParameterAware paramAware)
                    {
                        await paramAware.OnNavigatedToAsync(param);
                    }
                    else
                    {
                        await navigationAware.OnNavigatedToAsync();
                    }
                }
                else
                {
                    await navigationAware.OnNavigatedFromAsync();
                }
            }
            catch (Exception ex)
            {
                // Log to debug output - in production, consider surfacing this via an event
                System.Diagnostics.Debug.WriteLine(
                    $"[NavigationViewContentPresenter] INavigationAware callback failed: {ex.Message}"
                );
            }
        }

        switch (content)
        {
            // The order in which the OnNavigatedToAsync/OnNavigatedFromAsync methods of View and ViewModel are called
            // is not guaranteed - both are notified if both implement INavigationAware
            case INavigationAware navigationAwareNavigationContent:
                PerformNotifyAsync(navigationAwareNavigationContent, parameter, isNavigatingTo);
                if (
                    navigationAwareNavigationContent
                        is FrameworkElement { DataContext: INavigationAware viewModel }
                    && !ReferenceEquals(viewModel, navigationAwareNavigationContent)
                )
                {
                    PerformNotifyAsync(viewModel, parameter, isNavigatingTo);
                }

                break;
            case INavigableView<object> { ViewModel: INavigationAware navigationAwareNavigableViewViewModel }:
                PerformNotifyAsync(navigationAwareNavigableViewViewModel, parameter, isNavigatingTo);
                break;
            case FrameworkElement { DataContext: INavigationAware navigationAwareCurrentContent }:
                PerformNotifyAsync(navigationAwareCurrentContent, parameter, isNavigatingTo);
                break;
        }
    }
}

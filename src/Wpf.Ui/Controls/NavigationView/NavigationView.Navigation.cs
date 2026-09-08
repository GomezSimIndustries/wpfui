// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

/* Based on Windows UI Library */

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Controls;
using Wpf.Ui.Abstractions;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <content>
/// Defines navigation logic and state management for <see cref="NavigationView"/>.
/// </content>
public partial class NavigationView
{
    /// <summary>
    /// Maximum number of journal entries to keep. Older entries are automatically trimmed.
    /// </summary>
    private const int MaxJournalSize = 50;

    protected List<string> Journal { get; } = new(MaxJournalSize);

    /// <summary>
    /// Stack of entries that can be navigated forward to after going back.
    /// </summary>
    private readonly Stack<string> _forwardStack = new();

    /// <summary>
    /// Per-NavigationViewItem instance cache. Preserves page instances while their
    /// corresponding NavigationViewItem exists. Cleaned up when the item is removed.
    /// This works alongside DI - first navigation creates via DI, subsequent navigations
    /// return the cached instance.
    /// </summary>
    private readonly Dictionary<string, object?> _itemInstanceCache = [];

    protected ObservableCollection<INavigationViewItem> NavigationStack { get; } = [];

    private readonly NavigationCache _cache = new();

    private readonly Dictionary<
        INavigationViewItem,
        List<INavigationViewItem?[]>
    > _complexNavigationStackHistory = [];

    private IServiceProvider? _serviceProvider;

    private INavigationViewPageProvider? _pageService;

    private int _currentIndexInJournal;

    /// <inheritdoc />
    public bool CanGoBack => Journal.Count > 1 && _currentIndexInJournal >= 0;

    /// <summary>
    /// Gets a value indicating whether there is at least one entry in forward navigation history.
    /// </summary>
    public bool CanGoForward => _forwardStack.Count > 0;

    /// <inheritdoc />
    public void SetPageProviderService(INavigationViewPageProvider navigationViewPageProvider) =>
        _pageService = navigationViewPageProvider;

    /// <inheritdoc />
    public void SetServiceProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public virtual bool Navigate(Type pageType, object? dataContext = null)
    {
        if (
            PageTypeNavigationViewsDictionary.TryGetValue(
                pageType,
                out INavigationViewItem? navigationViewItem
            )
        )
        {
            return NavigateInternal(navigationViewItem, dataContext);
        }

        return TryToNavigateWithoutINavigationViewItem(pageType, false, dataContext);
    }

    /// <inheritdoc />
    public virtual bool Navigate(string pageIdOrTargetTag, object? dataContext = null)
    {
        if (
            PageIdOrTargetTagNavigationViewsDictionary.TryGetValue(
                pageIdOrTargetTag,
                out INavigationViewItem? navigationViewItem
            )
        )
        {
            return NavigateInternal(navigationViewItem, dataContext);
        }

        return false;
    }

    /// <inheritdoc />
    public virtual bool NavigateWithHierarchy(Type pageType, object? dataContext = null)
    {
        if (
            PageTypeNavigationViewsDictionary.TryGetValue(
                pageType,
                out INavigationViewItem? navigationViewItem
            )
        )
        {
            return NavigateInternal(navigationViewItem, dataContext, true);
        }

        return TryToNavigateWithoutINavigationViewItem(pageType, true, dataContext);
    }

    /// <inheritdoc />
    public virtual bool ReplaceContent(Type? pageTypeToEmbed)
    {
        if (pageTypeToEmbed == null)
        {
            return false;
        }

        if (_serviceProvider != null)
        {
            UpdateContent(_serviceProvider.GetService(pageTypeToEmbed));

            return true;
        }

        if (_pageService == null)
        {
            return false;
        }

        UpdateContent(_pageService.GetPage(pageTypeToEmbed));

        return true;
    }

    /// <inheritdoc />
    public virtual bool ReplaceContent(UIElement pageInstanceToEmbed, object? dataContext = null)
    {
        UpdateContent(pageInstanceToEmbed, dataContext);

        return true;
    }

    /// <inheritdoc />
    public virtual bool GoForward()
    {
        if (_forwardStack.Count == 0)
        {
            return false;
        }

        var itemId = _forwardStack.Pop();

        if (!PageIdOrTargetTagNavigationViewsDictionary.TryGetValue(itemId, out var navigationViewItem))
        {
            return false;
        }

        // Navigate without clearing forward stack (isForwardNavigated flag)
        return NavigateInternal(navigationViewItem, null, false, false, isForwardNavigated: true);
    }

    /// <inheritdoc />
    public virtual bool GoBack()
    {
        if (Journal.Count <= 1)
        {
            return false;
        }

        // Push current page to forward stack so we can navigate back to it
        var currentId = Journal[^1];
        _forwardStack.Push(currentId);

        var itemId = Journal[^2];

        OnBackRequested();
        return NavigateInternal(PageIdOrTargetTagNavigationViewsDictionary[itemId], null, false, true);
    }

    /// <inheritdoc />
    public virtual void ClearJournal()
    {
        _currentIndexInJournal = 0;

        Journal.Clear();
        _forwardStack.Clear();
        _itemInstanceCache.Clear();
        _complexNavigationStackHistory.Clear();
    }

    /// <inheritdoc />
    public virtual void ClearCache()
    {
        _cache.Clear();
        _itemInstanceCache.Clear();
    }

    /// <summary>
    /// Cleans up all navigation state associated with a NavigationViewItem.
    /// Call this when a NavigationViewItem is removed (e.g., device disconnected).
    /// </summary>
    /// <param name="item">The NavigationViewItem being removed.</param>
    internal void OnNavigationViewItemRemoved(INavigationViewItem item)
    {
        var itemId = item.Id;

        // Remove from per-item instance cache
        if (_itemInstanceCache.Remove(itemId))
        {
            Debug.WriteLine($"Removed cached instance for {itemId}");
        }

        // Remove all journal entries for this item
        _ = Journal.RemoveAll(id => id == itemId);

        // Remove from forward stack (need to rebuild without the removed item)
        if (_forwardStack.Count > 0)
        {
            var remaining = _forwardStack.Where(id => id != itemId).Reverse().ToList();
            _forwardStack.Clear();
            foreach (var id in remaining)
            {
                _forwardStack.Push(id);
            }
        }

        // Remove from navigation stack if present
        var stackItem = NavigationStack.FirstOrDefault(i => i.Id == itemId);
        if (stackItem is not null)
        {
            _ = NavigationStack.Remove(stackItem);
        }

        // Remove from complex navigation history
        _ = _complexNavigationStackHistory.Remove(item);

        // Remove page associations owned by this item, leaving associations for other live items intact
        RemovePageAssociations(item);

        // Remove from dictionaries
        _ = PageIdOrTargetTagNavigationViewsDictionary.Remove(itemId);

        if (!string.IsNullOrEmpty(item.TargetPageTag))
        {
            _ = PageIdOrTargetTagNavigationViewsDictionary.Remove(item.TargetPageTag);
        }

        if (item.TargetPageType is not null)
        {
            // Only remove from type dictionary if no other items use this type
            var otherItemsWithSameType = PageTypeNavigationViewsDictionary
                .Any(kvp => kvp.Value.Id != itemId && kvp.Key == item.TargetPageType);

            if (!otherItemsWithSameType)
            {
                _ = PageTypeNavigationViewsDictionary.Remove(item.TargetPageType);
            }
        }

        // Recalculate journal index
        _currentIndexInJournal = Math.Max(0, Journal.Count - 1);
        SetCurrentValue(IsBackEnabledProperty, CanGoBack);

        Debug.WriteLine($"Cleaned up navigation state for {itemId}");
    }

    private bool TryToNavigateWithoutINavigationViewItem(
        Type pageType,
        bool addToNavigationStack,
        object? dataContext = null
    )
    {
        var navigationViewItem = new NavigationViewItem(pageType);

        // Try to get a title from the page instance for the breadcrumb
        var pageInstance = GetNavigationItemInstance(navigationViewItem);
        if (pageInstance is Page page && !string.IsNullOrEmpty(page.Title))
        {
            navigationViewItem.Content = page.Title;
        }

        if (!NavigateInternal(navigationViewItem, dataContext, addToNavigationStack))
        {
            return false;
        }

        PageTypeNavigationViewsDictionary.Add(pageType, navigationViewItem);
        PageIdOrTargetTagNavigationViewsDictionary.Add(navigationViewItem.Id, navigationViewItem);

        return true;
    }

    private bool NavigateInternal(
        INavigationViewItem viewItem,
        object? dataContext = null,
        bool addToNavigationStack = false,
        bool isBackwardsNavigated = false,
        bool isForwardNavigated = false
    )
    {
        if (NavigationStack.Count > 0 && NavigationStack[^1] == viewItem)
        {
            return false;
        }

        // Use NavigationViewItem's TargetDataContext as fallback if no dataContext provided
        dataContext ??= viewItem.TargetDataContext;

        var pageInstance = GetNavigationItemInstance(viewItem);

        if (OnNavigating(pageInstance))
        {
            Debug.WriteLineIf(EnableDebugMessages, "Navigation canceled");

            return false;
        }

        Debug.WriteLineIf(
            EnableDebugMessages,
            $"DEBUG | {viewItem.Id} - {(string.IsNullOrEmpty(viewItem.TargetPageTag) ? "NO_TAG" : viewItem.TargetPageTag)} - {viewItem.TargetPageType} | NAVIGATED"
        );

        // Note: OnNavigated() is called in OnNavigationViewContentPresenterNavigated after Frame.Navigate() completes.
        // This ensures NavigationView.Navigated event fires AFTER the page is actually visible.

        // Set up the association before setting DataContext
        if (pageInstance is FrameworkElement frameworkElement)
        {
            // Set NavigationParent to allow HeaderContent property changes to find this NavigationView
            frameworkElement.SetValue(NavigationParentProperty, this);
        }

        // Set DataContext first so bindings can resolve
        UpdateContent(pageInstance, dataContext);

        // Apply properties after DataContext is set
        ApplyAttachedProperties(viewItem, pageInstance);

        AddToNavigationStack(viewItem, addToNavigationStack, isBackwardsNavigated);
        AddToJournal(viewItem, isBackwardsNavigated, isForwardNavigated);

        if (SelectedItem != NavigationStack[0] && NavigationStack[0].IsMenuElement)
        {
            SelectedItem = NavigationStack[0];
            OnSelectionChanged();
        }

        return true;
    }

    private void AddToJournal(INavigationViewItem viewItem, bool isBackwardsNavigated, bool isForwardNavigated = false)
    {
        if (isBackwardsNavigated)
        {
            Journal.RemoveAt(Journal.LastIndexOf(Journal[^2]));
            Journal.RemoveAt(Journal.LastIndexOf(Journal[^1]));

            _currentIndexInJournal -= 2;
        }

        // Clear forward stack when navigating to a new page (not back/forward)
        if (!isBackwardsNavigated && !isForwardNavigated)
        {
            _forwardStack.Clear();
        }

        Journal.Add(viewItem.Id);
        _currentIndexInJournal++;

        // Trim old journal entries if we exceed the maximum size
        while (Journal.Count > MaxJournalSize)
        {
            Journal.RemoveAt(0);
            _currentIndexInJournal--;
        }

        SetCurrentValue(IsBackEnabledProperty, CanGoBack);

        Debug.WriteLineIf(EnableDebugMessages, $"JOURNAL INDEX {_currentIndexInJournal}");

        if (Journal.Count > 0)
        {
            Debug.WriteLineIf(EnableDebugMessages, $"JOURNAL LAST ELEMENT {Journal[^1]}");
        }
    }

    private object GetNavigationItemInstance(INavigationViewItem viewItem)
    {
        if (viewItem.TargetPageType is null)
        {
            throw new InvalidOperationException(
                $"The {nameof(viewItem)}.{nameof(viewItem.TargetPageType)} property cannot be null."
            );
        }

        // Check per-item cache first (preserves instance while NavigationViewItem exists)
        if (viewItem.NavigationCacheMode != NavigationCacheMode.Disabled
            && _itemInstanceCache.TryGetValue(viewItem.Id, out var cachedInstance)
            && cachedInstance is not null)
        {
            Debug.WriteLine(
                $"Returning cached instance for {viewItem.Id} ({viewItem.TargetPageType.Name})"
            );
            return cachedInstance;
        }

        // Create new instance via DI, page service, or reflection
        object instance;

        if (_serviceProvider is not null)
        {
            instance = _serviceProvider.GetService(viewItem.TargetPageType)
                ?? throw new InvalidOperationException(
                    $"{nameof(_serviceProvider)}.{nameof(_serviceProvider.GetService)} returned null for type {viewItem.TargetPageType}."
                );
        }
        else if (_pageService is not null)
        {
            instance = _pageService.GetPage(viewItem.TargetPageType)
                ?? throw new InvalidOperationException(
                    $"{nameof(_pageService)}.{nameof(_pageService.GetPage)} returned null for type {viewItem.TargetPageType}."
                );
        }
        else
        {
            instance = _cache.Remember(
                    viewItem.TargetPageType,
                    viewItem.NavigationCacheMode,
                    ComputeCachedNavigationInstance
                )
                ?? throw new InvalidOperationException(
                    $"Unable to get or create instance of {viewItem.TargetPageType} from cache."
                );
        }

        // Store in per-item cache for reuse while the NavigationViewItem exists
        if (viewItem.NavigationCacheMode != NavigationCacheMode.Disabled)
        {
            _itemInstanceCache[viewItem.Id] = instance;
            Debug.WriteLine(
                $"Cached instance for {viewItem.Id} ({viewItem.TargetPageType.Name})"
            );
        }

        return instance;

        object? ComputeCachedNavigationInstance() => GetPageInstanceFromCache(viewItem.TargetPageType);
    }

    private object? GetPageInstanceFromCache(Type? targetPageType)
    {
        if (targetPageType is null)
        {
            return default;
        }

        if (_serviceProvider is not null)
        {
            Debug.WriteLine(
                $"Getting {targetPageType} from cache using IServiceProvider."
            );

            return _serviceProvider.GetService(targetPageType)
                ?? throw new InvalidOperationException(
                    $"{nameof(_serviceProvider.GetService)} returned null"
                );
        }

        if (_pageService is not null)
        {
            Debug.WriteLine(
                $"Getting {targetPageType} from cache using INavigationViewPageProvider."
            );

            return _pageService.GetPage(targetPageType)
                ?? throw new InvalidOperationException($"{nameof(_pageService.GetPage)} returned null");
        }

        Debug.WriteLine($"Getting {targetPageType} from cache using reflection.");

        return NavigationViewActivator.CreateInstance(targetPageType)
            ?? throw new InvalidOperationException("Failed to create instance of the page");
    }

    private void ApplyAttachedProperties(INavigationViewItem viewItem, object pageInstance)
    {
        if (pageInstance is FrameworkElement frameworkElement)
        {
            // Store the association between page and navigation item
            PageToNavigationItemDictionary[frameworkElement] = viewItem;

            // Apply HeaderContent if already available
            if (GetHeaderContent(frameworkElement) is { } headerContent)
            {
                viewItem.Content = headerContent;
                UpdateBreadcrumbContents();
            }
        }
    }

    /// <summary>
    /// Removes every <see cref="PageToNavigationItemDictionary"/> entry owned by the given item.
    /// </summary>
    /// <param name="item">The item whose page associations are being removed.</param>
    private void RemovePageAssociations(INavigationViewItem item)
    {
        List<FrameworkElement>? keysToRemove = null;

        foreach (KeyValuePair<FrameworkElement, INavigationViewItem> pair in PageToNavigationItemDictionary)
        {
            if (ReferenceEquals(pair.Value, item))
            {
                (keysToRemove ??= new List<FrameworkElement>()).Add(pair.Key);
            }
        }

        if (keysToRemove is null)
        {
            return;
        }

        foreach (FrameworkElement key in keysToRemove)
        {
            _ = PageToNavigationItemDictionary.Remove(key);
        }
    }

    internal void NotifyHeaderContentChanged(FrameworkElement page, object? headerContent)
    {
        if (PageToNavigationItemDictionary.TryGetValue(page, out INavigationViewItem? navItem))
        {
            navItem.Content = headerContent;
            UpdateBreadcrumbContents();
        }
    }

    private void UpdateContent(object? content, object? dataContext = null)
    {
        if (content is FrameworkElement frameworkViewContent)
        {
            // For INavigableView pages, set DataContext to the page itself
            // so bindings like {Binding ViewModel...} work immediately.
            // The navigation parameter is passed via INavigationParameterAware.OnNavigatedToAsync(parameter).
            if (content is Wpf.Ui.Abstractions.Controls.INavigableView<object>)
            {
                frameworkViewContent.DataContext = frameworkViewContent;
            }
            else if (dataContext is not null)
            {
                frameworkViewContent.DataContext = dataContext;
            }
        }

        // Store the navigation parameter for INavigationParameterAware notification
        if (NavigationViewContentPresenter is not null)
        {
            NavigationViewContentPresenter.PendingNavigationParameter = dataContext;
        }

        _ = NavigationViewContentPresenter?.Navigate(content);
    }

    private void OnNavigationViewContentPresenterNavigated(
        object sender,
        System.Windows.Navigation.NavigationEventArgs e
    )
    {
        if (sender is not Frame frame)
        {
            return;
        }

        _ = frame.RemoveBackEntry();

        // Raise NavigationView.Navigated event AFTER navigation is complete (page IS visible now)
        if (e.Content is not null)
        {
            OnNavigated(e.Content);
        }
    }

    private void AddToNavigationStack(
        INavigationViewItem viewItem,
        bool addToNavigationStack,
        bool isBackwardsNavigated
    )
    {
        if (isBackwardsNavigated)
        {
            RecreateNavigationStackFromHistory(viewItem);
        }

        if (addToNavigationStack && !NavigationStack.Contains(viewItem))
        {
            viewItem.Activate(this);
            NavigationStack.Add(viewItem);
        }

        if (!addToNavigationStack)
        {
            UpdateCurrentNavigationStackItem(viewItem);
        }

        ClearNavigationStack(viewItem);
    }

    private void UpdateCurrentNavigationStackItem(INavigationViewItem viewItem)
    {
        if (NavigationStack.Contains(viewItem))
        {
            return;
        }

        if (NavigationStack.Count > 1)
        {
            AddToNavigationStackHistory(viewItem);
        }

        if (NavigationStack.Count == 0)
        {
            viewItem.Activate(this);
            NavigationStack.Add(viewItem);
        }
        else
        {
            ReplaceThirstElementInNavigationStack(viewItem);
        }

        ClearNavigationStack(1);
    }

    private void RecreateNavigationStackFromHistory(INavigationViewItem item)
    {
        List<INavigationViewItem?[]>? historyList;
        if (!_complexNavigationStackHistory.TryGetValue(item, out historyList) || historyList.Count == 0)
        {
            return;
        }

        INavigationViewItem?[] latestHistory = historyList[^1];
        var startIndex = 0;

        if (latestHistory[0]!.IsMenuElement)
        {
            startIndex = 1;
            ReplaceThirstElementInNavigationStack(latestHistory[0]!);
        }

        for (int i = startIndex; i < latestHistory.Length; i++)
        {
            if (latestHistory[i] is null)
            {
                break;
            }

            AddToNavigationStack(latestHistory[i]!, true, false);
        }

        historyList.Remove(latestHistory);
        /*if (historyList.Count == 0)
            _complexNavigationStackHistory.Remove(item);
        */

#if NET6_0_OR_GREATER
        System.Buffers.ArrayPool<INavigationViewItem>.Shared.Return(latestHistory!, true);
#endif

        AddToNavigationStack(item, true, false);
    }

    private void AddToNavigationStackHistory(INavigationViewItem viewItem)
    {
        INavigationViewItem lastItem = NavigationStack[^1];
        var startIndex = NavigationStack.IndexOf(viewItem);

        if (startIndex < 0)
        {
            startIndex = 0;
        }

        List<INavigationViewItem?[]>? historyList;
        if (!_complexNavigationStackHistory.TryGetValue(lastItem, out historyList))
        {
            historyList = new List<INavigationViewItem?[]>(5);
            _complexNavigationStackHistory.Add(lastItem, historyList);
        }

        int arrayLength = NavigationStack.Count - 1 - startIndex;
        INavigationViewItem[] array;

        // OPTIMIZATION: Initializing an array every time well... not an ideal
#if NET6_0_OR_GREATER
        array = System.Buffers.ArrayPool<INavigationViewItem>.Shared.Rent(arrayLength);
#else
        array = new INavigationViewItem[arrayLength];
#endif

        historyList.Add(array);

        INavigationViewItem?[] latestHistory = historyList[^1];
        int i = 0;

        for (int j = startIndex; j < NavigationStack.Count - 1; j++)
        {
            latestHistory[i] = NavigationStack[j];
            i++;
        }
    }

    private void ClearNavigationStack(int navigationStackItemIndex)
    {
        var navigationStackCount = NavigationStack.Count;
        var length = navigationStackCount - navigationStackItemIndex;

        if (length == 0)
        {
            return;
        }

        for (int j = navigationStackCount - 1; j >= navigationStackCount - length; j--)
        {
            _ = NavigationStack.Remove(NavigationStack[j]);
        }
    }

    private void ClearNavigationStack(INavigationViewItem item)
    {
        var navigationStackCount = NavigationStack.Count;
        if (navigationStackCount <= 1)
        {
            return;
        }

        var index = NavigationStack.IndexOf(item);
        if (index >= navigationStackCount - 1)
        {
            return;
        }

        AddToNavigationStackHistory(item);
        ClearNavigationStack(++index);
    }

    private void ReplaceThirstElementInNavigationStack(INavigationViewItem newItem)
    {
        NavigationStack[0].Deactivate(this);
        NavigationStack[0] = newItem;
        NavigationStack[0].Activate(this);
    }
}

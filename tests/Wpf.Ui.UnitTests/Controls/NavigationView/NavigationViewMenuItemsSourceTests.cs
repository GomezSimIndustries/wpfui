// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class NavigationViewMenuItemsSourceTests
{
    public enum SourceKind
    {
        Menu,
        Footer,
        Header,
    }

    public static IEnumerable<object[]> AllSourceKinds()
    {
        yield return new object[] { SourceKind.Menu };
        yield return new object[] { SourceKind.Footer };
        yield return new object[] { SourceKind.Header };
    }

    [Theory]
    [MemberData(nameof(AllSourceKinds))]
    public void ReplacingSource_DetachesThePreviousSourceAndAttachesTheNewSourceExactlyOnce(SourceKind kind)
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new NavigationView();
            var first = new RecordingObservableSource("a");
            var second = new RecordingObservableSource("b");

            SetSource(navigationView, kind, first);
            Assert.Equal(1, first.SubscriberCount);

            SetSource(navigationView, kind, second);

            Assert.Equal(0, first.SubscriberCount);
            Assert.Equal(1, second.SubscriberCount);
        });
    }

    [Theory]
    [MemberData(nameof(AllSourceKinds))]
    public void MutatingTheReplacedSource_NoLongerChangesTheCurrentDestinationList(SourceKind kind)
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new NavigationView();
            var first = new RecordingObservableSource("a");
            var second = new RecordingObservableSource("b");

            SetSource(navigationView, kind, first);
            SetSource(navigationView, kind, second);

            first.Add("leaked");

            Assert.Equal(new object[] { "b" }, GetDestination(navigationView, kind).Cast<object>());
        });
    }

    [Theory]
    [MemberData(nameof(AllSourceKinds))]
    public void SettingSourceToNullThenReassigning_DetachesAndReattachesCleanly(SourceKind kind)
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new NavigationView();
            var first = new RecordingObservableSource("a");

            SetSource(navigationView, kind, first);
            SetSource(navigationView, kind, null);

            Assert.Equal(0, first.SubscriberCount);
            Assert.Empty(GetDestination(navigationView, kind));

            var second = new RecordingObservableSource("b");
            SetSource(navigationView, kind, second);

            Assert.Equal(1, second.SubscriberCount);
            Assert.Equal(new object[] { "b" }, GetDestination(navigationView, kind).Cast<object>());

            second.Add("c");

            Assert.Equal(new object[] { "b", "c" }, GetDestination(navigationView, kind).Cast<object>());
        });
    }

    [Theory]
    [MemberData(nameof(AllSourceKinds))]
    public void CurrentSourceCollectionActions_StillPropagateToTheDestinationList(SourceKind kind)
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new NavigationView();
            var source = new RecordingObservableSource("a", "b", "c");

            SetSource(navigationView, kind, source);
            var destination = GetDestination(navigationView, kind);

            source.Add("d");
            Assert.Equal(new object[] { "a", "b", "c", "d" }, destination.Cast<object>());

            source.RemoveAt(0);
            Assert.Equal(new object[] { "b", "c", "d" }, destination.Cast<object>());

            source.Move(0, 2);
            Assert.Equal(new object[] { "c", "d", "b" }, destination.Cast<object>());

            source.ReplaceAt(1, "e");
            Assert.Equal(new object[] { "c", "e", "b" }, destination.Cast<object>());

            source.Reset();
            Assert.Empty(destination);
        });
    }

    [Theory]
    [MemberData(nameof(AllSourceKinds))]
    public void InsertingIntoTheSource_PlacesTheItemAtTheSameIndexInTheDestination(SourceKind kind)
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new NavigationView();
            var source = new RecordingObservableSource("a", "b", "c");

            SetSource(navigationView, kind, source);
            var destination = GetDestination(navigationView, kind);

            source.Insert(1, "x");

            Assert.Equal(new object[] { "a", "x", "b", "c" }, destination.Cast<object>());
        });
    }

    [Theory]
    [MemberData(nameof(AllSourceKinds))]
    public void InsertingMultipleItemsAtOnce_PlacesThemAtTheSameStartingIndexInOrder(SourceKind kind)
    {
        WpfTestHost.Invoke(() =>
        {
            var navigationView = new NavigationView();
            var source = new RecordingObservableSource("a", "b", "c");

            SetSource(navigationView, kind, source);
            var destination = GetDestination(navigationView, kind);

            source.InsertRange(1, "x", "y");

            Assert.Equal(new object[] { "a", "x", "y", "b", "c" }, destination.Cast<object>());
        });
    }

    [Theory]
    [MemberData(nameof(AllSourceKinds))]
    public void ReplacingSource_AllowsTheNavigationViewToBeCollectedWhileThePreviousSourceSurvives(SourceKind kind)
    {
        var first = new RecordingObservableSource("a");

        WeakReference navigationViewRef = WpfTestHost.Invoke(() =>
        {
            WeakReference weak = CreateNavigationViewWithReplacedSource(first, kind);
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
            return weak;
        });

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(navigationViewRef.IsAlive);
        GC.KeepAlive(first);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateNavigationViewWithReplacedSource(
        RecordingObservableSource first,
        SourceKind kind
    )
    {
        var navigationView = new NavigationView();
        SetSource(navigationView, kind, first);
        SetSource(navigationView, kind, new RecordingObservableSource("b"));
        return new WeakReference(navigationView);
    }

    private static void SetSource(NavigationView navigationView, SourceKind kind, object? value)
    {
        switch (kind)
        {
            case SourceKind.Menu:
                navigationView.MenuItemsSource = value;
                break;
            case SourceKind.Footer:
                navigationView.FooterMenuItemsSource = value;
                break;
            case SourceKind.Header:
                navigationView.HeaderMenuItemsSource = value;
                break;
        }
    }

    private static IList GetDestination(NavigationView navigationView, SourceKind kind) =>
        kind switch
        {
            SourceKind.Menu => navigationView.MenuItems,
            SourceKind.Footer => navigationView.FooterMenuItems,
            SourceKind.Header => navigationView.HeaderMenuItems,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    private sealed class RecordingObservableSource : IEnumerable, INotifyCollectionChanged
    {
        private readonly List<object> _items;
        private NotifyCollectionChangedEventHandler? _collectionChanged;

        public RecordingObservableSource(params object[] items)
        {
            _items = new List<object>(items);
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add
            {
                _collectionChanged += value;
                SubscriberCount++;
            }
            remove
            {
                _collectionChanged -= value;
                SubscriberCount--;
            }
        }

        public int SubscriberCount { get; private set; }

        public void Add(object item)
        {
            _items.Add(item);
            _collectionChanged?.Invoke(
                this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add,
                    item,
                    _items.Count - 1
                )
            );
        }

        public void Insert(int index, object item)
        {
            _items.Insert(index, item);
            _collectionChanged?.Invoke(
                this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index)
            );
        }

        public void InsertRange(int index, params object[] items)
        {
            _items.InsertRange(index, items);
            _collectionChanged?.Invoke(
                this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, index)
            );
        }

        public void RemoveAt(int index)
        {
            var item = _items[index];
            _items.RemoveAt(index);
            _collectionChanged?.Invoke(
                this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index)
            );
        }

        public void Move(int oldIndex, int newIndex)
        {
            var item = _items[oldIndex];
            _items.RemoveAt(oldIndex);
            _items.Insert(newIndex, item);
            _collectionChanged?.Invoke(
                this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Move,
                    item,
                    newIndex,
                    oldIndex
                )
            );
        }

        public void ReplaceAt(int index, object newItem)
        {
            var oldItem = _items[index];
            _items[index] = newItem;
            _collectionChanged?.Invoke(
                this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace,
                    newItem,
                    oldItem,
                    index
                )
            );
        }

        public void Reset()
        {
            _items.Clear();
            _collectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public IEnumerator GetEnumerator() => _items.GetEnumerator();
    }
}

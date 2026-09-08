// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class VirtualizingPanelMutationGroupingTests
{
    public static IEnumerable<object[]> MatrixCases()
    {
        foreach (var mode in new[] { VirtualizationMode.Standard, VirtualizationMode.Recycling })
        {
            foreach (var grouped in new[] { false, true })
            {
                foreach (var orientation in new[] { Orientation.Vertical, Orientation.Horizontal })
                {
                    yield return new object[] { mode, grouped, orientation };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(MatrixCases))]
    public void Mutations_PreserveGeneratedContainerIdentityAndDataCorrespondence(
        VirtualizationMode mode,
        bool grouped,
        Orientation orientation)
    {
        WpfTestHost.Invoke(() =>
        {
            var (control, items, outerPanel) = BuildHarness(mode, grouped, orientation, itemCount: 80, groupSize: 10);

            AssertConsistent(control, items, grouped);

            var realizedIds = RealizedIds(control);
            var outsideBefore = items.First(i => !realizedIds.Contains(i.Id));
            var insideBefore = items.First(i => realizedIds.Contains(i.Id));

            // Remove an item outside the realized region.
            items.Remove(outsideBefore);
            Layout(control);
            AssertConsistent(control, items, grouped);
            Assert.DoesNotContain(AllRealizedItemContainers(control), c => ReferenceEquals(c.DataContext, outsideBefore));

            // Replace a realized item with a new identity.
            var replaceIndex = items.IndexOf(insideBefore);
            var replacement = new ProbeItem(1000 + insideBefore.Id, insideBefore.Group);
            items[replaceIndex] = replacement;
            Layout(control);
            AssertConsistent(control, items, grouped);
            Assert.DoesNotContain(AllRealizedItemContainers(control), c => ReferenceEquals(c.DataContext, insideBefore));

            // Move an item within its own group from one end of the group's block to the other.
            var moveGroup = replacement.Group;
            var groupIndices = items
                .Select((item, index) => (item, index))
                .Where(pair => pair.item.Group == moveGroup)
                .Select(pair => pair.index)
                .ToList();
            var moveSource = groupIndices.First();
            var moveTarget = groupIndices.Last();
            var moved = items[moveSource];
            items.Move(moveSource, moveTarget);
            Layout(control);
            AssertConsistent(control, items, grouped);

            // Remove a currently realized (visible) item outright.
            realizedIds = RealizedIds(control);
            var visibleToRemove = items.First(i => realizedIds.Contains(i.Id) && !ReferenceEquals(i, moved));
            items.Remove(visibleToRemove);
            Layout(control);
            AssertConsistent(control, items, grouped);
            Assert.DoesNotContain(AllRealizedItemContainers(control), c => ReferenceEquals(c.DataContext, visibleToRemove));

            // Reset the collection outright and repopulate it with fresh identities.
            items.Clear();
            Layout(control);
            AssertConsistent(control, items, grouped);
            Assert.Empty(AllRealizedItemContainers(control));

            for (var i = 0; i < 40; i++)
            {
                items.Add(new ProbeItem(2000 + i, "G" + (i / 10)));
            }

            Layout(control);
            AssertConsistent(control, items, grouped);
        });
    }

    [Theory]
    [MemberData(nameof(MatrixCases))]
    public void BringIndexIntoView_MovesOffsetAndResizeKeepsOffsetClamped(
        VirtualizationMode mode,
        bool grouped,
        Orientation orientation)
    {
        WpfTestHost.Invoke(() =>
        {
            // Grouped mode needs enough groups for the outer panel's own extent to exceed
            // the viewport before a bring-into-view has any offset to move.
            var itemCount = grouped ? 110 : 80;
            var (control, items, outerPanel) = BuildHarness(mode, grouped, orientation, itemCount, groupSize: 10);

            if (grouped)
            {
                outerPanel.InvokeBringIndexIntoView(10);
            }
            else
            {
                outerPanel.InvokeBringIndexIntoView(70);
            }

            Layout(control);
            AssertConsistent(control, items, grouped);

            var offsetAfterBring = orientation == Orientation.Horizontal
                ? outerPanel.HorizontalOffset
                : outerPanel.VerticalOffset;
            Assert.True(offsetAfterBring > 0, "Bring-into-view did not move the offset forward.");

            if (!grouped)
            {
                Assert.Contains(AllRealizedItemContainers(control), c => ((ProbeItem)c.DataContext!).Id == 70);
            }
            else
            {
                var nestedPanel = FindAllPanels(control).FirstOrDefault(p => !ReferenceEquals(p, outerPanel));
                if (nestedPanel is not null)
                {
                    nestedPanel.InvokeBringIndexIntoView(0);
                    Layout(control);
                    AssertConsistent(control, items, grouped);
                }
            }

            // Shrink the viewport well below the extent and verify the offset stays clamped.
            control.Width = 120;
            control.Height = 120;
            Layout(control);
            AssertConsistent(control, items, grouped);

            var extent = orientation == Orientation.Horizontal ? outerPanel.ExtentWidth : outerPanel.ExtentHeight;
            var viewport = orientation == Orientation.Horizontal ? outerPanel.ViewportWidth : outerPanel.ViewportHeight;
            var offsetAfterResize = orientation == Orientation.Horizontal
                ? outerPanel.HorizontalOffset
                : outerPanel.VerticalOffset;
            var maxOffset = Math.Max(0, extent - viewport);
            Assert.True(offsetAfterResize <= maxOffset + 0.01, "Offset was not clamped after the viewport shrank.");
            Assert.True(offsetAfterResize >= 0, "Offset went negative after the viewport shrank.");
        });
    }

    private static HashSet<int> RealizedIds(ItemsControl control) =>
        AllRealizedItemContainers(control).Select(c => ((ProbeItem)c.DataContext!).Id).ToHashSet();

    private static void AssertConsistent(ItemsControl control, ObservableCollection<ProbeItem> items, bool grouped)
    {
        var containers = AllRealizedItemContainers(control).ToList();
        var seen = new HashSet<ProbeItem>(ReferenceEqualityComparer.Instance);

        foreach (var container in containers)
        {
            var probeItem = Assert.IsType<ProbeItem>(container.DataContext);
            Assert.True(seen.Add(probeItem), $"{probeItem} is bound to more than one realized container.");
            Assert.Contains(items, item => ReferenceEquals(item, probeItem));

            var index = control.ItemContainerGenerator.IndexFromContainer(container);
            Assert.True(index >= 0, $"Container for {probeItem} has no generator index.");

            if (!grouped)
            {
                Assert.Same(items[index], probeItem);
            }
        }

        if (grouped)
        {
            var outerPanel = FindAllPanels(control).First();
            Assert.All(
                outerPanel.Children.OfType<FrameworkElement>(),
                child => Assert.IsAssignableFrom<GroupItem>(child));
        }
    }

    private static IEnumerable<FrameworkElement> AllRealizedItemContainers(DependencyObject root) =>
        FindAllPanels(root)
            .SelectMany(panel => panel.Children.OfType<FrameworkElement>())
            .Where(fe => fe.DataContext is ProbeItem);

    private static List<ProbePanel> FindAllPanels(DependencyObject root)
    {
        var results = new List<ProbePanel>();
        CollectPanels(root, results);
        return results;
    }

    private static void CollectPanels(DependencyObject node, List<ProbePanel> results)
    {
        var count = VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(node, i);
            if (child is ProbePanel panel)
            {
                results.Add(panel);
            }

            CollectPanels(child, results);
        }
    }

    private static (VirtualizingItemsControl Control, ObservableCollection<ProbeItem> Items, ProbePanel OuterPanel) BuildHarness(
        VirtualizationMode mode,
        bool grouped,
        Orientation orientation,
        int itemCount,
        int groupSize)
    {
        var items = new ObservableCollection<ProbeItem>();
        for (var i = 0; i < itemCount; i++)
        {
            items.Add(new ProbeItem(i, "G" + (i / groupSize)));
        }

        var itemSize = orientation == Orientation.Vertical ? new Size(280, 30) : new Size(30, 280);

        var control = new VirtualizingItemsControl
        {
            Template = BuildControlTemplate(),
            ItemsPanel = new ItemsPanelTemplate(BuildPanelFactory(orientation, itemSize)),
            Width = 300,
            Height = 300,
        };

        VirtualizingPanel.SetIsVirtualizing(control, true);
        VirtualizingPanel.SetVirtualizationMode(control, mode);

        if (grouped)
        {
            control.GroupStyle.Add(
                new GroupStyle { Panel = new ItemsPanelTemplate(BuildPanelFactory(orientation, itemSize)) });

            ICollectionView view = CollectionViewSource.GetDefaultView(items);
            view.GroupDescriptions!.Add(new PropertyGroupDescription(nameof(ProbeItem.Group)));
            control.ItemsSource = view;
        }
        else
        {
            control.ItemsSource = items;
        }

        Layout(control);

        var outerPanel = FindAllPanels(control).FirstOrDefault()
            ?? throw new InvalidOperationException("The outer virtualizing panel was not realized.");

        return (control, items, outerPanel);
    }

    private static ControlTemplate BuildControlTemplate()
    {
        const string xaml = """
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                              xmlns:controls="clr-namespace:Wpf.Ui.Controls;assembly=Wpf.Ui"
                              TargetType="{x:Type controls:VirtualizingItemsControl}">
                <ScrollViewer CanContentScroll="True"
                              HorizontalScrollBarVisibility="Auto"
                              VerticalScrollBarVisibility="Auto">
                    <ItemsPresenter />
                </ScrollViewer>
            </ControlTemplate>
            """;

        return (ControlTemplate)XamlReader.Parse(xaml);
    }

    private static FrameworkElementFactory BuildPanelFactory(Orientation orientation, Size? itemSize)
    {
        var factory = new FrameworkElementFactory(typeof(ProbePanel));
        factory.SetValue(VirtualizingWrapPanel.OrientationProperty, orientation);
        if (itemSize is { } size)
        {
            factory.SetValue(VirtualizingWrapPanel.ItemSizeProperty, size);
        }

        factory.SetValue(VirtualizingWrapPanel.SpacingModeProperty, SpacingMode.StartAndEndOnly);
        return factory;
    }

    private static void Layout(FrameworkElement control)
    {
        control.Measure(new Size(control.Width, control.Height));
        control.Arrange(new Rect(0, 0, control.Width, control.Height));
        control.UpdateLayout();
    }

    private sealed class ProbeItem
    {
        public ProbeItem(int id, string group)
        {
            Id = id;
            Group = group;
        }

        public int Id { get; }

        public string Group { get; }

        public override string ToString() => $"Item{Id}";
    }

    private sealed class ProbePanel : VirtualizingWrapPanel
    {
        public new ItemRange ItemRange => base.ItemRange;

        public void InvokeBringIndexIntoView(int index) => BringIndexIntoView(index);
    }
}

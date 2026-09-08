// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Wpf.Ui.Appearance;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Appearance;

[Collection("ThemeState")]
public sealed class ResourceDictionaryCompositionTests : IDisposable
{
    private readonly List<ResourceDictionary> _insertedAtTopLevel = [];

    public void Dispose()
    {
        WpfTestHost.Invoke(() =>
        {
            Collection<ResourceDictionary> top = UiApplication.Current.Resources.MergedDictionaries;

            foreach (ResourceDictionary dictionary in _insertedAtTopLevel)
            {
                top.Remove(dictionary);
            }
        });
    }

    [Fact]
    public void GetDictionary_TopLevelMatch_ReturnsIt()
    {
        WpfTestHost.Invoke(() =>
        {
            ResourceDictionary probe = AddTopLevel(DarkDictionary());
            var manager = new ResourceDictionaryManager(ApplicationThemeManager.LibraryNamespace);

            ResourceDictionary? found = manager.GetDictionary("theme");

            Assert.Same(probe, found);
        });
    }

    [Fact]
    public void GetDictionary_NestedTwoLevelsDeep_ReturnsIt()
    {
        WpfTestHost.Invoke(() =>
        {
            ResourceDictionary match = LightDictionary();
            ResourceDictionary wrapper = Wrap(Wrap(match));
            AddTopLevel(wrapper);

            var manager = new ResourceDictionaryManager(ApplicationThemeManager.LibraryNamespace);

            ResourceDictionary? found = manager.GetDictionary("theme");

            Assert.Same(match, found);
        });
    }

    [Fact]
    public void GetDictionary_NestedThreeLevelsDeep_ReturnsIt()
    {
        WpfTestHost.Invoke(() =>
        {
            ResourceDictionary match = LightDictionary();
            ResourceDictionary wrapper = Wrap(Wrap(Wrap(match)));
            AddTopLevel(wrapper);

            var manager = new ResourceDictionaryManager(ApplicationThemeManager.LibraryNamespace);

            ResourceDictionary? found = manager.GetDictionary("theme");

            Assert.Same(match, found);
        });
    }

    [Fact]
    public void GetDictionary_LaterTopLevelDirectMatch_OutranksEarlierNestedMatch()
    {
        WpfTestHost.Invoke(() =>
        {
            ResourceDictionary nestedMatch = LightDictionary();
            AddTopLevel(Wrap(nestedMatch));
            ResourceDictionary laterDirectMatch = AddTopLevel(DarkDictionary());

            var manager = new ResourceDictionaryManager(ApplicationThemeManager.LibraryNamespace);

            ResourceDictionary? found = manager.GetDictionary("theme");

            Assert.Same(laterDirectMatch, found);
        });
    }

    [Fact]
    public void GetDictionary_LaterTopLevelWrapper_OutranksEarlierDirectMatch()
    {
        WpfTestHost.Invoke(() =>
        {
            AddTopLevel(DarkDictionary());
            ResourceDictionary nestedMatch = LightDictionary();
            AddTopLevel(Wrap(nestedMatch));

            var manager = new ResourceDictionaryManager(ApplicationThemeManager.LibraryNamespace);

            ResourceDictionary? found = manager.GetDictionary("theme");

            Assert.Same(nestedMatch, found);
        });
    }

    [Fact]
    public void GetDictionary_NoMatchInNamespace_ReturnsNull()
    {
        WpfTestHost.Invoke(() =>
        {
            AddTopLevel(DarkDictionary());

            var manager = new ResourceDictionaryManager("no.such.library;");

            ResourceDictionary? found = manager.GetDictionary("theme");

            Assert.Null(found);
        });
    }

    [Fact]
    public void GetDictionary_DeeplyNestedTree_TerminatesWithinBoundedTime()
    {
        WpfTestHost.Invoke(() =>
        {
            ResourceDictionary match = LightDictionary();
            ResourceDictionary wrapper = match;

            for (var depth = 0; depth < 25; depth++)
            {
                wrapper = Wrap(wrapper);
            }

            AddTopLevel(wrapper);

            var manager = new ResourceDictionaryManager(ApplicationThemeManager.LibraryNamespace);

            ResourceDictionary? found = null;
            bool completed = Task.Run(() => found = manager.GetDictionary("theme")).Wait(TimeSpan.FromSeconds(30));

            Assert.True(completed, "GetDictionary did not terminate against a deeply nested tree.");
            Assert.Same(match, found);
        });
    }

    [Fact]
    public void UpdateDictionary_NestedMatch_ReplacesInPlacePreservingPosition()
    {
        WpfTestHost.Invoke(() =>
        {
            ResourceDictionary match = LightDictionary();
            ResourceDictionary wrapper = Wrap(match);
            AddTopLevel(wrapper);

            var manager = new ResourceDictionaryManager(ApplicationThemeManager.LibraryNamespace);
            var newSource = new Uri(
                ApplicationThemeManager.ThemesDictionaryPath + "HCWhite.xaml",
                UriKind.Absolute
            );

            bool updated = manager.UpdateDictionary("theme", newSource);

            Assert.True(updated);
            Assert.Single(wrapper.MergedDictionaries);
            Assert.Equal(newSource, wrapper.MergedDictionaries[0].Source);
        });
    }

    [Fact]
    public void UpdateDictionary_MultipleMatches_ReplacesOnlyTheEffectiveOne()
    {
        WpfTestHost.Invoke(() =>
        {
            ResourceDictionary earlierMatch = AddTopLevel(DarkDictionary());
            ResourceDictionary laterMatch = AddTopLevel(LightDictionary());

            var manager = new ResourceDictionaryManager(ApplicationThemeManager.LibraryNamespace);
            var newSource = new Uri(
                ApplicationThemeManager.ThemesDictionaryPath + "HCWhite.xaml",
                UriKind.Absolute
            );

            Collection<ResourceDictionary> top = UiApplication.Current.Resources.MergedDictionaries;
            int laterIndex = top.IndexOf(laterMatch);

            bool updated = manager.UpdateDictionary("theme", newSource);

            Assert.True(updated);
            Assert.Equal(newSource, top[laterIndex].Source);
            Assert.Same(earlierMatch, top[top.IndexOf(earlierMatch)]);
            Assert.Equal(
                new Uri(ApplicationThemeManager.ThemesDictionaryPath + "Dark.xaml", UriKind.Absolute),
                earlierMatch.Source
            );

            _insertedAtTopLevel.Remove(laterMatch);
            _insertedAtTopLevel.Add(top[laterIndex]);
        });
    }

    [Fact]
    public void UpdateDictionary_NoMatchInNamespace_ReturnsFalseAndLeavesTreeUnchanged()
    {
        WpfTestHost.Invoke(() =>
        {
            ResourceDictionary probe = AddTopLevel(DarkDictionary());
            Collection<ResourceDictionary> top = UiApplication.Current.Resources.MergedDictionaries;
            int countBefore = top.Count;

            var manager = new ResourceDictionaryManager("no.such.library;");

            bool updated = manager.UpdateDictionary(
                "theme",
                new Uri(ApplicationThemeManager.ThemesDictionaryPath + "HCWhite.xaml", UriKind.Absolute)
            );

            Assert.False(updated);
            Assert.Equal(countBefore, top.Count);
            Assert.Same(probe, top[top.IndexOf(probe)]);
        });
    }

    private ResourceDictionary AddTopLevel(ResourceDictionary dictionary)
    {
        UiApplication.Current.Resources.MergedDictionaries.Add(dictionary);
        _insertedAtTopLevel.Add(dictionary);

        return dictionary;
    }

    private static ResourceDictionary Wrap(ResourceDictionary child)
    {
        var wrapper = new ResourceDictionary();
        wrapper.MergedDictionaries.Add(child);

        return wrapper;
    }

    private static ResourceDictionary DarkDictionary() =>
        new() { Source = new Uri(ApplicationThemeManager.ThemesDictionaryPath + "Dark.xaml", UriKind.Absolute) };

    private static ResourceDictionary LightDictionary() =>
        new() { Source = new Uri(ApplicationThemeManager.ThemesDictionaryPath + "Light.xaml", UriKind.Absolute) };
}

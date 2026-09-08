// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using Wpf.Ui.Markup;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

/// <summary>Asserts that controls kept out of the library surface are absent from its types and its merged dictionaries.</summary>
public sealed class UnsupportedControlSurfaceTests
{
    private const string ExcludedControl = "TreeGrid";

    [Fact]
    public void LibraryTypes_ExcludeUnsupportedControls()
    {
        string[] declared = typeof(ControlsDictionary)
            .Assembly.GetTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => name.Contains(ExcludedControl, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal([], declared);
    }

    [Fact]
    public void ControlsDictionary_MergesNoUnsupportedControlResources()
    {
        List<string> sources = WpfTestHost.Invoke(() => CollectSources(new ControlsDictionary()));

        string[] excluded = sources
            .Where(source => source.Contains(ExcludedControl, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal([], excluded);
    }

    private static List<string> CollectSources(ResourceDictionary dictionary)
    {
        List<string> sources = [];
        Collect(dictionary, sources);
        return sources;
    }

    private static void Collect(ResourceDictionary dictionary, List<string> sources)
    {
        if (dictionary.Source is not null)
        {
            sources.Add(dictionary.Source.ToString());
        }

        foreach (ResourceDictionary merged in dictionary.MergedDictionaries)
        {
            Collect(merged, sources);
        }
    }
}

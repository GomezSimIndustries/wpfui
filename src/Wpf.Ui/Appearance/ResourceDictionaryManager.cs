// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Wpf.Ui.Appearance;

/// <summary>
/// Allows managing application dictionaries.
/// </summary>
internal class ResourceDictionaryManager
{
    /// <summary>
    /// Gets the namespace, e.g. the library the resource is being searched for.
    /// </summary>
    public string SearchNamespace { get; }

    public ResourceDictionaryManager(string searchNamespace)
    {
        SearchNamespace = searchNamespace;
    }

    /// <summary>
    /// Shows whether the application contains the <see cref="ResourceDictionary"/>.
    /// </summary>
    /// <param name="resourceLookup">Any part of the resource name.</param>
    /// <returns><see langword="false"/> if it doesn't exist.</returns>
    public bool HasDictionary(string resourceLookup)
    {
        return GetDictionary(resourceLookup) != null;
    }

    /// <summary>
    /// Gets the <see cref="ResourceDictionary"/> that wins WPF's merged-dictionary resource
    /// precedence among every dictionary matching <paramref name="resourceLookup"/>, at any nesting depth.
    /// </summary>
    /// <param name="resourceLookup">Any part of the resource name.</param>
    /// <returns><see cref="ResourceDictionary"/>, <see langword="null"/> if it doesn't exist.</returns>
    public ResourceDictionary? GetDictionary(string resourceLookup)
    {
        DictionarySlot? effective = FindEffectiveSlot(resourceLookup);

        return effective?.Owner[effective.Value.Index];
    }

    /// <summary>
    /// Replaces the <see cref="ResourceDictionary"/> that wins WPF's merged-dictionary resource
    /// precedence in place, preserving its position among its siblings.
    /// </summary>
    /// <param name="resourceLookup">Any part of the resource name.</param>
    /// <param name="newResourceUri">A valid <see cref="Uri"/> for the replaced resource.</param>
    /// <returns><see langword="true"/> if the dictionary <see cref="Uri"/> was updated. <see langword="false"/> otherwise.</returns>
    public bool UpdateDictionary(string resourceLookup, Uri? newResourceUri)
    {
        if (newResourceUri is null)
        {
            return false;
        }

        DictionarySlot? effective = FindEffectiveSlot(resourceLookup);

        if (effective is null)
        {
            return false;
        }

        effective.Value.Owner[effective.Value.Index] = new ResourceDictionary { Source = newResourceUri };

        return true;
    }

    /// <summary>
    /// Finds the slot of the matching dictionary with the highest effective WPF resource precedence.
    /// </summary>
    private DictionarySlot? FindEffectiveSlot(string resourceLookup)
    {
        DictionarySlot? effective = null;

        foreach (DictionarySlot slot in EnumerateSlots(GetApplicationMergedDictionaries(), []))
        {
            if (Matches(slot.Owner[slot.Index], resourceLookup))
            {
                effective = slot;
            }
        }

        return effective;
    }

    /// <summary>
    /// Walks a merged-dictionary tree in WPF's precedence order: a dictionary's own merged
    /// children are yielded before the dictionary itself, and siblings are yielded in list order.
    /// The last slot yielded overall is the one that wins resource resolution. Reference cycles
    /// along the current recursion path are skipped.
    /// </summary>
    private static IEnumerable<DictionarySlot> EnumerateSlots(
        Collection<ResourceDictionary> dictionaries,
        HashSet<ResourceDictionary> ancestors
    )
    {
        for (var i = 0; i < dictionaries.Count; i++)
        {
            ResourceDictionary? dictionary = dictionaries[i];

            if (dictionary is null || !ancestors.Add(dictionary))
            {
                continue;
            }

            foreach (DictionarySlot nested in EnumerateSlots(dictionary.MergedDictionaries, ancestors))
            {
                yield return nested;
            }

            ancestors.Remove(dictionary);

            yield return new DictionarySlot(dictionaries, i);
        }
    }

    private bool Matches(ResourceDictionary dictionary, string resourceLookup)
    {
        if (dictionary?.Source is null)
        {
            return false;
        }

        string resourceDictionaryUri = dictionary.Source.ToString();

        return resourceDictionaryUri.Contains(SearchNamespace, StringComparison.OrdinalIgnoreCase)
            && resourceDictionaryUri.Contains(resourceLookup, StringComparison.OrdinalIgnoreCase);
    }

    private Collection<ResourceDictionary> GetApplicationMergedDictionaries()
    {
        return UiApplication.Current.Resources.MergedDictionaries;
    }

    private readonly record struct DictionarySlot(Collection<ResourceDictionary> Owner, int Index);
}

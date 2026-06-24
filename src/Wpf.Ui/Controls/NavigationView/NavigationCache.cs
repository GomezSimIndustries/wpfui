// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// Based on Windows UI Library
// Copyright(c) Microsoft Corporation.All rights reserved.

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

internal class NavigationCache
{
    private readonly Dictionary<Type, object?> _entries = [];

    /// <summary>
    /// Gets the number of cached entries.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Gets or creates a cached instance for the specified type.
    /// </summary>
    /// <param name="entryType">The type to cache.</param>
    /// <param name="cacheMode">The cache mode determining whether to cache or always create new.</param>
    /// <param name="generate">Factory function to create a new instance if not cached.</param>
    /// <returns>The cached or newly created instance.</returns>
    public object? Remember(Type? entryType, NavigationCacheMode cacheMode, Func<object?> generate)
    {
        if (entryType == null)
        {
            return null;
        }

        if (cacheMode == NavigationCacheMode.Disabled)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Cache for {entryType} is disabled. Generating instance using action..."
            );

            return generate.Invoke();
        }

        if (!_entries.TryGetValue(entryType, out var value))
        {
            System.Diagnostics.Debug.WriteLine(
                $"{entryType} not found in cache, generating instance using action..."
            );

            value = generate.Invoke();

            _entries.Add(entryType, value);
        }

        System.Diagnostics.Debug.WriteLine($"{entryType} found in cache.");

        return value;
    }

    /// <summary>
    /// Removes a specific type from the cache.
    /// </summary>
    /// <param name="entryType">The type to remove.</param>
    /// <returns>True if the entry was removed; false if it wasn't in the cache.</returns>
    public bool Remove(Type? entryType)
    {
        if (entryType == null)
        {
            return false;
        }

        return _entries.Remove(entryType);
    }

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
        System.Diagnostics.Debug.WriteLine("Navigation cache cleared.");
    }

    /// <summary>
    /// Checks if a type is currently cached.
    /// </summary>
    /// <param name="entryType">The type to check.</param>
    /// <returns>True if the type is cached; false otherwise.</returns>
    public bool Contains(Type? entryType)
    {
        return entryType != null && _entries.ContainsKey(entryType);
    }
}

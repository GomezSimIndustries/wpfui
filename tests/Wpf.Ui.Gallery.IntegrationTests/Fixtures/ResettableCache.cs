// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.IntegrationTests.Fixtures;

/// <summary>
/// Caches a lazily computed value until explicitly reset.
/// </summary>
/// <typeparam name="T">The type of value being cached.</typeparam>
internal sealed class ResettableCache<T>
    where T : class
{
    private T? value;

    /// <summary>
    /// Returns the cached value, computing and caching it with <paramref name="factory"/> if none is cached.
    /// </summary>
    /// <param name="factory">The function used to compute the value when none is cached.</param>
    public T? GetOrCompute(Func<T?> factory) => value ??= factory();

    /// <summary>
    /// Clears the cached value.
    /// </summary>
    public void Reset() => value = null;
}

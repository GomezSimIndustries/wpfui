// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Gallery.IntegrationTests;

public sealed class ResettableCacheTests
{
    [Fact]
    public void GetOrCompute_CalledRepeatedly_ReturnsTheSameCachedInstance()
    {
        ResettableCache<object> cache = new();
        int factoryCallCount = 0;

        object? first = cache.GetOrCompute(() =>
        {
            factoryCallCount++;
            return new object();
        });
        object? second = cache.GetOrCompute(() =>
        {
            factoryCallCount++;
            return new object();
        });

        second.Should().BeSameAs(first);
        factoryCallCount.Should().Be(1);
    }

    [Fact]
    public void Reset_ThenGetOrCompute_ComputesAndCachesANewInstance()
    {
        ResettableCache<object> cache = new();
        object first = cache.GetOrCompute(() => new object())!;

        cache.Reset();

        int factoryCallCount = 0;
        object? second = cache.GetOrCompute(() =>
        {
            factoryCallCount++;
            return new object();
        });

        second.Should().NotBeSameAs(first, "because resetting the cache must not leave the previous value reachable");
        factoryCallCount.Should().Be(1);
    }
}

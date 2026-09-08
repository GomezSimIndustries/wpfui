// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Extensions;

namespace Wpf.Ui.UnitTests.Extensions;

public class DateTimeExtensionsTests
{
    private static readonly DateTime UnixEpochUtc = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(2026, 1, 15)]
    [InlineData(2026, 7, 15)]
    public void GetTimestamp_LocalKind_MatchesUtcConversionForSameWallClockInstant(int year, int month, int day)
    {
        var localWallClock = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Local);
        var expectedUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localWallClock, DateTimeKind.Unspecified),
            TimeZoneInfo.Local);
        var expectedTimestamp = (long)expectedUtc.Subtract(UnixEpochUtc).TotalSeconds;

        Assert.Equal(expectedTimestamp, localWallClock.GetTimestamp());
    }

    [Theory]
    [InlineData(2026, 1, 15)]
    [InlineData(2026, 7, 15)]
    public void GetMillisTimestamp_LocalKind_MatchesUtcConversionForSameWallClockInstant(int year, int month, int day)
    {
        var localWallClock = new DateTime(year, month, day, 12, 0, 0, 250, DateTimeKind.Local);
        var expectedUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localWallClock, DateTimeKind.Unspecified),
            TimeZoneInfo.Local);
        var expectedTimestamp = (long)expectedUtc.Subtract(UnixEpochUtc).TotalMilliseconds;

        Assert.Equal(expectedTimestamp, localWallClock.GetMillisTimestamp());
    }

    [Theory]
    [InlineData(2026, 1, 15)]
    [InlineData(2026, 7, 15)]
    public void GetMicroTimestamp_LocalKind_MatchesUtcConversionForSameWallClockInstant(int year, int month, int day)
    {
        var localWallClock = new DateTime(year, month, day, 12, 0, 0, 250, DateTimeKind.Local);
        var expectedUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localWallClock, DateTimeKind.Unspecified),
            TimeZoneInfo.Local);
        var expectedTimestamp = expectedUtc.Subtract(UnixEpochUtc).Ticks / (TimeSpan.TicksPerMillisecond / 1000);

        Assert.Equal(expectedTimestamp, localWallClock.GetMicroTimestamp());
    }

    [Fact]
    public void GetTimestamp_UnspecifiedKind_IsTreatedAsUtc()
    {
        var utcInstant = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var unspecifiedInstant = DateTime.SpecifyKind(utcInstant, DateTimeKind.Unspecified);

        Assert.Equal(utcInstant.GetTimestamp(), unspecifiedInstant.GetTimestamp());
    }

    [Fact]
    public void GetTimestamp_PreEpochInstant_ReturnsNegativeValue()
    {
        var preEpoch = UnixEpochUtc.AddDays(-1);

        Assert.Equal(-86400, preEpoch.GetTimestamp());
    }

    [Fact]
    public void GetMillisTimestamp_SubSecondValue_PreservesMilliseconds()
    {
        var instant = UnixEpochUtc.AddTicks(15_000_030);

        Assert.Equal(1500, instant.GetMillisTimestamp());
    }

    [Fact]
    public void GetMicroTimestamp_SubMillisecondValue_PreservesMicroseconds()
    {
        var instant = UnixEpochUtc.AddTicks(15_000_030);

        Assert.Equal(1_500_003, instant.GetMicroTimestamp());
    }
}

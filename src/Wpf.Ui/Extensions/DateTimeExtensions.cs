// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Extensions;

/// <summary>
/// A collection of several extensions to the <see cref="DateTime"/> class.
/// </summary>
public static class DateTimeExtensions
{
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Gets the number of seconds that have elapsed since the Unix epoch, excluding leap seconds. The Unix epoch is 00:00:00 UTC on 1 January 1970.
    /// </summary>
    /// <remarks>
    /// <see cref="DateTimeKind.Local"/> values are converted to UTC before the epoch is subtracted. <see cref="DateTimeKind.Unspecified"/> values are treated as already representing UTC.
    /// </remarks>
    public static long GetTimestamp(this DateTime dateTime)
    {
        return (long)ToUnixUtc(dateTime).Subtract(UnixEpoch).TotalSeconds;
    }

    /// <summary>
    /// Gets the number of milliseconds that have elapsed since the Unix epoch, excluding leap seconds. The Unix epoch is 00:00:00 UTC on 1 January 1970.
    /// </summary>
    /// <remarks>
    /// <see cref="DateTimeKind.Local"/> values are converted to UTC before the epoch is subtracted. <see cref="DateTimeKind.Unspecified"/> values are treated as already representing UTC.
    /// </remarks>
    public static long GetMillisTimestamp(this DateTime dateTime)
    {
        // Should be 10^-3
        return (long)ToUnixUtc(dateTime).Subtract(UnixEpoch).TotalMilliseconds;
    }

    /// <summary>
    /// Gets the number of microseconds that have elapsed since the Unix epoch, excluding leap seconds. The Unix epoch is 00:00:00 UTC on 1 January 1970.
    /// </summary>
    /// <remarks>
    /// <see cref="DateTimeKind.Local"/> values are converted to UTC before the epoch is subtracted. <see cref="DateTimeKind.Unspecified"/> values are treated as already representing UTC.
    /// </remarks>
    public static long GetMicroTimestamp(this DateTime dateTime)
    {
        // Should be 10^-6
        return ToUnixUtc(dateTime).Subtract(UnixEpoch).Ticks
            / (TimeSpan.TicksPerMillisecond / 1000);
    }

    private static DateTime ToUnixUtc(DateTime dateTime)
    {
        return dateTime.Kind == DateTimeKind.Local
            ? dateTime.ToUniversalTime()
            : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
    }
}

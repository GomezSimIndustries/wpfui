// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Controls;

/// <summary>
/// Represents responsive breakpoint tiers based on container width percentages.
/// </summary>
public enum Breakpoint
{
    /// <summary>
    /// Extra small - default/fallback when container width is below SmThreshold.
    /// </summary>
    Xs,

    /// <summary>
    /// Small - when container width is at or above SmThreshold (default 25%).
    /// </summary>
    Sm,

    /// <summary>
    /// Medium - when container width is at or above MdThreshold (default 50%).
    /// </summary>
    Md,

    /// <summary>
    /// Large - when container width is at or above LgThreshold (default 75%).
    /// </summary>
    Lg,
}

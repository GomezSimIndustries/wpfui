// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Wpf.Ui.UnitTests.TestHelpers;

/// <summary>Records the window instance targeted by each native call <see cref="WindowBackgroundManager"/> issues.</summary>
internal sealed class RecordingWindowBackgroundNativeCalls : IWindowBackgroundNativeCalls
{
    public List<Window?> ApplyWindowDarkModeCalls { get; } = [];

    public List<Window?> RemoveWindowDarkModeCalls { get; } = [];

    public List<(Window? Window, WindowBackdropType Backdrop)> ApplyBackdropCalls { get; } = [];

    public List<Window?> RemoveBackdropCalls { get; } = [];

    public List<Window?> RemoveTitlebarBackgroundCalls { get; } = [];

    public bool ApplyWindowDarkMode(Window? window)
    {
        ApplyWindowDarkModeCalls.Add(window);

        return true;
    }

    public bool RemoveWindowDarkMode(Window? window)
    {
        RemoveWindowDarkModeCalls.Add(window);

        return true;
    }

    public bool ApplyBackdrop(Window? window, WindowBackdropType backdropType)
    {
        ApplyBackdropCalls.Add((window, backdropType));

        return true;
    }

    public bool RemoveBackdrop(Window? window)
    {
        RemoveBackdropCalls.Add(window);

        return true;
    }

    public bool RemoveTitlebarBackground(Window? window)
    {
        RemoveTitlebarBackgroundCalls.Add(window);

        return true;
    }
}

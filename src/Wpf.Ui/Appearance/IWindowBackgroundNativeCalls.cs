// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Controls;
using Wpf.Ui.Interop;

namespace Wpf.Ui.Appearance;

/// <summary>
/// Issues the native window effects that <see cref="WindowBackgroundManager"/> applies per window.
/// </summary>
internal interface IWindowBackgroundNativeCalls
{
    /// <summary>
    /// Applies the ImmersiveDarkMode effect to the given window.
    /// </summary>
    bool ApplyWindowDarkMode(Window? window);

    /// <summary>
    /// Removes the ImmersiveDarkMode effect from the given window.
    /// </summary>
    bool RemoveWindowDarkMode(Window? window);

    /// <summary>
    /// Applies the given backdrop type to the given window.
    /// </summary>
    bool ApplyBackdrop(Window? window, WindowBackdropType backdropType);

    /// <summary>
    /// Removes any applied backdrop effect from the given window.
    /// </summary>
    bool RemoveBackdrop(Window? window);

    /// <summary>
    /// Removes the titlebar background color override from the given window.
    /// </summary>
    bool RemoveTitlebarBackground(Window? window);
}

/// <summary>
/// Issues the native window effects through <see cref="UnsafeNativeMethods"/> and <see cref="WindowBackdrop"/>.
/// </summary>
internal sealed class Win32WindowBackgroundNativeCalls : IWindowBackgroundNativeCalls
{
    /// <inheritdoc />
    public bool ApplyWindowDarkMode(Window? window) => UnsafeNativeMethods.ApplyWindowDarkMode(window);

    /// <inheritdoc />
    public bool RemoveWindowDarkMode(Window? window) => UnsafeNativeMethods.RemoveWindowDarkMode(window);

    /// <inheritdoc />
    public bool ApplyBackdrop(Window? window, WindowBackdropType backdropType) =>
        WindowBackdrop.ApplyBackdrop(window, backdropType);

    /// <inheritdoc />
    public bool RemoveBackdrop(Window? window) => WindowBackdrop.RemoveBackdrop(window);

    /// <inheritdoc />
    public bool RemoveTitlebarBackground(Window? window) => WindowBackdrop.RemoveTitlebarBackground(window);
}

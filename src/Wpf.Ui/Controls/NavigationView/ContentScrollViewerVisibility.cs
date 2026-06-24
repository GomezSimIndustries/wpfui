// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Controls;

/// <summary>
/// Specifies the visibility behavior of the internal ScrollViewer in NavigationViewContentPresenter.
/// </summary>
public enum ContentScrollViewerVisibility
{
    /// <summary>
    /// Automatically determine based on the page's ScrollViewer.CanContentScroll property.
    /// This is the default behavior.
    /// </summary>
    Auto,

    /// <summary>
    /// Always show the internal ScrollViewer regardless of page settings.
    /// </summary>
    Visible,

    /// <summary>
    /// Never show the internal ScrollViewer, allowing pages to provide their own scrolling.
    /// </summary>
    Hidden,
}

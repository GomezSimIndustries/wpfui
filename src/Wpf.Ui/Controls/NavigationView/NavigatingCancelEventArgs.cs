// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// Based on Windows UI Library
// Copyright(c) Microsoft Corporation.All rights reserved.

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

public class NavigatingCancelEventArgs : RoutedEventArgs
{
    public NavigatingCancelEventArgs(RoutedEvent routedEvent, object source)
        : base(routedEvent, source) { }

    public required object Page { get; init; }

    public bool Cancel { get; set; }

    /// <summary>Gets a one-use continuation for this request after it has been canceled.</summary>
    public Func<bool>? ResumeNavigation { get; internal init; }

    /// <summary>Gets the data context supplied with the navigation request.</summary>
    public object? TargetDataContext { get; internal init; }
}

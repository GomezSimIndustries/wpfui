// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Carries the exception thrown by an <see cref="Wpf.Ui.Abstractions.Controls.INavigationAware"/> callback.
/// </summary>
public sealed class NavigationLifecycleFailedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationLifecycleFailedEventArgs"/> class.
    /// </summary>
    /// <param name="target">The object whose callback threw.</param>
    /// <param name="isNavigatingTo">Whether the callback was the entry callback.</param>
    /// <param name="exception">The exception the callback threw.</param>
    public NavigationLifecycleFailedEventArgs(object target, bool isNavigatingTo, Exception exception)
    {
        Target = target;
        IsNavigatingTo = isNavigatingTo;
        Exception = exception;
    }

    /// <summary>Gets the object whose callback threw.</summary>
    public object Target { get; }

    /// <summary>Gets a value indicating whether the callback that threw was the entry callback.</summary>
    public bool IsNavigatingTo { get; }

    /// <summary>Gets the exception the callback threw.</summary>
    public Exception Exception { get; }
}

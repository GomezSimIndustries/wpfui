// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Wpf.Ui.UnitTests.TestHelpers;

/// <summary>Records every popup a test opens and checks it, together with every open window, for screen presence and activation.</summary>
public static class WindowActivationGuard
{
    private const double OffScreenThreshold = -1000d;

    private static readonly ConcurrentBag<Popup> OpenedPopups = new();

    /// <summary>Records a popup a test opened, for the compliance walk in <see cref="FindViolations"/>. Safe to call from a framework callback, since it never throws.</summary>
    public static void TrackPopupOpened(Popup popup) => OpenedPopups.Add(popup);

    /// <summary>Records a popup a test opened and fails the calling test immediately if it is on-screen or takes activation. Call only from plain test code, never from a framework callback.</summary>
    public static void AssertPopupCompliant(Popup popup)
    {
        TrackPopupOpened(popup);

        Window? owner = FindOwningWindow(popup);
        Assert.True(owner is not null && IsWindowCompliant(owner), DescribePopup(popup, owner));
    }

    /// <summary>Describes every currently open, fully loaded window and every tracked popup that is on-screen or takes foreground activation.</summary>
    public static IReadOnlyList<string> FindViolations()
    {
        var violations = new List<string>();

        foreach (Window window in Application.Current!.Windows)
        {
            if (window.IsLoaded && !IsWindowCompliant(window))
            {
                violations.Add(DescribeWindow(window));
            }
        }

        foreach (Popup popup in OpenedPopups)
        {
            Window? owner = FindOwningWindow(popup);
            if (owner is null || (owner.IsLoaded && !IsWindowCompliant(owner)))
            {
                violations.Add(DescribePopup(popup, owner));
            }
        }

        return violations;
    }

    private static bool IsWindowCompliant(Window window) =>
        !window.ShowActivated
        && !window.ShowInTaskbar
        && window.Left <= OffScreenThreshold
        && window.Top <= OffScreenThreshold;

    private static Window? FindOwningWindow(Popup popup)
    {
        DependencyObject? node = popup.PlacementTarget ?? LogicalTreeHelper.GetParent(popup);

        while (node is not null)
        {
            if (node is Window window)
            {
                return window;
            }

            node = LogicalTreeHelper.GetParent(node)
                ?? (node is Visual visual ? VisualTreeHelper.GetParent(visual) : null);
        }

        return null;
    }

    private static string DescribeWindow(Window window) =>
        $"Window '{window.GetType().Name}' ShowActivated={window.ShowActivated} ShowInTaskbar={window.ShowInTaskbar} Left={window.Left} Top={window.Top}";

    private static string DescribePopup(Popup popup, Window? owner) =>
        $"Popup under {(owner is null ? "no window" : owner.GetType().Name)} PlacementTarget={popup.PlacementTarget?.GetType().Name ?? "null"}";
}

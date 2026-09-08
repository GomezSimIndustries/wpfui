// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Markup;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// Represents an icon that uses an IconSource as its content.
/// </summary>
[ContentProperty(nameof(IconSource))]
public class IconSourceElement : IconElement
{
    /// <summary>Identifies the <see cref="IconSource"/> dependency property.</summary>
    public static readonly DependencyProperty IconSourceProperty = DependencyProperty.Register(
        nameof(IconSource),
        typeof(IconSource),
        typeof(IconSourceElement),
        new FrameworkPropertyMetadata(null, OnIconSourcePropertyChanged)
    );

    /// <summary>
    /// Gets or sets <see cref="IconSource"/>
    /// </summary>
    public IconSource? IconSource
    {
        get => (IconSource?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    protected override UIElement InitializeChildren()
    {
        return CreatePresentedChild();
    }

    public IconElement? CreateIconElement()
    {
        return IconSource?.CreateIconElement();
    }

    private static void OnIconSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((IconSourceElement)d).OnIconSourceChanged((IconSource?)e.OldValue, (IconSource?)e.NewValue);
    }

    private void OnIconSourceChanged(IconSource? oldSource, IconSource? newSource)
    {
        if (oldSource is not null)
        {
            oldSource.Changed -= OnIconSourceContentChanged;
        }

        if (newSource is not null)
        {
            newSource.Changed += OnIconSourceContentChanged;
        }

        RefreshPresentedChild();
    }

    private void OnIconSourceContentChanged(object? sender, EventArgs e)
    {
        RefreshPresentedChild();
    }

    private void RefreshPresentedChild()
    {
        if (!HasPresentedChild)
        {
            return;
        }

        ReplacePresentedChild(CreatePresentedChild());
    }

    private UIElement CreatePresentedChild()
    {
        return (UIElement?)CreateIconElement() ?? new Grid();
    }
}

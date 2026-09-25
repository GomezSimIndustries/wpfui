// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

[Collection("ThemeState")]
public sealed class TitleBarButtonMouseOverForegroundBindingTests : IDisposable
{
    public void Dispose()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
        });
    }

    [Fact]
    public void DefaultStyle_DoesNotBindMouseOverButtonsForegroundToItself()
    {
        WpfTestHost.Invoke(() =>
        {
            var style = (Style)Application.Current.FindResource(typeof(TitleBarButton));

            Setter? mouseOverForegroundSetter = style
                .Setters.OfType<Setter>()
                .FirstOrDefault(setter => setter.Property == TitleBarButton.MouseOverButtonsForegroundProperty);

            if (mouseOverForegroundSetter?.Value is BindingBase binding)
            {
                var expression = Assert.IsType<Binding>(binding);
                bool bindsToSelf =
                    expression.RelativeSource?.Mode == RelativeSourceMode.Self
                    && expression.Path.Path == nameof(TitleBarButton.MouseOverButtonsForeground);

                Assert.False(bindsToSelf, "MouseOverButtonsForeground must not bind to itself.");
            }
        });
    }

    [Fact]
    public void ThemeApply_WithADynamicResourceMouseOverForegroundWhileHovered_DoesNotRecurse()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);

            Window window = WpfTestHost.CreateOffscreenOwnerWindow();
            var button = new TitleBarButton { ButtonType = TitleBarButtonType.Help };
            button.SetResourceReference(
                TitleBarButton.MouseOverButtonsForegroundProperty,
                "TextFillColorPrimaryBrush");
            window.Content = button;
            window.Show();
            button.ApplyTemplate();
            button.UpdateLayout();

            try
            {
                button.Hover();

                ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);

                Assert.True(button.IsHovered);
            }
            finally
            {
                window.Close();
            }
        });
    }
}

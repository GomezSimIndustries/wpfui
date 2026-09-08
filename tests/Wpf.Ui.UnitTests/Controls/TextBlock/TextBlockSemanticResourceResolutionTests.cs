// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class TextBlockSemanticResourceResolutionTests
{
    [Fact]
    public void FontTypography_StyleBasedOnAnotherStyle_ResolvesInheritedFontSizeAndWeight()
    {
        WpfTestHost.Invoke(() =>
        {
            var baseStyle = new Style(typeof(System.Windows.Controls.TextBlock));
            baseStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontSizeProperty, 22d));
            baseStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontWeightProperty, FontWeights.Bold));
            var derivedStyle = new Style(typeof(System.Windows.Controls.TextBlock), baseStyle);

            WithReplacedResource(
                "TitleTextBlockStyle",
                derivedStyle,
                () =>
                {
                    var textBlock = new Wpf.Ui.Controls.TextBlock { FontTypography = FontTypography.Title };

                    Assert.Equal(22d, textBlock.FontSize);
                    Assert.Equal(FontWeights.Bold, textBlock.FontWeight);
                }
            );
        });
    }

    [Fact]
    public void Appearance_SemanticResourceReplacedAfterAssignment_ForegroundTracksReplacement()
    {
        WpfTestHost.Invoke(() =>
        {
            WithReplacedResource(
                "TextFillColorSecondaryBrush",
                Brushes.Red,
                () =>
                {
                    Window window = WpfTestHost.CreateOffscreenOwnerWindow();
                    var textBlock = new Wpf.Ui.Controls.TextBlock { Appearance = TextColor.Secondary, Text = "Sample" };
                    ((System.Windows.Controls.Grid)window.Content).Children.Add(textBlock);

                    try
                    {
                        window.Show();
                        window.UpdateLayout();

                        Assert.Equal(Colors.Red, ((SolidColorBrush)textBlock.Foreground).Color);

                        Application.Current!.Resources["TextFillColorSecondaryBrush"] = Brushes.Blue;
                        window.UpdateLayout();

                        Assert.Equal(Colors.Blue, ((SolidColorBrush)textBlock.Foreground).Color);
                        Assert.Equal(Colors.Blue, RenderedForegroundColor(textBlock));
                    }
                    finally
                    {
                        window.Close();
                    }
                }
            );
        });
    }

    [Fact]
    public void FontTypography_SetBeforeAttachingToResourceScope_ResolvesFromScopeOnceAttached()
    {
        WpfTestHost.Invoke(() =>
        {
            var globalMarkerStyle = new Style(typeof(System.Windows.Controls.TextBlock));
            globalMarkerStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontSizeProperty, 999d));

            WithReplacedResource(
                "TitleTextBlockStyle",
                globalMarkerStyle,
                () =>
                {
                    var textBlock = new Wpf.Ui.Controls.TextBlock { FontTypography = FontTypography.Title };

                    var scopedStyle = new Style(typeof(System.Windows.Controls.TextBlock));
                    scopedStyle.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontSizeProperty, 28d));
                    var host = new System.Windows.Controls.StackPanel();
                    host.Resources["TitleTextBlockStyle"] = scopedStyle;
                    host.Children.Add(textBlock);
                    host.Measure(new Size(400, 200));
                    host.Arrange(new Rect(0, 0, 400, 200));

                    Assert.Equal(28d, textBlock.FontSize);
                }
            );
        });
    }

    [Fact]
    public void FontTypography_Cleared_RestoresUnstyledFontMetrics()
    {
        WpfTestHost.Invoke(() =>
        {
            var textBlock = new Wpf.Ui.Controls.TextBlock { FontTypography = FontTypography.Title };
            Assert.NotEqual(14d, textBlock.FontSize);

            textBlock.FontTypography = null;

            Assert.Equal(14d, textBlock.FontSize);
            Assert.Equal(FontWeights.Normal, textBlock.FontWeight);
        });
    }

    [Fact]
    public void FontSize_ExplicitLocalValue_SurvivesAfterTypographyCleared()
    {
        WpfTestHost.Invoke(() =>
        {
            var textBlock = new Wpf.Ui.Controls.TextBlock { FontTypography = FontTypography.Title };

            textBlock.FontTypography = null;
            textBlock.FontSize = 42d;

            Assert.Equal(42d, textBlock.FontSize);
        });
    }

    [Fact]
    public void Foreground_ExplicitBinding_IsPreservedWhenAppearanceNotSet()
    {
        WpfTestHost.Invoke(() =>
        {
            var textBlock = new Wpf.Ui.Controls.TextBlock { DataContext = new BrushHolder(Brushes.Green) };
            _ = textBlock.SetBinding(
                System.Windows.Controls.TextBlock.ForegroundProperty,
                new Binding(nameof(BrushHolder.Brush))
            );

            Assert.Equal(Brushes.Green, textBlock.Foreground);
        });
    }

    private static void WithReplacedResource(string key, object replacement, Action test)
    {
        ResourceDictionary resources = Application.Current!.Resources;
        bool hadOriginal = resources.Contains(key);
        object? original = hadOriginal ? resources[key] : null;

        resources[key] = replacement;
        try
        {
            test();
        }
        finally
        {
            if (hadOriginal)
            {
                resources[key] = original!;
            }
            else
            {
                resources.Remove(key);
            }
        }
    }

    private static Color RenderedForegroundColor(System.Windows.Controls.TextBlock textBlock)
    {
        int width = Math.Max(1, (int)Math.Ceiling(textBlock.ActualWidth));
        int height = Math.Max(1, (int)Math.Ceiling(textBlock.ActualHeight));

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(textBlock);

        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);

        for (int i = 0; i < pixels.Length; i += 4)
        {
            byte blue = pixels[i];
            byte green = pixels[i + 1];
            byte red = pixels[i + 2];
            byte alpha = pixels[i + 3];

            if (alpha > 0 && blue > red && blue > green)
            {
                return Color.FromArgb(alpha, red, green, blue);
            }
        }

        return Colors.Transparent;
    }

    private sealed class BrushHolder(Brush brush)
    {
        public Brush Brush { get; } = brush;
    }
}

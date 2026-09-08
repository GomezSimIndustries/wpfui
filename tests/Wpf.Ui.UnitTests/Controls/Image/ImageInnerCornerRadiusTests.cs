// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Media;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class ImageInnerCornerRadiusTests
{
    [Fact]
    public void InnerCornerRadius_BorderThicknessSetAfterCornerRadius_Recalculates()
    {
        WpfTestHost.Invoke(() =>
        {
            var image = new Wpf.Ui.Controls.Image { CornerRadius = new CornerRadius(10) };
            image.BorderThickness = new Thickness(4);

            Assert.Equal(new CornerRadius(8), image.InnerCornerRadius);
        });
    }

    [Fact]
    public void InnerCornerRadius_BorderThicknessSetBeforeCornerRadius_MatchesOppositeOrder()
    {
        WpfTestHost.Invoke(() =>
        {
            var image = new Wpf.Ui.Controls.Image { BorderThickness = new Thickness(4) };
            image.CornerRadius = new CornerRadius(10);

            Assert.Equal(new CornerRadius(8), image.InnerCornerRadius);
        });
    }

    [Fact]
    public void InnerCornerRadius_BorderThicknessResetToZero_MatchesCornerRadius()
    {
        WpfTestHost.Invoke(() =>
        {
            var image = new Wpf.Ui.Controls.Image
            {
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(4),
            };
            Assert.Equal(new CornerRadius(8), image.InnerCornerRadius);

            image.BorderThickness = new Thickness(0);

            Assert.Equal(new CornerRadius(10), image.InnerCornerRadius);
        });
    }

    [Fact]
    public void InnerCornerRadius_PerCornerRadiiWithSymmetricBorder_SubtractsHalfThicknessFromEachCorner()
    {
        WpfTestHost.Invoke(() =>
        {
            var image = new Wpf.Ui.Controls.Image
            {
                CornerRadius = new CornerRadius(topLeft: 10, topRight: 8, bottomRight: 6, bottomLeft: 4),
                BorderThickness = new Thickness(4),
            };

            Assert.Equal(
                new CornerRadius(topLeft: 8, topRight: 6, bottomRight: 4, bottomLeft: 2),
                image.InnerCornerRadius
            );
        });
    }

    [Fact]
    public void InnerCornerRadius_AsymmetricBorder_SubtractsHalfOfEachCornersOwnSide()
    {
        WpfTestHost.Invoke(() =>
        {
            var thickness = new Thickness(left: 2, top: 4, right: 6, bottom: 8);
            var expected = new CornerRadius(topLeft: 9, topRight: 8, bottomRight: 7, bottomLeft: 6);

            var cornerFirst = new Wpf.Ui.Controls.Image { CornerRadius = new CornerRadius(10) };
            cornerFirst.BorderThickness = thickness;

            var thicknessFirst = new Wpf.Ui.Controls.Image { BorderThickness = thickness };
            thicknessFirst.CornerRadius = new CornerRadius(10);

            Assert.Equal(expected, cornerFirst.InnerCornerRadius);
            Assert.Equal(expected, thicknessFirst.InnerCornerRadius);
        });
    }

    [Fact]
    public void InnerCornerRadius_ClampsToZeroWhenThicknessExceedsRadius()
    {
        WpfTestHost.Invoke(() =>
        {
            var image = new Wpf.Ui.Controls.Image
            {
                CornerRadius = new CornerRadius(2),
                BorderThickness = new Thickness(20),
            };

            Assert.Equal(new CornerRadius(0), image.InnerCornerRadius);
        });
    }

    [Fact]
    public void MaskGeometry_AfterTemplateApplied_ReflectsBorderThicknessChange()
    {
        WpfTestHost.Invoke(() =>
        {
            var image = new Wpf.Ui.Controls.Image
            {
                Width = 100,
                Height = 100,
                CornerRadius = new CornerRadius(10),
                BorderThickness = new Thickness(4),
            };

            Window window = WpfTestHost.CreateOffscreenOwnerWindow();
            ((System.Windows.Controls.Grid)window.Content).Children.Add(image);

            try
            {
                window.Show();
                window.UpdateLayout();

                Assert.Equal(new CornerRadius(8), image.InnerCornerRadius);
                Assert.Equal(new CornerRadius(8), MaskCornerRadius(image));

                image.BorderThickness = new Thickness(0);
                window.UpdateLayout();

                Assert.Equal(new CornerRadius(10), image.InnerCornerRadius);
                Assert.Equal(new CornerRadius(10), MaskCornerRadius(image));
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static CornerRadius MaskCornerRadius(Wpf.Ui.Controls.Image image)
    {
        var outerBorder = (System.Windows.Controls.Border)VisualTreeHelper.GetChild(image, 0);
        var innerImage = (System.Windows.Controls.Image)VisualTreeHelper.GetChild(outerBorder, 0);
        var mask = (VisualBrush)innerImage.OpacityMask;
        var maskBorder = (System.Windows.Controls.Border)mask.Visual;

        return maskBorder.CornerRadius;
    }
}

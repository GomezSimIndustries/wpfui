// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Controls;

namespace Wpf.Ui.UnitTests.Controls;

public class TitleBarTests
{
    [Theory]
    [InlineData(1.0, 1.0, 1000, 800, 1000, 800)]
    [InlineData(1.25, 1.25, 1000, 800, 800, 640)]
    [InlineData(1.5, 1.5, 1500, 900, 1000, 600)]
    [InlineData(2.0, 2.0, 2000, 1000, 1000, 500)]
    public void ToDeviceIndependentPoint_DividesByTheGivenDpiScale(
        double dpiScaleX,
        double dpiScaleY,
        double deviceX,
        double deviceY,
        double expectedX,
        double expectedY
    )
    {
        var dpi = new DpiScale(dpiScaleX, dpiScaleY);

        Point result = TitleBar.ToDeviceIndependentPoint(new Point(deviceX, deviceY), dpi);

        Assert.Equal(expectedX, result.X, 3);
        Assert.Equal(expectedY, result.Y, 3);
    }

    [Fact]
    public void ToDeviceIndependentPoint_PreservesSignForNegativeMonitorCoordinates()
    {
        var dpi = new DpiScale(1.5, 1.5);

        Point result = TitleBar.ToDeviceIndependentPoint(new Point(-1920, -300), dpi);

        Assert.Equal(-1280, result.X, 3);
        Assert.Equal(-200, result.Y, 3);
    }

    [Fact]
    public void ToDeviceIndependentPoint_ConvertsEachCallAgainstTheDpiItWasGiven()
    {
        var point = new Point(1000, 1000);

        Point atStandardScale = TitleBar.ToDeviceIndependentPoint(point, new DpiScale(1.0, 1.0));
        Point atDoubleScale = TitleBar.ToDeviceIndependentPoint(point, new DpiScale(2.0, 2.0));

        Assert.Equal(1000, atStandardScale.X, 3);
        Assert.Equal(500, atDoubleScale.X, 3);
    }
}

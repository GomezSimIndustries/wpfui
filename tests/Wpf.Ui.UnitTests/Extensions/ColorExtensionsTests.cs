// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Media;
using Wpf.Ui.Extensions;

namespace Wpf.Ui.UnitTests.Extensions;

public class ColorExtensionsTests
{
    [Theory]
    [InlineData(0f, 0f, 0f)]
    [InlineData(0f, 0f, 50f)]
    [InlineData(0f, 0f, 100f)]
    [InlineData(0f, 0.005f, 50f)]
    [InlineData(180f, 0f, 25f)]
    public void FromHslToRgb_AchromaticLightness_ChannelsStayInByteRange(
        float hue,
        float saturation,
        float lightness
    )
    {
        (int red, int green, int blue) = ColorExtensions.FromHslToRgb(hue, saturation, lightness);

        Assert.InRange(red, byte.MinValue, byte.MaxValue);
        Assert.InRange(green, byte.MinValue, byte.MaxValue);
        Assert.InRange(blue, byte.MinValue, byte.MaxValue);
    }

    [Fact]
    public void FromHslToRgb_ZeroLightness_ReturnsBlack()
    {
        (int red, int green, int blue) = ColorExtensions.FromHslToRgb(0f, 0f, 0f);

        Assert.Equal((0, 0, 0), (red, green, blue));
    }

    [Fact]
    public void FromHslToRgb_FullLightness_ReturnsWhite()
    {
        (int red, int green, int blue) = ColorExtensions.FromHslToRgb(0f, 0f, 100f);

        Assert.Equal((255, 255, 255), (red, green, blue));
    }

    [Fact]
    public void FromHslToRgb_MiddleLightness_MatchesHsvAchromaticRounding()
    {
        (int hslRed, int hslGreen, int hslBlue) = ColorExtensions.FromHslToRgb(0f, 0f, 50f);
        (int hsvRed, int hsvGreen, int hsvBlue) = ColorExtensions.FromHsvToRgb(0f, 0f, 50f);

        Assert.Equal((hsvRed, hsvGreen, hsvBlue), (hslRed, hslGreen, hslBlue));
    }

    [Theory]
    [InlineData(0f, 100f, 50f)]
    [InlineData(120f, 100f, 0f)]
    [InlineData(240f, 100f, 100f)]
    [InlineData(60f, 50f, 25f)]
    public void FromHslToRgb_ChromaticBoundaries_ChannelsStayInByteRange(
        float hue,
        float saturation,
        float lightness
    )
    {
        (int red, int green, int blue) = ColorExtensions.FromHslToRgb(hue, saturation, lightness);

        Assert.InRange(red, byte.MinValue, byte.MaxValue);
        Assert.InRange(green, byte.MinValue, byte.MaxValue);
        Assert.InRange(blue, byte.MinValue, byte.MaxValue);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(255, 255, 255)]
    [InlineData(128, 128, 128)]
    [InlineData(255, 0, 0)]
    [InlineData(12, 200, 97)]
    public void ToHsl_ThenFromHslToRgb_RoundTripsWithinByteRange(byte red, byte green, byte blue)
    {
        var color = Color.FromRgb(red, green, blue);

        (float hue, float saturation, float lightness) = color.ToHsl();
        (int roundTrippedRed, int roundTrippedGreen, int roundTrippedBlue) = ColorExtensions.FromHslToRgb(
            hue,
            saturation,
            lightness
        );

        Assert.InRange(roundTrippedRed, byte.MinValue, byte.MaxValue);
        Assert.InRange(roundTrippedGreen, byte.MinValue, byte.MaxValue);
        Assert.InRange(roundTrippedBlue, byte.MinValue, byte.MaxValue);
    }
}

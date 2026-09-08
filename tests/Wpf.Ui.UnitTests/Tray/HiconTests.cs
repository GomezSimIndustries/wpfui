// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Tray;
using Wpf.Ui.Tray.Interop;

namespace Wpf.Ui.UnitTests.Tray;

public class HiconTests
{
    public static TheoryData<PixelFormat> SupportedPixelFormats =>
        new()
        {
            PixelFormats.Pbgra32,
            PixelFormats.Bgra32,
            PixelFormats.Bgr32,
            PixelFormats.Bgr24,
            PixelFormats.Gray8,
        };

    [Theory]
    [MemberData(nameof(SupportedPixelFormats))]
    public void FromSource_ConvertsSupportedPixelFormats_ToAnOwnedHicon(PixelFormat format)
    {
        BitmapSource source = CreateBitmap(4, 4, format);

        IntPtr hIcon = Hicon.FromSource(source);

        Assert.NotEqual(IntPtr.Zero, hIcon);
        Assert.True(User32.DestroyIcon(hIcon));
    }

    [Fact]
    public void FromSource_RepeatedCalls_EachProduceAnIndependentHicon()
    {
        BitmapSource source = CreateBitmap(4, 4, PixelFormats.Bgra32);

        IntPtr first = Hicon.FromSource(source);
        IntPtr second = Hicon.FromSource(source);

        Assert.NotEqual(IntPtr.Zero, first);
        Assert.NotEqual(IntPtr.Zero, second);
        Assert.NotEqual(first, second);
        Assert.True(User32.DestroyIcon(first));
        Assert.True(User32.DestroyIcon(second));
    }

    private static BitmapSource CreateBitmap(int width, int height, PixelFormat format)
    {
        int stride = width * ((format.BitsPerPixel + 7) / 8);
        var pixels = new byte[height * stride];

        return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
    }
}

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

// TODO: This class is the only reason for using System.Drawing.Common.
// It is worth looking for a way to get hIcon without using it.
//
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Wpf.Ui.Tray;

/// <summary>
/// Facilitates the creation of a hIcon.
/// </summary>
internal static class Hicon
{
    /// <summary>
    /// Tries to take the icon pointer assigned to the application.
    /// </summary>
    /// <returns>A handle the caller owns and must destroy with <see cref="Interop.User32.DestroyIcon"/>, or <see cref="IntPtr.Zero"/>.</returns>
    public static IntPtr FromApp()
    {
        try
        {
            var processName = Process.GetCurrentProcess().MainModule?.FileName;

            if (string.IsNullOrEmpty(processName))
            {
                return IntPtr.Zero;
            }

            using var appIcon = Icon.ExtractAssociatedIcon(processName);

            if (appIcon == null)
            {
                return IntPtr.Zero;
            }

            return Interop.User32.CopyIcon(appIcon.Handle);
        }
        catch (Exception e)
        {
            Debug.WriteLine(
                $"ERROR | Unable to get application hIcon - {e}",
                "Wpf.Ui.Hicon"
            );
#if DEBUG
            throw;
#else
            return IntPtr.Zero;
#endif
        }
    }

    /// <summary>
    /// Tries to allocate an icon to memory and fetch a pointer to it.
    /// </summary>
    /// <param name="source">Image source.</param>
    /// <returns>A handle the caller owns and must destroy with <see cref="Interop.User32.DestroyIcon"/>, or <see cref="IntPtr.Zero"/>.</returns>
    public static IntPtr FromSource(ImageSource source)
    {
        IntPtr hIcon = IntPtr.Zero;
        var bitmapFrame = source as BitmapFrame;

        if (source is not BitmapSource bitmapSource)
        {
            Debug.WriteLine(
                $"ERROR | Unable to allocate hIcon, ImageSource is not a BitmapSource",
                "Wpf.Ui.Hicon"
            );
            return hIcon;
        }

        if ((bitmapFrame?.Decoder?.Frames?.Count ?? 0) > 1)
        {
            // Gets first bitmap frame.
            bitmapSource = bitmapFrame!.Decoder!.Frames![0];
        }

        // Converts to 32 bits per pixel with the red, green, and blue components premultiplied by alpha.
        if (bitmapSource!.Format != PixelFormats.Pbgra32)
        {
            bitmapSource = new FormatConvertedBitmap(bitmapSource, PixelFormats.Pbgra32, null, 0);
        }

        var stride = bitmapSource.PixelWidth * ((bitmapSource.Format.BitsPerPixel + 7) / 8);
        var pixels = new byte[bitmapSource.PixelHeight * stride];

        bitmapSource.CopyPixels(pixels, stride, 0);

        var gcHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        Bitmap? bitmap = null;

        try
        {
            // Format32bppPArgb matches the Pbgra32 conversion above.
            bitmap = new Bitmap(
                bitmapSource.PixelWidth,
                bitmapSource.PixelHeight,
                stride,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb,
                gcHandle.AddrOfPinnedObject()
            );

            hIcon = bitmap.GetHicon();
        }
        finally
        {
            bitmap?.Dispose();
            gcHandle.Free();
        }

        return hIcon;
    }
}

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Extensions;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

/// <summary>
/// A 2D hue-saturation color spectrum rendered via <see cref="WriteableBitmap"/>.
/// X axis = Hue (0-360), Y axis = Saturation (100 at top, 0 at bottom).
/// </summary>
internal class ColorSpectrum : FrameworkElement
{
    private const int BitmapSize = 256;

    private WriteableBitmap? _bitmap;
    private bool _isDragging;
    private readonly DrawingVisual _visual = new();

    /// <summary>Identifies the <see cref="Hue"/> dependency property.</summary>
    public static readonly DependencyProperty HueProperty = DependencyProperty.Register(
        nameof(Hue),
        typeof(double),
        typeof(ColorSpectrum),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHueChanged));

    /// <summary>Identifies the <see cref="Saturation"/> dependency property.</summary>
    public static readonly DependencyProperty SaturationProperty = DependencyProperty.Register(
        nameof(Saturation),
        typeof(double),
        typeof(ColorSpectrum),
        new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSaturationChanged));

    /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(ColorSpectrum),
        new PropertyMetadata(100.0, OnValueChanged));

    /// <summary>Identifies the <see cref="MinHue"/> dependency property.</summary>
    public static readonly DependencyProperty MinHueProperty = DependencyProperty.Register(
        nameof(MinHue), typeof(int), typeof(ColorSpectrum), new PropertyMetadata(0, OnSpectrumRangeChanged));

    /// <summary>Identifies the <see cref="MaxHue"/> dependency property.</summary>
    public static readonly DependencyProperty MaxHueProperty = DependencyProperty.Register(
        nameof(MaxHue), typeof(int), typeof(ColorSpectrum), new PropertyMetadata(359, OnSpectrumRangeChanged));

    /// <summary>Identifies the <see cref="MinSaturation"/> dependency property.</summary>
    public static readonly DependencyProperty MinSaturationProperty = DependencyProperty.Register(
        nameof(MinSaturation), typeof(int), typeof(ColorSpectrum), new PropertyMetadata(0, OnSpectrumRangeChanged));

    /// <summary>Identifies the <see cref="MaxSaturation"/> dependency property.</summary>
    public static readonly DependencyProperty MaxSaturationProperty = DependencyProperty.Register(
        nameof(MaxSaturation), typeof(int), typeof(ColorSpectrum), new PropertyMetadata(100, OnSpectrumRangeChanged));

    /// <summary>Gets or sets the selected hue (0-360).</summary>
    public double Hue
    {
        get => (double)GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    /// <summary>Gets or sets the selected saturation (0-100).</summary>
    public double Saturation
    {
        get => (double)GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    /// <summary>Gets or sets the current brightness value (0-100) used to tint the spectrum.</summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int MinHue { get => (int)GetValue(MinHueProperty); set => SetValue(MinHueProperty, value); }
    public int MaxHue { get => (int)GetValue(MaxHueProperty); set => SetValue(MaxHueProperty, value); }
    public int MinSaturation { get => (int)GetValue(MinSaturationProperty); set => SetValue(MinSaturationProperty, value); }
    public int MaxSaturation { get => (int)GetValue(MaxSaturationProperty); set => SetValue(MaxSaturationProperty, value); }

    public ColorSpectrum()
    {
        AddVisualChild(_visual);
        AddLogicalChild(_visual);
        ClipToBounds = true;
        Cursor = Cursors.Cross;
        Focusable = true;
    }

    protected override int VisualChildrenCount => 1;

    protected override Visual GetVisualChild(int index) => _visual;

    protected override Size MeasureOverride(Size availableSize)
    {
        var side = double.IsInfinity(availableSize.Width) ? BitmapSize : Math.Min(availableSize.Width, availableSize.Height);
        if (double.IsInfinity(side))
        {
            side = BitmapSize;
        }

        return new Size(side, side);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var side = Math.Min(finalSize.Width, finalSize.Height);
        RegenerateBitmap();
        Render();
        return new Size(side, side);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        RegenerateBitmap();
        Render();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        _isDragging = true;
        CaptureMouse();
        UpdateFromMouse(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_isDragging)
        {
            UpdateFromMouse(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_isDragging)
        {
            _isDragging = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        double step = (e.KeyboardDevice.Modifiers & ModifierKeys.Shift) != 0 ? 10 : 1;

        switch (e.Key)
        {
            case Key.Left:
                SetCurrentValue(HueProperty, Math.Max(MinHue, Hue - step));
                e.Handled = true;
                break;
            case Key.Right:
                SetCurrentValue(HueProperty, Math.Min(MaxHue, Hue + step));
                e.Handled = true;
                break;
            case Key.Up:
                SetCurrentValue(SaturationProperty, Math.Min(MaxSaturation, Saturation + step));
                e.Handled = true;
                break;
            case Key.Down:
                SetCurrentValue(SaturationProperty, Math.Max(MinSaturation, Saturation - step));
                e.Handled = true;
                break;
        }
    }

    private void UpdateFromMouse(Point position)
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0)
        {
            return;
        }

        var nx = Math.Clamp(position.X / w, 0, 1);
        var ny = Math.Clamp(position.Y / h, 0, 1);

        SetCurrentValue(HueProperty, MinHue + (nx * (MaxHue - MinHue)));
        SetCurrentValue(SaturationProperty, MaxSaturation - (ny * (MaxSaturation - MinSaturation)));
    }

    private void RegenerateBitmap()
    {
        var w = (int)ActualWidth;
        var h = (int)ActualHeight;
        if (w <= 0 || h <= 0)
        {
            return;
        }

        if (_bitmap == null || _bitmap.PixelWidth != w || _bitmap.PixelHeight != h)
        {
            _bitmap = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
        }

        var stride = w * 4;
        var pixels = new byte[stride * h];
        var value = (float)Value;
        var minH = MinHue;
        var maxH = MaxHue;
        var minS = MinSaturation;
        var maxS = MaxSaturation;
        var hueRange = maxH - minH;
        var satRange = maxS - minS;

        for (var y = 0; y < h; y++)
        {
            var sat = (float)(maxS - ((double)y / (h - 1)) * satRange);
            for (var x = 0; x < w; x++)
            {
                var hue = (float)(minH + ((double)x / (w - 1)) * hueRange);
                var (r, g, b) = ColorExtensions.FromHsvToRgb(hue, sat, value);
                var idx = (y * stride) + (x * 4);
                pixels[idx + 0] = ClampByte(b);
                pixels[idx + 1] = ClampByte(g);
                pixels[idx + 2] = ClampByte(r);
                pixels[idx + 3] = 255;
            }
        }

        _bitmap.WritePixels(new Int32Rect(0, 0, w, h), pixels, stride, 0);
    }

    private void Render()
    {
        using var dc = _visual.RenderOpen();
        var w = ActualWidth;
        var h = ActualHeight;

        if (_bitmap != null && w > 0 && h > 0)
        {
            // Draw the spectrum bitmap
            dc.DrawImage(_bitmap, new Rect(0, 0, w, h));

            // Draw a rounded-corner clip border
            var clipGeometry = new RectangleGeometry(new Rect(0, 0, w, h), 6, 6);
            dc.PushClip(clipGeometry);
            dc.Pop();

            // Draw crosshair at current selection
            var hueRange = MaxHue - MinHue;
            var satRange = MaxSaturation - MinSaturation;
            var cx = hueRange > 0 ? ((Hue - MinHue) / hueRange) * w : 0;
            var cy = satRange > 0 ? ((MaxSaturation - Saturation) / satRange) * h : 0;
            cx = Math.Clamp(cx, 0, w);
            cy = Math.Clamp(cy, 0, h);

            // Outer ring (dark)
            dc.DrawEllipse(null, new Pen(Brushes.Black, 2), new Point(cx, cy), 7, 7);
            // Inner ring (white)
            dc.DrawEllipse(null, new Pen(Brushes.White, 1.5), new Point(cx, cy), 6, 6);
        }
    }

    private static void OnHueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorSpectrum spectrum)
        {
            spectrum.Render();
        }
    }

    private static void OnSaturationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorSpectrum spectrum)
        {
            spectrum.Render();
        }
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorSpectrum spectrum)
        {
            spectrum.RegenerateBitmap();
            spectrum.Render();
        }
    }

    private static void OnSpectrumRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorSpectrum spectrum)
        {
            spectrum.RegenerateBitmap();
            spectrum.Render();
        }
    }

    private static byte ClampByte(int value) => (byte)Math.Clamp(value, 0, 255);
}

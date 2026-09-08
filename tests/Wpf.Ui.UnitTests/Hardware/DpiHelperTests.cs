// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Hardware;

namespace Wpf.Ui.UnitTests.Hardware;

public sealed class DpiHelperTests
{
    [Fact]
    public void NormalizeNativeDpi_ZeroValue_ReturnsDefaultDpi()
    {
        int normalized = DpiHelper.NormalizeNativeDpi(0);

        Assert.Equal(DpiHelper.DefaultDpi, normalized);
    }

    [Fact]
    public void NormalizeNativeDpi_NegativeValue_ReturnsDefaultDpi()
    {
        int normalized = DpiHelper.NormalizeNativeDpi(-1);

        Assert.Equal(DpiHelper.DefaultDpi, normalized);
    }

    [Fact]
    public void NormalizeNativeDpi_PositiveValue_ReturnsThatValue()
    {
        int normalized = DpiHelper.NormalizeNativeDpi(120);

        Assert.Equal(120, normalized);
    }
}

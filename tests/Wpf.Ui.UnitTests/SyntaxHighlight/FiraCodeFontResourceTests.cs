// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Media;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.SyntaxHighlight;

public sealed class FiraCodeFontResourceTests
{
    [Fact]
    public void FiraCodeResource_ResolvesToTheBundledFiraCodeGlyphTypeface()
    {
        WpfTestHost.Invoke(() =>
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/Wpf.Ui.SyntaxHighlight;component/SyntaxHighlight.xaml"),
            };

            var fontFamily = Assert.IsType<FontFamily>(dictionary["FiraCode"]);
            var typeface = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            Assert.True(typeface.TryGetGlyphTypeface(out GlyphTypeface glyphTypeface));
            Assert.Contains(
                glyphTypeface.FamilyNames.Values,
                name => name.Contains("Fira Code", StringComparison.Ordinal));
        });
    }
}

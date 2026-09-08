// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Linq;
using System.Windows.Documents;
using System.Windows.Media;
using Wpf.Ui.SyntaxHighlight;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.SyntaxHighlight;

public sealed class HighlighterTests
{
    [Fact]
    public void FormatAsParagraph_OrdinaryCSharpStatement_PreservesFullText()
    {
        const string code = "var x = 1;";

        string text = WpfTestHost.Invoke(() =>
            ExtractText(Highlighter.FormatAsParagraph(code, SyntaxLanguage.CSHARP))
        );

        Assert.Equal(code, text);
    }

    [Fact]
    public void FormatAsParagraph_XamlTextNodeBetweenTags_PreservesFullText()
    {
        const string code = "<TextBlock>Hello</TextBlock>";

        string text = WpfTestHost.Invoke(() =>
            ExtractText(Highlighter.FormatAsParagraph(code, SyntaxLanguage.XAML))
        );

        Assert.Equal(code, text);
    }

    [Fact]
    public void FormatAsParagraph_LeadingInterTokenAndTrailingWhitespace_PreservesFullText()
    {
        const string code = "   spaced   out   text   ";

        string text = WpfTestHost.Invoke(() =>
            ExtractText(Highlighter.FormatAsParagraph(code, SyntaxLanguage.XAML))
        );

        Assert.Equal(code, text);
    }

    [Fact]
    public void FormatAsParagraph_CommentSurroundedByText_PreservesFullText()
    {
        const string code = "before // a comment\nafter";

        string text = WpfTestHost.Invoke(() =>
            ExtractText(Highlighter.FormatAsParagraph(code, SyntaxLanguage.CSHARP))
        );

        Assert.Equal(code, text);
    }

    [Fact]
    public void FormatAsParagraph_QuotedStringSurroundedByText_PreservesFullText()
    {
        const string code = "var s = \"hello world\";";

        string text = WpfTestHost.Invoke(() =>
            ExtractText(Highlighter.FormatAsParagraph(code, SyntaxLanguage.CSHARP))
        );

        Assert.Equal(code, text);
    }

    [Fact]
    public void FormatAsParagraph_MalformedUnclosedTag_PreservesFullText()
    {
        const string code = "<Broken text without a closing bracket";

        string text = WpfTestHost.Invoke(() =>
            ExtractText(Highlighter.FormatAsParagraph(code, SyntaxLanguage.XAML))
        );

        Assert.Equal(code, text);
    }

    [Fact]
    public void FormatAsParagraph_EmptyInput_ProducesNoInlines()
    {
        Paragraph paragraph = WpfTestHost.Invoke(() =>
            Highlighter.FormatAsParagraph(string.Empty, SyntaxLanguage.XAML)
        );

        Assert.Empty(paragraph.Inlines);
    }

    [Fact]
    public void FormatAsParagraph_ColorsCommentDifferentlyFromSurroundingText_WithoutChangingText()
    {
        const string code = "before // a comment\nafter";

        (string text, bool leadingTextUncolored, bool commentColoredOrange, bool trailingTextPresent) =
            WpfTestHost.Invoke(() =>
            {
                Paragraph paragraph = Highlighter.FormatAsParagraph(code, SyntaxLanguage.CSHARP);
                var runs = paragraph.Inlines.OfType<Run>().ToList();

                return (
                    ExtractText(paragraph),
                    runs.Any(r => r.Text == "before " && !Equals(r.Foreground, Brushes.Orange)),
                    runs.Any(r => r.Text.Contains("// a comment") && Equals(r.Foreground, Brushes.Orange)),
                    runs.Any(r => r.Text == "after")
                );
            });

        Assert.Equal(code, text);
        Assert.True(leadingTextUncolored);
        Assert.True(commentColoredOrange);
        Assert.True(trailingTextPresent);
    }

    private static string ExtractText(Paragraph paragraph) =>
        string.Concat(paragraph.Inlines.OfType<Run>().Select(run => run.Text));
}

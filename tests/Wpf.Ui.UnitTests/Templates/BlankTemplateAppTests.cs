// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Wpf.Ui.UnitTests.Templates;

public sealed class BlankTemplateAppTests
{
    [Fact]
    public void ConfigureServices_DoesNotThrow()
    {
        var configureServicesBody = GetConfigureServicesBody();

        var throwStatements = configureServicesBody.DescendantNodesAndSelf().OfType<ThrowStatementSyntax>();

        Assert.Empty(throwStatements);
    }

    private static BlockSyntax GetConfigureServicesBody()
    {
        var source = File.ReadAllText(FindBlankTemplateAppSource());
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        var configureServicesCall = root
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(invocation =>
                invocation.Expression is MemberAccessExpressionSyntax memberAccess
                && memberAccess.Name.Identifier.Text == "ConfigureServices");

        var lambda = configureServicesCall.ArgumentList.Arguments
            .Select(argument => argument.Expression)
            .OfType<ParenthesizedLambdaExpressionSyntax>()
            .Single();

        return Assert.IsType<BlockSyntax>(lambda.Body);
    }

    private static string FindBlankTemplateAppSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Wpf.Ui.sln")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException(
                "Could not locate the repository root above the test output directory.");
        }

        return Path.Combine(directory.FullName, "src", "Wpf.Ui.Extension.Template.Blank", "App.xaml.cs");
    }
}

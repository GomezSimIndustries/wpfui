// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Runtime.CompilerServices;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.UnitTests.TestHelpers;
using CodeBlockControl = Wpf.Ui.SyntaxHighlight.Controls.CodeBlock;

namespace Wpf.Ui.UnitTests.Controls;

[Collection("ThemeState")]
public sealed class CodeBlockThemeLifetimeTests : IDisposable
{
    public void Dispose()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
        });
    }

    [Fact]
    public void ThemeChange_WhileUnloaded_DoesNotUpdateSyntax()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
            var codeBlock = new CountingCodeBlock { Content = "int x = 1;" };
            codeBlock.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            int baseline = codeBlock.UpdateCount;

            codeBlock.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);

            Assert.Equal(baseline, codeBlock.UpdateCount);
        });
    }

    [Fact]
    public void ThemeChange_AfterReload_UpdatesSyntaxExactlyOnce()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
            var codeBlock = new CountingCodeBlock { Content = "int x = 1;" };
            codeBlock.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            codeBlock.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            codeBlock.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            int baseline = codeBlock.UpdateCount;

            ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);

            Assert.Equal(baseline + 1, codeBlock.UpdateCount);
        });
    }

    [Fact]
    public void ThemeChange_AfterLoadedRaisedTwiceWithoutUnload_UpdatesSyntaxExactlyOnce()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
            var codeBlock = new CountingCodeBlock { Content = "int x = 1;" };
            codeBlock.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            codeBlock.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            int baseline = codeBlock.UpdateCount;

            ApplicationThemeManager.Apply(ApplicationTheme.Light, updateAccent: false);

            Assert.Equal(baseline + 1, codeBlock.UpdateCount);
        });
    }

    [Fact]
    public void Unloaded_AllowsTheControlToBeCollected()
    {
        WpfTestHost.Invoke(() =>
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, updateAccent: false);
            WeakReference weakReference = CreateLoadedThenUnloadedCodeBlock();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(weakReference.IsAlive);
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateLoadedThenUnloadedCodeBlock()
    {
        var codeBlock = new CodeBlockControl { Content = "int x = 1;" };
        codeBlock.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
        codeBlock.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

        return new WeakReference(codeBlock);
    }

    private sealed class CountingCodeBlock : CodeBlockControl
    {
        public int UpdateCount { get; private set; }

        protected override void UpdateSyntax()
        {
            UpdateCount++;
            base.UpdateSyntax();
        }
    }
}

// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Input;
using System.Windows.Markup;
using Wpf.Ui.Controls;

namespace Wpf.Ui.UnitTests.Controls;

public class BreadcrumbBarCommandInvocationTests
{
    private static readonly string BreadcrumbBarTemplatePath = Path.GetFullPath(
        Path.Combine(
            Path.GetDirectoryName(ThisFilePath())!,
            "..",
            "..",
            "..",
            "..",
            "src",
            "Wpf.Ui",
            "Controls",
            "BreadcrumbBar",
            "BreadcrumbBar.xaml"
        )
    );

    public enum CommandAcceptance
    {
        Both,
        ItemOnly,
        NullOnly,
        Neither,
    }

    [Theory]
    [InlineData(CommandAcceptance.Both)]
    [InlineData(CommandAcceptance.ItemOnly)]
    [InlineData(CommandAcceptance.NullOnly)]
    [InlineData(CommandAcceptance.Neither)]
    public void ClickingAnItem_ExecutesTheCommandAtMostOnceWithTheContractedArgument(CommandAcceptance acceptance)
    {
        StaThread.Run(() =>
        {
            var item = "Documents";
            var (window, bar) = CreateHostedBreadcrumbBar(item);

            try
            {
                var command = new RecordingCommand(CanExecuteFor(acceptance));
                bar.Command = command;

                bar.TemplateButtonCommand.Execute(item);

                Assert.Equal(ExpectedExecutions(acceptance, item), command.Executions);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ClickingAnItem_RaisesItemClickedExactlyOnceRegardlessOfCommandOutcome()
    {
        StaThread.Run(() =>
        {
            var item = "Documents";
            var (window, bar) = CreateHostedBreadcrumbBar(item);

            try
            {
                bar.Command = new RecordingCommand(CanExecuteFor(CommandAcceptance.Both));

                var raised = 0;
                bar.ItemClicked += (_, _) => raised++;

                bar.TemplateButtonCommand.Execute(item);

                Assert.Equal(1, raised);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void KeyboardActivation_OnTheRealItemTemplate_ExecutesTheCommandExactlyOnceWithTheClickedItem()
    {
        StaThread.Run(() =>
        {
            var item = "Documents";
            var (window, bar) = CreateHostedBreadcrumbBar(item, "Reports");

            try
            {
                var command = new RecordingCommand(CanExecuteFor(CommandAcceptance.Both));
                bar.Command = command;

                var container = (BreadcrumbBarItem)bar.ItemContainerGenerator.ContainerFromItem(item);
                container.ApplyTemplate();
                var button = (System.Windows.Controls.Button)container.Template.FindName("Button", container);

                button.Focus();

                var source = System.Windows.PresentationSource.FromVisual(button);
                button.RaiseEvent(
                    new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Space)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    }
                );
                button.RaiseEvent(
                    new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Space)
                    {
                        RoutedEvent = Keyboard.KeyUpEvent,
                    }
                );

                Assert.Single(command.Executions);
                Assert.Equal(item, command.Executions[0]);
            }
            finally
            {
                window.Close();
            }
        });
    }

    private static (Window Window, Wpf.Ui.Controls.BreadcrumbBar Bar) CreateHostedBreadcrumbBar(
        params object[] items
    )
    {
        var xaml = File.ReadAllText(BreadcrumbBarTemplatePath)
            .Replace("clr-namespace:Wpf.Ui.Controls\"", "clr-namespace:Wpf.Ui.Controls;assembly=Wpf.Ui\"");
        var styles = (ResourceDictionary)XamlReader.Parse(xaml);

        var bar = new Wpf.Ui.Controls.BreadcrumbBar();

        foreach (var item in items)
        {
            bar.Items.Add(item);
        }

        var window = new Window
        {
            Content = bar,
            Width = 200,
            Height = 80,
            Left = -5000,
            Top = -5000,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
        };
        window.Resources.MergedDictionaries.Add(styles);
        window.Show();
        bar.UpdateLayout();

        return (window, bar);
    }

    private static string ThisFilePath([CallerFilePath] string path = "") => path;

    private static Func<object?, bool> CanExecuteFor(CommandAcceptance acceptance) =>
        acceptance switch
        {
            CommandAcceptance.Both => _ => true,
            CommandAcceptance.ItemOnly => parameter => parameter is not null,
            CommandAcceptance.NullOnly => parameter => parameter is null,
            CommandAcceptance.Neither => _ => false,
            _ => throw new ArgumentOutOfRangeException(nameof(acceptance)),
        };

    private static IReadOnlyList<object?> ExpectedExecutions(CommandAcceptance acceptance, object item) =>
        acceptance switch
        {
            CommandAcceptance.Both => new object?[] { item },
            CommandAcceptance.ItemOnly => new object?[] { item },
            CommandAcceptance.NullOnly => new object?[] { null },
            CommandAcceptance.Neither => Array.Empty<object?>(),
            _ => throw new ArgumentOutOfRangeException(nameof(acceptance)),
        };

    private static class StaThread
    {
        public static void Run(Action action)
        {
            ExceptionDispatchInfo? capturedException = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    capturedException = ExceptionDispatchInfo.Capture(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            capturedException?.Throw();
        }
    }

    private sealed class RecordingCommand : ICommand
    {
        private readonly Func<object?, bool> _canExecute;

        public RecordingCommand(Func<object?, bool> canExecute)
        {
            _canExecute = canExecute;
        }

        public List<object?> Executions { get; } = new();

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => _canExecute(parameter);

        public void Execute(object? parameter) => Executions.Add(parameter);
    }
}

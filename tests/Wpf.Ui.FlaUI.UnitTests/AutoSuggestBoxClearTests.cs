// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using FlaUI.UIA3;
using Xunit;
using TextBox = System.Windows.Controls.TextBox;
using Window = System.Windows.Window;

#pragma warning disable SA1402 // File may only contain a single type

namespace Wpf.Ui.FlaUI.UnitTests;

/// <summary>
/// Exercises the deterministic clearing behavior of <see cref="AutoSuggestBox.Enter"/> against a real, in-process window.
/// </summary>
public sealed class AutoSuggestBoxClearTests : IDisposable
{
    private const string TargetAutomationId = "ClearTarget";

    private readonly Thread _uiThread;
    private readonly ManualResetEventSlim _dispatcherReady = new(false);
    private Dispatcher? _dispatcher;
    private Window? _window;
    private UIA3Automation? _automation;

    public AutoSuggestBoxClearTests()
    {
        _uiThread = new Thread(RunDispatcher) { IsBackground = true };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();
        _dispatcherReady.Wait();
    }

    [Fact]
    public void Enter_ClearsThroughValuePattern_WhenSupported()
    {
        (AutoSuggestBox element, TextBox control) = HostControl(() => new TextBox { Text = "existing" });

        element.Enter(string.Empty);

        Assert.Equal(string.Empty, Invoke(() => control.Text));
    }

    [Fact]
    public void Enter_ClearsThroughKeyboardFallback_WhenValuePatternUnsupported()
    {
        (AutoSuggestBox element, TextBox control) = HostControl(() => new PeerlessTextBox { Text = "existing" });

        element.Enter(string.Empty);

        Assert.Equal(string.Empty, Invoke(() => control.Text));
    }

    [Fact]
    public void Enter_Throws_WhenClearingDoesNotTakeEffect()
    {
        (AutoSuggestBox element, _) = HostControl(() => new StubbornTextBox());

        Assert.Throws<InvalidOperationException>(() => element.Enter(string.Empty));
    }

    public void Dispose()
    {
        if (_dispatcher is not null)
        {
            Invoke(() => _window?.Close());
            _dispatcher.InvokeShutdown();
        }

        _automation?.Dispose();
        _dispatcherReady.Dispose();
    }

    private (AutoSuggestBox Element, TextBox Control) HostControl(Func<TextBox> createControl)
    {
        TextBox control = Invoke(() =>
        {
            TextBox created = createControl();
            AutomationProperties.SetAutomationId(created, TargetAutomationId);
            return created;
        });

        Invoke(() => _window?.Close());

        _window = Invoke(() =>
        {
            var window = new Window
            {
                Content = control,
                Width = 260,
                Height = 120,
                Left = 0,
                Top = 0,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Topmost = true,
                ShowActivated = true,
            };
            window.Show();
            window.Activate();
            return window;
        });

        nint hwnd = Invoke(() => new WindowInteropHelper(_window).Handle);

        _automation?.Dispose();
        _automation = new UIA3Automation();

        global::FlaUI.Core.AutomationElements.AutomationElement windowElement = _automation.FromHandle(hwnd);
        global::FlaUI.Core.AutomationElements.AutomationElement? controlElement = windowElement.FindFirstDescendant(cf =>
            cf.ByAutomationId(TargetAutomationId)
        );

        Assert.NotNull(controlElement);

        return (new AutoSuggestBox(controlElement!.FrameworkAutomationElement), control);
    }

    private void Invoke(Action action) => _dispatcher!.Invoke(action);

    private T Invoke<T>(Func<T> func) => _dispatcher!.Invoke(func);

    private void RunDispatcher()
    {
        _dispatcher = Dispatcher.CurrentDispatcher;
        _dispatcherReady.Set();
        Dispatcher.Run();
    }
}

/// <summary>
/// A text box whose automation peer exposes no value pattern.
/// </summary>
file sealed class PeerlessTextBox : TextBox
{
    protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
}

/// <summary>
/// A text box whose text always coerces back to its original value.
/// </summary>
file sealed class StubbornTextBox : TextBox
{
    static StubbornTextBox()
    {
        TextProperty.OverrideMetadata(
            typeof(StubbornTextBox),
            new FrameworkPropertyMetadata(
                "existing",
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                null,
                CoerceText
            )
        );
    }

    private static object CoerceText(DependencyObject d, object baseValue) => "existing";
}

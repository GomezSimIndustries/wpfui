// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using FlaUI.Core;

namespace Wpf.Ui.Gallery.IntegrationTests;

public sealed class TestedApplicationTests
{
    private const string ExistingExecutableName = "Wpf.Ui.Gallery.IntegrationTests.exe";

    [Fact]
    public async Task InitializeAsync_CalledAgainWithoutDispose_DisposesThePreviousHandleAndResetsAutomation()
    {
        List<FakeApplicationHandle> handles = [];
        TestedApplication fixture = new(ExistingExecutableName, _ =>
        {
            FakeApplicationHandle handle = new();
            handles.Add(handle);
            return handle;
        });

        await fixture.InitializeAsync();
        AutomationBase? firstAutomation = fixture.CurrentAutomation;
        _ = fixture.MainWindow;

        await fixture.InitializeAsync();
        AutomationBase? secondAutomation = fixture.CurrentAutomation;
        _ = fixture.MainWindow;

        handles.Should().HaveCount(2);
        secondAutomation.Should().NotBeSameAs(firstAutomation, "because reinitializing must not reuse a disposed automation instance");
        handles[0].CloseCallCount.Should().Be(1);
        handles[0].DisposeCallCount.Should().Be(1);
        handles[0].GetMainWindowCallCount.Should().Be(1);
        handles[1].GetMainWindowCallCount.Should().Be(1);

        await fixture.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ThenInitializeAsync_ObtainsAFreshWindowWrapper()
    {
        List<FakeApplicationHandle> handles = [];
        TestedApplication fixture = new(ExistingExecutableName, _ =>
        {
            FakeApplicationHandle handle = new();
            handles.Add(handle);
            return handle;
        });

        await fixture.InitializeAsync();
        _ = fixture.MainWindow;
        await fixture.DisposeAsync();

        await fixture.InitializeAsync();
        _ = fixture.MainWindow;

        handles.Should().HaveCount(2);
        handles[1].GetMainWindowCallCount.Should().Be(1, "because a disposed fixture must resolve a new window wrapper on reinitialization");

        await fixture.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_WhenNeverInitialized_DoesNotThrow()
    {
        TestedApplication fixture = new(ExistingExecutableName, _ => new FakeApplicationHandle());

        Func<Task> act = async () => await fixture.DisposeAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisposeAsync_WhenProcessNeverReportsExiting_ThrowsTimeoutAndStillDisposesTheHandle()
    {
        FakeApplicationHandle handle = new(hasExited: () => false);
        TestedApplication fixture = new(ExistingExecutableName, _ => handle);

        await fixture.InitializeAsync();

        Func<Task> act = async () => await fixture.DisposeAsync();

        await act.Should().ThrowAsync<TimeoutException>();
        handle.CloseCallCount.Should().Be(1);
        handle.DisposeCallCount.Should().Be(1);
    }

    [Fact]
    public async Task InitializeAsync_WhenExecutableIsMissing_ThrowsAndLeavesNoApplicationOrWindow()
    {
        TestedApplication fixture = new("Definitely.Not.Present.exe", _ => new FakeApplicationHandle());

        Func<Task> act = async () => await fixture.InitializeAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
        fixture.Application.Should().BeNull();
        fixture.MainWindow.Should().BeNull();
    }

    private sealed class FakeApplicationHandle(Func<bool>? hasExited = null) : IApplicationHandle
    {
        private readonly Func<bool> hasExited = hasExited ?? (() => true);

        public int CloseCallCount { get; private set; }

        public int DisposeCallCount { get; private set; }

        public int GetMainWindowCallCount { get; private set; }

        public Application? Underlying => null;

        public bool HasExited => hasExited();

        public void Close() => CloseCallCount++;

        public Window? GetMainWindow(AutomationBase automation)
        {
            GetMainWindowCallCount++;
            return null;
        }

        public void Dispose() => DisposeCallCount++;
    }
}

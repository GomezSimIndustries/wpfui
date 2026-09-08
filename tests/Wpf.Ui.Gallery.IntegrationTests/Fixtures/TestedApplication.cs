// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using FlaUI.Core;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

namespace Wpf.Ui.Gallery.IntegrationTests.Fixtures;

/// <summary>
/// Class managing the lifecycle of the tested application implementing <see cref="IAsyncLifetime"/>.
/// Uses <see cref="UIA3Automation"/> for UI automation.
/// </summary>
public sealed class TestedApplication : IAsyncLifetime
{
    private const string DefaultExecutableName = "Wpf.Ui.Gallery.exe";

    private readonly string executableName;

    private readonly Func<string, IApplicationHandle> launchApplication;

    private readonly ResettableCache<Window> mainWindowCache = new();

    private AutomationBase? automation;

    private IApplicationHandle? app;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestedApplication"/> class.
    /// </summary>
    public TestedApplication()
        : this(DefaultExecutableName, LaunchApplication) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestedApplication"/> class with a substitute executable name and launcher.
    /// </summary>
    /// <param name="executableName">The executable file name to resolve next to the test assembly.</param>
    /// <param name="launchApplication">The function used to launch the application and produce its handle.</param>
    internal TestedApplication(string executableName, Func<string, IApplicationHandle> launchApplication)
    {
        this.executableName = executableName;
        this.launchApplication = launchApplication;
    }

    /// <summary>
    /// Gets the wrapper for an application which should be automated.
    /// </summary>
    public Application? Application => app?.Underlying;

    /// <summary>
    /// Gets the main window of the applications process.
    /// </summary>
    public Window? MainWindow => mainWindowCache.GetOrCompute(
        () => app is not null && automation is not null ? app.GetMainWindow(automation) : null
    );

    /// <summary>
    /// Gets the automation instance currently backing this fixture.
    /// </summary>
    internal AutomationBase? CurrentAutomation => automation;

    /// <inheritdoc />
    public ValueTask InitializeAsync()
    {
        ResetState();

        string path = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            executableName
        );

        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"Unable to find the application executable at path \"{path}\"."
            );
        }

        automation = new UIA3Automation();
        app = launchApplication(path);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        mainWindowCache.Reset();

        bool shutdownTimedOut = false;

        if (app is not null)
        {
            if (!app.HasExited)
            {
                app.Close();
            }

            IApplicationHandle current = app;

            // ReSharper disable once AccessToDisposedClosure
            RetryResult<bool> shutdown = Retry.WhileFalse(
                () => current.HasExited,
                TimeSpan.FromSeconds(2),
                ignoreException: true
            );
            shutdownTimedOut = !shutdown.Result;

            app.Dispose();
            app = null;
        }

        automation?.Dispose();
        automation = null;

        if (shutdownTimedOut)
        {
            throw new TimeoutException(
                $"The {executableName} process did not report exiting within the bounded shutdown retry."
            );
        }

        return ValueTask.CompletedTask;
    }

    private static IApplicationHandle LaunchApplication(string path)
    {
        Application application = Application.Launch(path);
        application.WaitWhileMainHandleIsMissing(TimeSpan.FromMinutes(1));

        return new FlaUiApplicationHandle(application);
    }

    private void ResetState()
    {
        mainWindowCache.Reset();

        if (app is not null)
        {
            app.Close();
            app.Dispose();
            app = null;
        }

        automation?.Dispose();
        automation = null;
    }

    private sealed class FlaUiApplicationHandle(Application application) : IApplicationHandle
    {
        public Application? Underlying => application;

        public bool HasExited => application.HasExited;

        public void Close() => application.Close();

        public Window? GetMainWindow(AutomationBase automation) => application.GetMainWindow(automation);

        public void Dispose() => application.Dispose();
    }
}

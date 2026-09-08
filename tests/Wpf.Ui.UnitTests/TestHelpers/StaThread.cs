// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Threading;

namespace Wpf.Ui.UnitTests.TestHelpers;

/// <summary>Runs an action on its own STA thread, isolated from the shared <see cref="WpfTestHost"/> dispatcher.</summary>
internal static class StaThread
{
    /// <summary>Starts a dedicated STA thread, runs the action on it, and rethrows any exception the action raised.</summary>
    public static void Run(Action action)
    {
        WpfTestHost.EnsureInitialized();

        ExceptionDispatchInfo? capturedException = null;
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                capturedException = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                dispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);

        lock (WpfTestHost.ExclusivityGate)
        {
            thread.Start();
            thread.Join();
        }

        capturedException?.Throw();
    }
}

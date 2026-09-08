// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.Tray;

namespace Wpf.Ui.UnitTests.Tray;

/// <summary>
/// Swaps <see cref="TrayManager"/>'s shell gateway and hook window factory for fakes, restoring the originals on dispose.
/// </summary>
internal sealed class TrayManagerFixture : IDisposable
{
    private readonly IShellNotifyIconGateway _originalGateway;
    private readonly ITrayHookWindowFactory _originalFactory;
    private readonly INativeIconHandleGateway _originalIconHandleGateway;

    public TrayManagerFixture(
        IShellNotifyIconGateway gateway,
        ITrayHookWindowFactory factory,
        INativeIconHandleGateway? iconHandleGateway = null)
    {
        _originalGateway = TrayManager.ShellGateway;
        _originalFactory = TrayManager.HookWindowFactory;
        _originalIconHandleGateway = TrayManager.IconHandleGateway;

        TrayManager.ShellGateway = gateway;
        TrayManager.HookWindowFactory = factory;
        TrayManager.IconHandleGateway = iconHandleGateway ?? new FakeNativeIconHandleGateway();
    }

    public void Dispose()
    {
        TrayManager.ShellGateway = _originalGateway;
        TrayManager.HookWindowFactory = _originalFactory;
        TrayManager.IconHandleGateway = _originalIconHandleGateway;
    }
}

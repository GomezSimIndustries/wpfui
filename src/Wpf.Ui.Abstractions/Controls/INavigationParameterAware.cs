// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.Abstractions.Controls;

/// <summary>
/// Extends <see cref="INavigationAware"/> to receive navigation parameters.
/// Implement this interface when your page needs to receive data during navigation
/// without relying on DataContext (useful for pages implementing <see cref="INavigableView{T}"/>).
/// </summary>
public interface INavigationParameterAware : INavigationAware
{
    /// <summary>
    /// Asynchronously handles the event that is fired after the component is navigated to,
    /// providing the navigation parameter.
    /// </summary>
    /// <param name="parameter">The navigation parameter (e.g., TargetDataContext from NavigationViewItem).</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task OnNavigatedToAsync(object? parameter);
}

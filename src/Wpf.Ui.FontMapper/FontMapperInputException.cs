// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.FontMapper;

/// <summary>
/// Thrown when the generator inputs are missing or inconsistent.
/// </summary>
public sealed class FontMapperInputException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FontMapperInputException"/> class.
    /// </summary>
    /// <param name="message">A message describing the invalid input.</param>
    public FontMapperInputException(string message)
        : base(message)
    {
    }
}

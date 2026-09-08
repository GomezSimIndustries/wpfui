// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.FontMapper;

/// <summary>
/// The outcome of a single generation run.
/// </summary>
/// <param name="RegularFilePath">The path the regular icon enum was written to.</param>
/// <param name="FilledFilePath">The path the filled icon enum was written to.</param>
/// <param name="Version">The version recorded in the generated enum documentation.</param>
/// <param name="RemovedKeys">The icon names present in only one of the two sets, and so excluded from both.</param>
public sealed record FontMapperResult(
    string RegularFilePath,
    string FilledFilePath,
    string Version,
    IReadOnlyList<string> RemovedKeys);

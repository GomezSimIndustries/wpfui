// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

namespace Wpf.Ui.FontMapper;

/// <summary>
/// Validates <see cref="FontMapperOptions"/> before generation starts.
/// </summary>
public static class FontMapperInputValidator
{
    /// <summary>
    /// Validates that the supplied options identify one consistent source version and its packaged fonts.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <exception cref="FontMapperInputException">Thrown when a required input is missing or inconsistent.</exception>
    public static void Validate(FontMapperOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Version))
        {
            throw new FontMapperInputException("A version must be specified for the generated enum documentation.");
        }

        var hasRegularLocalPath = options.RegularJsonPath is not null;
        var hasFilledLocalPath = options.FilledJsonPath is not null;

        if (hasRegularLocalPath != hasFilledLocalPath)
        {
            throw new FontMapperInputException(
                "Local JSON paths for both the regular and filled icon sets must be supplied together, or neither.");
        }

        if (!hasRegularLocalPath)
        {
            if (string.IsNullOrWhiteSpace(options.Revision))
            {
                throw new FontMapperInputException(
                    "Either local JSON paths for both icon sets or an upstream revision must be supplied.");
            }

            ValidateRevision(options.Revision);
        }
        else
        {
            if (!File.Exists(options.RegularJsonPath))
            {
                throw new FontMapperInputException($"Regular icon JSON was not found at \"{options.RegularJsonPath}\".");
            }

            if (!File.Exists(options.FilledJsonPath))
            {
                throw new FontMapperInputException($"Filled icon JSON was not found at \"{options.FilledJsonPath}\".");
            }
        }

        var regularFontPath = Path.Combine(options.FontsRootDirectory, IconFontDefinitions.Regular.PackagedFontFileName);
        var filledFontPath = Path.Combine(options.FontsRootDirectory, IconFontDefinitions.Filled.PackagedFontFileName);

        if (!File.Exists(regularFontPath))
        {
            throw new FontMapperInputException($"The packaged regular icon font was not found at \"{regularFontPath}\".");
        }

        if (!File.Exists(filledFontPath))
        {
            throw new FontMapperInputException($"The packaged filled icon font was not found at \"{filledFontPath}\".");
        }
    }

    private static void ValidateRevision(string revision)
    {
        if (IsMovingReference(revision) || revision.Length is < 7 or > 40 || !revision.All(Uri.IsHexDigit))
        {
            throw new FontMapperInputException(
                $"\"{revision}\" is not a pinned commit revision. Use the full or short commit SHA, not a branch name.");
        }
    }

    private static bool IsMovingReference(string revision) =>
        revision.Equals("main", StringComparison.OrdinalIgnoreCase)
        || revision.Equals("master", StringComparison.OrdinalIgnoreCase)
        || revision.Equals("head", StringComparison.OrdinalIgnoreCase)
        || revision.Contains('/');
}

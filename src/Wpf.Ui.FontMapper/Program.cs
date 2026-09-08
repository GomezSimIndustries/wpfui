// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using Wpf.Ui.FontMapper;

Console.WriteLine("Fluent System Icons Mapper");

string? version = null;
string? revision = null;
string? regularJsonPath = null;
string? filledJsonPath = null;
string? fontsRootDirectory = null;
string? outputDirectory = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--version" when i + 1 < args.Length:
            version = args[++i];
            break;
        case "--revision" when i + 1 < args.Length:
            revision = args[++i];
            break;
        case "--regular-json" when i + 1 < args.Length:
            regularJsonPath = args[++i];
            break;
        case "--filled-json" when i + 1 < args.Length:
            filledJsonPath = args[++i];
            break;
        case "--fonts-root" when i + 1 < args.Length:
            fontsRootDirectory = args[++i];
            break;
        case "--output" when i + 1 < args.Length:
            outputDirectory = args[++i];
            break;
    }
}

var workingDirectory =
    Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
    ?? throw new InvalidOperationException("Could not determine the working directory.");

try
{
    var options = new FontMapperOptions
    {
        Version = version ?? throw new FontMapperInputException("The --version argument is required."),
        Revision = revision,
        RegularJsonPath = regularJsonPath,
        FilledJsonPath = filledJsonPath,
        FontsRootDirectory = fontsRootDirectory ?? throw new FontMapperInputException("The --fonts-root argument is required."),
        OutputDirectory = outputDirectory ?? workingDirectory,
    };

    var result = await FontMapperGenerator.GenerateAsync(options);

    foreach (var removedKey in result.RemovedKeys)
    {
        Console.WriteLine($"Deleted key \"{removedKey}\" because no duplicate found in all lists");
    }

    Console.WriteLine($"Wrote to file \"{result.RegularFilePath}\"");
    Console.WriteLine($"Wrote to file \"{result.FilledFilePath}\"");
    Console.WriteLine("Done.");
}
catch (FontMapperInputException ex)
{
    Console.Error.WriteLine($"Input error: {ex.Message}");
    Environment.Exit(1);
}

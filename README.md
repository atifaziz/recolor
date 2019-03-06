# Recolor

Colors text received over standard input based on regular expression patterns.

## Requirements

- .NET Core 2.1 or later


## Installation

[Recolor is available as a .NET Core tool][nupkg] for installation:

    dotnet tool install -g recolor


## Usage

    recolor COLOR1=REGEX1 COLOR2=REGEX2 ... COLORN=REGEXN

Each line read over standard input is tested against each regular expression
pattern (REGEX) in turn. Those that match are used to highlight the matched
portion of the text in the corresponding color (COLOR). The regular
expression patterns are tested on each line only once unless a start-equal
(`*=`) instead just an equal (`=`) is used to separate the color from the 
regular expression pattern specification.

A color is specified in one of three formats:

1. FOREGROUND
2. FOREGROUND/BACKGROUND
3. HEX

Format 1 sets only the foreground color of the text whereas format 2 sets the
foreground and background colors (separated by a forward-slash to mean
foreground over background). FOREGROUND can be omitted to set just the
background. The colors themselves are specified using the names listed below.
In format 3, the colors are specified by two hex digits: the first corresponds 
to the background and the second the foreground. If only a single hex digit is 
given then it sets the foreground color. The color corresponding to each hex 
digits is shown below.

| Hex | Color Name     |
|----:|----------------|
|   0 | `Black`        |
|   1 | `DarkBlue`     |
|   2 | `DarkGreen`    |
|   3 | `DarkCyan`     |
|   4 | `DarkRed`      |
|   5 | `DarkMagenta`  |
|   6 | `DarkYellow`   |
|   7 | `Gray`         |
|   8 | `DarkGray`     |
|   9 | `Blue`         |
|   A | `Green`        |
|   B | `Cyan`         |
|   C | `Red`          |
|   D | `Magenta`      |
|   E | `Yellow`       |
|   F | `White`        |

For `REGEX` syntax, see [Regular Expression Language Quick Reference][regex].


## Building

The .NET Core SDK is the minimum requirement.

To build just the binaries on Windows, run:

    .\build.cmd

On Linux or macOS, run instead:

    ./build.sh

To build the binaries and the NuGet packages on Windows, run:

    .\pack.cmd

On Linux or macOS, run instead:

    ./pack.sh


  [regex]: http://go.microsoft.com/fwlink/?LinkId=133231
  [nupkg]: https://www.nuget.org/packages/Recolor/

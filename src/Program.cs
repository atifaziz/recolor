#region Copyright (c) 2010 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AngryArrays.Splice;
using Mannex;
using Recolor;

try
{
    Wain(args);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Trace.WriteLine(ex.ToString());
    return 1;
}

static partial class Program
{
    [DebuggerDisplay("Foreground = {Foreground}, Background = {Background}")]
    readonly record struct Color(ConsoleColor? Foreground, ConsoleColor? Background)
    {
        public void Do(Action<ConsoleColor> onForeground, Action<ConsoleColor> onBackground)
        {
            if (Background is { } bg) onBackground(bg);
            if (Foreground is { } fg) onForeground(fg);
        }

        public static Color Parse(string input)
        {
            Color color;
            if (input.Length > 0 && IsHexChar(input[0])
                && (input.Length == 1 || (input.Length == 2 && IsHexChar(input[1]))))
            {
                var n = int.Parse(input, NumberStyles.HexNumber);
                color = new Color((ConsoleColor) (n & 0xf), (ConsoleColor) (n >> 4));
            }
            else
            {
                var tokens = input.Split(StringSeparatorStock.Slash, 2);
                color = new Color(ParseConsoleColor(tokens[0]),
                                  tokens.Length > 1 ? ParseConsoleColor(tokens[1]) : null);
            }
            return color;
        }

        static bool IsHexChar(char ch) => ch is >= '0' and <= '9'
                                             or >= 'a' and <= 'f'
                                             or >= 'A' and <= 'F';

        static ConsoleColor? ParseConsoleColor(string input)
        {
            if (input.Length == 0) return null;
            if (!Regex.IsMatch(input, " *[a-zA-Z]+ *", RegexOptions.CultureInvariant))
                throw new FormatException("Color name syntax error.");
            return input.Length > 0
                 ? Enum.Parse<ConsoleColor>(input, true)
                 : null;
        }

        public void ApplyToConsole() => Do(fg => Console.ForegroundColor = fg,
                                           bg => Console.BackgroundColor = bg);
    }

    [DebuggerDisplay("{Index}...{End} ({Length})")]
    readonly record struct Run(int Index, int Length)
    {
        public int End      => Index + Length;
        public bool IsEmpty => Length == 0;
    }

    [DebuggerDisplay("Run = {Run}, Color = {Color}")]
    sealed class Markup
    {
        public Run   Run      { get; }
        public Color Color    { get; }

        public Markup(int index, int length, Color color) :
            this(new Run(index, length), color) { }

        public Markup(Run run, Color color)
        {
            Run      = run;
            Color    = color;
        }
    }

    delegate IEnumerable<Markup> Marker(string line);

    static Marker CreateMarker(Regex regex, bool all, Color color) =>
        line => from matches in new[]
                {
                    from Match m in regex.Matches(line)
                    select m
                }
                from m in all ? matches : matches.Take(1)
                select new Run(m.Index, m.Length) into run
                select new Markup(run, color);

    static void Wain(string[] args)
    {
        bool PopSwitch(string name1, string name2 = null, string name3 = null)
        {
            var i = Array.FindIndex(args, arg => arg == name1
                                              || arg == name2
                                              || arg == name3);
            var found = i >= 0;
            if (found)
                args = args.Splice(i, 1);
            return found;
        }

        var debug = PopSwitch("--debug");
        if (debug)
            Debugger.Launch();

        var verbose = PopSwitch("--verbose", "-v");

        if (PopSwitch("-?", "-h", "--help"))
        {
            ShowHelp(Console.Out);
            return;
        }

        var tail = Enumerable.ToArray(
            from arg in args
            select !arg.StartsWith("@")
                ? new[] { arg }.AsEnumerable()
                : arg.Length == 1
                    ? Enumerable.Empty<string>()
                    : ParseResponseFile(arg[1..]) into argz
            from arg in argz
            select arg);

        if (verbose && tail.Any())
        {
            Console.Error.WriteLine(FormattableString.Invariant($"Command-line arguments ({tail.Length}):"));

            foreach (var arg in tail)
                Console.Error.WriteLine("- " + arg);
        }

        var defaultColor = new Color(Console.ForegroundColor, Console.BackgroundColor);
        var markers =
            from arg in tail
            let tokens = arg.TrimStart()
                            .Split(StringSeparatorStock.Equal, 2, StringSplitOptions.RemoveEmptyEntries)
            where tokens.Length > 1
            select new
            {
                Color = tokens[0],
                Regex = new Regex(tokens[1]),
            }
            into arg
            let all = arg.Color.EndsWith("*")
            let color = Color.Parse(all ? arg.Color[..^1] : arg.Color)
            select CreateMarker(arg.Regex, all, color);

        try
        {
            PaintLines(Console.In, defaultColor, markers.Prepend(line => new[] { new Markup(0, line.Length, defaultColor) }));
        }
        finally
        {
            defaultColor.ApplyToConsole();
        }
    }

    static IEnumerable<string> ParseResponseFile(string path)
    {
        if (path.Length > 1 && path[0] == '~'
                            && path[1] == Path.DirectorySeparatorChar
                            || path[1] == Path.AltDirectorySeparatorChar)
        {
            path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path.AsSpan(2));
        }

        var lines
            = GetSearchPaths(path).FirstOrDefault(File.Exists) is { } existingPath
            ? File.ReadLines(existingPath)
            : throw new FileNotFoundException($"Unable to find the response file \"{path}\".");

        return CommandLineParser.ParseArgumentsToList(
            string.Join(" ",
                from line in lines
                where !string.IsNullOrWhiteSpace(line)
                      && !line.StartsWith("#")
                select line));

        IEnumerable<string> GetSearchPaths(string basePath)
        {
            yield return basePath;
            if (basePath[0] != '~' || basePath.IndexOfAny(StringSeparatorStock.DirectorySeparators) >= 0)
                yield break;
            var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            const string dotname = ".recolor";
            yield return Path.Join(userProfilePath, dotname, basePath[1..] + ".rsp");
            yield return Path.Join(userProfilePath, dotname, basePath.AsSpan(1));
        }
    }

    static void ShowHelp(TextWriter output)
    {
        var help = Help.Replace("$NAME", Path.GetFileNameWithoutExtension(Environment.ProcessPath))
                       .Replace("$PRODUCT", ThisAssembly.Info.Product)
                       .Replace("$VERSION", ThisAssembly.Info.FileVersion)
                       .Replace("$COPYRIGHT", ThisAssembly.Info.Copyright);

        foreach (var line in help.Trim().SplitIntoLines())
            output.WriteLine(line);
    }

    static void PaintLines(TextReader reader, Color defaultColor, IEnumerable<Marker> markers)
    {
        markers = markers.ToArray();
        var colors = new Color[1024];

        for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
        {
            if (colors.Length < line.Length)
                colors = new Color[line.Length];

            {
                // ReSharper disable once AccessToModifiedClosure
                var markups = markers.SelectMany(m => m(line));
                var count = 0;
                foreach (var markup in markups)
                {
                    count++;
                    for (var i = markup.Run.Index; i < markup.Run.End; i++)
                    {
                        colors[i] = new Color(markup.Color.Foreground ?? colors[i].Foreground,
                                              markup.Color.Background ?? colors[i].Background);
                    }
                }

                if (count == 0)
                {
                    Console.WriteLine(line);
                    continue;
                }
            }

            {
                var anchor = 0;
                var cc = default(Color);
                for (var i = 0; i < line.Length; i++)
                {
                    var color = colors[i];
                    if (color != cc)
                    {
                        cc.ApplyToConsole();
                        Console.Write(line.Substring(anchor, i - anchor));
                        anchor = i;
                        cc = color;
                    }
                }
                if (anchor < line.Length)
                {
                    cc.ApplyToConsole();
                    Console.Write(line.Substring(anchor, line.Length - anchor));
                }
                defaultColor.ApplyToConsole();
                Console.WriteLine();
            }
        }
    }

    static class StringSeparatorStock
    {
        public static readonly char[] Slash = { '/' };
        public static readonly char[] Equal = { '=' };

        public static readonly char[] DirectorySeparators =
        {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        };
    }

    const string Help = """
        $PRODUCT $VERSION
        $COPYRIGHT

        Colors text received over standard input based on regular expression patterns.

        Usage:

            $NAME COLOR1=REGEX1 COLOR2=REGEX2 ... COLORN=REGEXN

        Each line read over standard input is tested against each regular expression
        pattern (REGEX) in turn. Those that match are used to highlight the matched
        portion of the text in the corresponding color (COLOR). The regular
        expression patterns are tested on each line only once unless a start-equal
        (*=) instead just an equal (=) is used to separate the color from the regular
        expression pattern specification.

        Any command-line argument of the form @FILE is expanded with the contents of
        the response file identified by FILE. A response file specifies command-line
        arguments, one per line. Blank lines or lines in the response file starting
        with pound/hash (#) are ignored. More than one response file can be given.
        If FILE starts with (~) and is not a path (i.e. contains no directory
        separators) then either "~/.recolor/FILE.rsp" or "~/.recolor/FILE" will be
        sought and used as the response file instead (where ~ is the user home).
        Processing of all arguments begins after expanding all response files to form
        a single command-line.

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

            0 = Black           8 = DarkGray
            1 = DarkBlue        9 = Blue
            2 = DarkGreen       A = Green
            3 = DarkCyan        B = Cyan
            4 = DarkRed         C = Red
            5 = DarkMagenta     D = Magenta
            6 = DarkYellow      E = Yellow
            7 = Gray            F = White

        The regular expression pattern language reference can be found online at:

            http://go.microsoft.com/fwlink/?LinkId=133231

        Below is a quick reference:

            Main Elements ------------------------------------------------------------

            text     Matches exact characters anywhere in the original text.

            .        Matches any single character.

            [chars]  Matches at least one of the characters in the brackets.

            [range]  Matches at least one of the characters within the range. The use
                     of a hyphen (-) allows you to specify an adjacent character.

            [^chars] Matches any characters except those in brackets.

            ^        Matches the beginning characters.

            $        Matches the end characters.

            *        Matches any instances of the preceding character.

            ?        Matches zero or one instance of the preceding character.

            \        Matches the character that follows as an escaped character.

            Quantifiers --------------------------------------------------------------

            *        Specifies zero or more matches.

            +        Matches repeating instances of the preceding characters.

            ?        Specifies zero or one matches.

            {n}      Specifies exactly n matches.

            {n,}     Specifies at least n matches.

            {n,m}    Specifies at least n, but no more than m, matches.

            Character Classes --------------------------------------------------------

            \p{name} Matches any character in the named character class specified by
                     {name}. Supported names are Unicode groups and block ranges such
                     as Ll, Nd, Z, IsGreek, and IsBoxDrawing.

            \P{name} Matches text not included in the groups and block ranges
                     specified in {name}.

            \w       Matches any word character. Equivalent to the Unicode character
                     categories [\p{Ll} \p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Pc}].

            \W       Matches any nonword character. Equivalent to the Unicode
                     categories [^\p{Ll}\p{Lu}\p{Lt} \p{Lo}\p{Nd}\p{Pc}].

            \s       Matches any white-space character. Equivalent to the Unicode
                     character categories [\f\n\r\t\v\x85\p{Z}].

            \S       Matches any non-white-space character. Equivalent to the Unicode
                     character categories [^\f\n\r\t\v\x85\p{Z}].

            \d       Matches any decimal digit. Equivalent to \p{Nd} for Unicode and
                     [0-9] for non-Unicode behavior.

            \D       Matches any nondigit. Equivalent  to \P{Nd} for Unicode and
                     [^0-9] for non-Unicode behavior.

        Licensed under the Apache License, Version 2.0 (the "License"); you may not
        use this file except in compliance with the License. You may obtain a copy of
        the License at

           http://www.apache.org/licenses/LICENSE-2.0

        Unless required by applicable law or agreed to in writing, software
        distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
        WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
        License for the specific language governing permissions and limitations under
        the License.

        Portions of this software are covered by The MIT License (MIT):

            Copyright (c) .NET Foundation and Contributors. All rights reserved.

            Permission is hereby granted, free of charge, to any person
            obtaining a copy of this software and associated documentation files
            (the "Software"), to deal in the Software without restriction,
            including without limitation the rights to use, copy, modify, merge,
            publish, distribute, sublicense, and/or sell copies of the Software,
            and to permit persons to whom the Software is furnished to do so,
            subject to the following conditions:

            The above copyright notice and this permission notice shall be
            included in all copies or substantial portions of the Software.

            THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
            EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
            MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
            NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
            BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
            LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
            ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE
            OR OTHER DEALINGS IN THE SOFTWARE.
        """;
}

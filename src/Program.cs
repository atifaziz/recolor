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

namespace Recolor
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using AngryArrays.Splice;
    using Mannex;
    using Mannex.Reflection;

    #endregion

    static class Program
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
            var type = typeof(Program);
            var assembly = type.Assembly;

            var vi = FileVersionInfo.GetVersionInfo(assembly.Location);

            var help = assembly.GetManifestResourceString(type, "Help.txt")
                               .Replace("$NAME", Path.GetFileNameWithoutExtension(vi.FileName))
                               .Replace("$PRODUCT", vi.ProductName)
                               .Replace("$VERSION", vi.FileVersion)
                               .Replace("$COPYRIGHT", vi.LegalCopyright);

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

        static int Main(string[] args)
        {
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
        }
    }
}

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
    using System.Text.RegularExpressions;
    using AngryArrays.Splice;
    using Mannex;
    using Mannex.Reflection;

    #endregion

    static class Program
    {
        [DebuggerDisplay("Foreground = {Foreground}, Background = {Background}")]
        readonly struct Color : IEquatable<Color>
        {
            public ConsoleColor? Foreground { get; }
            public ConsoleColor? Background { get; }

            public Color(ConsoleColor? foreground, ConsoleColor? background) : this()
            {
                Foreground = foreground;
                Background = background;
            }

            public void Do(Action<ConsoleColor> onForeground, Action<ConsoleColor> onBackground)
            {
                if (Background is ConsoleColor bg) onBackground(bg);
                if (Foreground is ConsoleColor fg) onForeground(fg);
            }

            public bool Equals(Color other) =>
                Foreground == other.Foreground && Background == other.Background;

            public override bool Equals(object obj) =>
                obj is Color color && Equals(color);

            public override int GetHashCode() =>
                unchecked((Foreground.GetHashCode() * 397) ^ Background.GetHashCode());

            public static bool operator ==(Color a, Color b) => a.Equals(b);
            public static bool operator !=(Color a, Color b) => !(a == b);

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

            static bool IsHexChar(char ch) => (ch >= '0' && ch <= '9')
                                           || (ch >= 'a' && ch <= 'f')
                                           || (ch >= 'A' && ch <= 'F');

            static ConsoleColor? ParseConsoleColor(string input)
            {
                if (input.Length == 0) return null;
                if (!Regex.IsMatch(input, " *[a-zA-Z]+ *", RegexOptions.CultureInvariant))
                    throw new FormatException("Color name syntax error.");
                return input.Length > 0
                     ? Enum.Parse<ConsoleColor>(input, true)
                     : (ConsoleColor?)null;
            }

            public void ApplyToConsole() => Do(fg => Console.ForegroundColor = fg,
                                               bg => Console.BackgroundColor = bg);
        }

        [DebuggerDisplay("{Index}...{End} ({Length})")]
        readonly struct Run : IEquatable<Run>
        {
            public int Index    { get; }
            public int Length   { get; }
            public int End      => Index + Length;
            public bool IsEmpty => Length == 0;

            public Run(int index, int length) : this()
            {
                Index = index;
                Length = length;
            }

            public bool Equals(Run other) => Index == other.Index && Length == other.Length;
            public override bool Equals(object obj) => obj is Run run && Equals(run);
            public override int GetHashCode() => unchecked((Index * 397) ^ Length);
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
            var debug = Array.FindIndex(args, arg => "--debug".Equals(arg, StringComparison.OrdinalIgnoreCase));
            if (debug >= 0)
            {
                Debugger.Launch();
                args = args.Splice(debug, 1);
            }

            if (args.Any(arg => "-?".Equals(arg, StringComparison.OrdinalIgnoreCase)
                             || "-h".Equals(arg, StringComparison.OrdinalIgnoreCase)
                             || "--help".Equals(arg, StringComparison.OrdinalIgnoreCase)))
            {
                ShowHelp(Console.Out);
                return;
            }

            var tail =
                from arg in args
                select !arg.StartsWith("@")
                     ? new[] { arg }.AsEnumerable()
                     : arg.Length == 1
                     ? Enumerable.Empty<string>()
                     : ParseResponseFile(arg.Substring(1)) into argz
                from arg in argz
                select arg;

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
                let color = Color.Parse(all ? arg.Color.Slice(0, -1) : arg.Color)
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

        static IEnumerable<string> ParseResponseFile(string path) =>
            CommandLineParser.ParseArgumentsToList(
                string.Join(" ",
                    from line in File.ReadAllLines(path)
                    where !string.IsNullOrWhiteSpace(line)
                       && !line.StartsWith("#")
                    select line));

        static void ShowHelp(TextWriter output)
        {
            var type = typeof(Program);
            var assembly = type.Assembly;

            var vi = FileVersionInfo.GetVersionInfo(new Uri(assembly.CodeBase).LocalPath);

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

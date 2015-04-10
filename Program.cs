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
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Mannex;
    using Mannex.Collections.Generic;
    using MoreLinq;

    #endregion

    static class Program
    {
        [DebuggerDisplay("Foreground = {Foreground}, Background = {Background}")]
        struct Color : IEquatable<Color>
        {
            public ConsoleColor? Foreground { get; private set; }
            public ConsoleColor? Background { get; private set; }

            public Color(ConsoleColor? foreground, ConsoleColor? background) : this()
            {
                Foreground = foreground;
                Background = background;
            }

            public void Do(Action<ConsoleColor> onForeground, Action<ConsoleColor> onBackground)
            {
                if (Background != null) onBackground(Background.Value);
                if (Foreground != null) onForeground(Foreground.Value);
            }

            public bool Equals(Color other)
            {
                return Foreground == other.Foreground && Background == other.Background;
            }

            public override bool Equals(object obj)
            {
                return obj is Color && Equals((Color) obj);
            }

            public override int GetHashCode()
            {
                return unchecked((Foreground.GetHashCode() * 397) ^ Background.GetHashCode());
            }

            public static Color Parse(string input)
            {
                var tokens = input.Split(new[] { ',' }, 2, StringSplitOptions.RemoveEmptyEntries);
                return new Color(ParseConsoleColor(tokens[0]), tokens.Length > 1 ? ParseConsoleColor(tokens[1]) : null);
            }

            static ConsoleColor? ParseConsoleColor(string input)
            {
                return input.Length > 0
                     ? (ConsoleColor)Enum.Parse(typeof(ConsoleColor), input, true)
                     : (ConsoleColor?)null;
            }
        }

        [DebuggerDisplay("{Index}...{End} ({Length})")]
        struct Run : IEquatable<Run>
        {
            public int Index { get; private set; }
            public int Length { get; private set; }
            public int End { get { return Index + Length; } }

            public Run(int index, int length) : this()
            {
                Index = index;
                Length = length;
            }

            public bool Equals(Run other) { return Index == other.Index && Length == other.Length; }
            public override bool Equals(object obj) { return obj is Run && Equals((Run) obj); }
            public override int GetHashCode() { return unchecked((Index * 397) ^ Length); }
            public bool OverlapsWith(Run other) { return other.Index >= Index && other.Index < End; }
        }

        [DebuggerDisplay("Run = {Run}, Color = {Color}, Priority = {Priority}")]
        sealed class Markup
        {
            public Run   Run      { get; private set; }
            public Color Color    { get; private set; }
            public int   Priority { get; private set; }

            public Markup(int index, int length, Color color, int priority) :
                this(new Run(index, length), color, priority) { }

            public Markup(Run run, Color color, int priority)
            {
                Run      = run;
                Color    = color;
                Priority = priority;
            }

            public T Split<T>(Run run, Func<Markup, Markup, T> selector)
            {
                return selector(new Markup(Run.Index, run.Index - Run.Index, Color, Priority),
                                new Markup(run.End, Run.End - run.End, Color, Priority));
            }
        }

        delegate IEnumerable<Markup> Marker(string line);

        sealed class MarkupSplicer : ISplicer<Markup>
        {
            public static readonly MarkupSplicer Stock = new MarkupSplicer();

            public TResult Splice<TResult>(Markup input, Run run, Func<Markup, Markup, TResult> selector)
            {
                return input.Split(run, selector);
            }
        }

        interface ISplicer<T>
        {
            TResult Splice<TResult>(T input, Run run, Func<T, T, TResult> selector);
        }

        sealed class Comparer<T> : IComparer<T>
        {
            readonly Comparison<T> _comparison;
            public Comparer(Comparison<T> comparison) { _comparison = comparison; }
            public int Compare(T x, T y) { return _comparison(x, y); }
        }

        static IEnumerable<Markup> Reflow(IEnumerable<Markup> runs, ISplicer<Markup> splicer)
        {
            var q =
                from e in runs
                orderby e.Run.Index, e.Priority
                select e;
            var ordered = q.ToList();
            var comparer = new Comparer<Markup>((a, b) => Comparables.Compare(a.Run.Index, a.Priority, b.Run.Index, b.Priority));

            while (ordered.Count > 1)
            {
                var fst = ordered.PopAt(0);
                var snd = ordered[0];
                if (fst.Run.OverlapsWith(snd.Run))
                {
                    var splits = splicer.Splice(fst, snd.Run, (l, r) => new { Left = l, Right = r });
                    yield return splits.Left;
                    var index = ~ordered.BinarySearch(splits.Right, comparer);
                    ordered.Insert(index, splits.Right);
                }
                else
                {
                    yield return fst;
                }
            }

            yield return ordered[0];
        }

        static Marker CreateMarker(Regex regex, bool all, Color color, int priority)
        {
            return line => from run in Matches(line, regex, all)
                           select new Markup(run, color, priority);
        }

        static IEnumerable<Run> Matches(string line, Regex regex, bool all)
        {
            for (var m = regex.Match(line); m.Success; m = m.NextMatch())
            {
                yield return new Run(m.Index, m.Length);
                if (!all) yield break;
            }
        }

        static void Wain(IEnumerable<string> args)
        {
            var defaultColor = new Color(Console.ForegroundColor, Console.BackgroundColor);
            var markers =
                from arg in args.Reverse().Select((spec, i) => new { Spec = spec, Priority = i })
                let tokens = arg.Spec.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries)
                where tokens.Length > 1
                select new
                {
                    arg.Priority,
                    Color = tokens[0],
                    Regex = new Regex(tokens[1]),
                }
                into arg
                let all = arg.Color.EndsWith("*")
                let color = Color.Parse(all ? arg.Color.Slice(0, -1) : arg.Color)
                select CreateMarker(arg.Regex, all, color, arg.Priority);

            try
            {
                PaintLines(Console.In, markers.Prepend(line => new[] { new Markup(0, line.Length, defaultColor, -1) }));
            }
            finally
            {
                defaultColor.Do(fg => Console.ForegroundColor = fg, bg => Console.BackgroundColor = bg);
            }
        }

        static void PaintLines(TextReader reader, IEnumerable<Marker> markers)
        {
            markers = markers.ToArray();
            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                // ReSharper disable once AccessToModifiedClosure
                var runs = markers.SelectMany(m => m(line));
                var markups = Reflow(runs, MarkupSplicer.Stock);
                foreach (var markup in markups)
                {
                    markup.Color.Do(fg => Console.ForegroundColor = fg, bg => Console.BackgroundColor = bg);
                    Console.Write(line.Substring(markup.Run.Index, markup.Run.Length));
                }
                Console.WriteLine();
            }
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

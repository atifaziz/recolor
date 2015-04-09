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
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;

    #endregion

    static class Program
    {
        static void Wain(string[] args)
        {
            var foregroundColor = Console.ForegroundColor;
            var painter = LinePainterFactory.CreateLineColoringPainter(foregroundColor);
            Array.Reverse(args);
            foreach (var arg in args)
            {
                var tokens = arg.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                var lineColoringPainter = LinePainterFactory.CreateLineColoringPainter((ConsoleColor)Enum.Parse(typeof(ConsoleColor), tokens[0], true));
                var re = new Regex(tokens[1], RegexOptions.IgnoreCase);
                painter = LinePainterFactory.CreateConditionalPainter(re, lineColoringPainter, painter);
            }
            try
            {
                PaintLines(Console.In, painter);
            }
            finally
            {
                Console.ForegroundColor = foregroundColor;
            }
        }

        static void PaintLines(TextReader reader, LinePainter painter)
        {
            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                painter(line, WriteWithColor);
                Console.WriteLine();
            }
        }

        static void WriteWithColor(ConsoleColor color, string text)
        {
            Console.ForegroundColor = color;
            Console.Write(text);
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

        delegate void TextPainter(ConsoleColor color, string text);
        delegate void LinePainter(string line, TextPainter painter);

        static class LinePainterFactory
        {
            public static LinePainter CreateLineColoringPainter(ConsoleColor color)
            {
                return (line, painter) => painter(color, line);
            }

            public static LinePainter CreateConditionalPainter(Regex condition, LinePainter truePainter, LinePainter falsePainter)
            {
                return (line, painter) =>
                {
                    if (condition.IsMatch(line))
                        truePainter(line, painter);
                    else
                        falsePainter(line, painter);
                };
            }
        }
    }
}

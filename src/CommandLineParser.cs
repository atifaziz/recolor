#region Copyright (c) .NET Foundation and Contributors
//
// The MIT License (MIT)
//
// All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
#endregion

namespace Recolor;

using System.Collections.Generic;
using System.Text;

static class CommandLineParser
{
    public static List<string> ParseArgumentsToList(string arguments)
    {
        var list = new List<string>();
        ParseArgumentsIntoList(arguments, list);
        return list;
    }

    // https://github.com/dotnet/corefx/blob/96925bba1377b6042fc7322564b600a4e651f7fd/src/System.Diagnostics.Process/src/System/Diagnostics/Process.Unix.cs#L527-L626

    /// <summary>Parses a command-line argument string into a list of arguments.</summary>
    /// <param name="arguments">The argument string.</param>
    /// <param name="results">The list into which the component arguments should be stored.</param>
    /// <remarks>
    /// This follows the rules outlined in "Parsing C++ Command-Line Arguments" at
    /// https://msdn.microsoft.com/en-us/library/17w5ykft.aspx.
    /// </remarks>
    private static void ParseArgumentsIntoList(string arguments, List<string> results)
    {
        // Iterate through all of the characters in the argument string.
        for (int i = 0; i < arguments.Length; i++)
        {
            while (i < arguments.Length && (arguments[i] == ' ' || arguments[i] == '\t'))
                i++;

            if (i == arguments.Length)
                break;

            results.Add(GetNextArgument(ref i));
        }

        string GetNextArgument(ref int i)
        {
            var currentArgument = StringBuilderCache.Acquire();
            bool inQuotes = false;

            while (i < arguments.Length)
            {
                // From the current position, iterate through contiguous backslashes.
                int backslashCount = 0;
                while (i < arguments.Length && arguments[i] == '\\')
                {
                    i++;
                    backslashCount++;
                }

                if (backslashCount > 0)
                {
                    if (i >= arguments.Length || arguments[i] != '"')
                    {
                        // Backslashes not followed by a double quote:
                        // they should all be treated as literal backslashes.
                        currentArgument.Append('\\', backslashCount);
                    }
                    else
                    {
                        // Backslashes followed by a double quote:
                        // - Output a literal slash for each complete pair of slashes
                        // - If one remains, use it to make the subsequent quote a literal.
                        currentArgument.Append('\\', backslashCount / 2);
                        if (backslashCount % 2 != 0)
                        {
                            currentArgument.Append('"');
                            i++;
                        }
                    }

                    continue;
                }

                char c = arguments[i];

                // If this is a double quote, track whether we're inside of quotes or not.
                // Anything within quotes will be treated as a single argument, even if
                // it contains spaces.
                if (c == '"')
                {
                    if (inQuotes && i < arguments.Length - 1 && arguments[i + 1] == '"')
                    {
                        // Two consecutive double quotes inside an inQuotes region should result in a literal double quote
                        // (the parser is left in the inQuotes region).
                        // This behavior is not part of the spec of code:ParseArgumentsIntoList, but is compatible with CRT
                        // and .NET Framework.
                        currentArgument.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    i++;
                    continue;
                }

                // If this is a space/tab and we're not in quotes, we're done with the current
                // argument, it should be added to the results and then reset for the next one.
                if ((c == ' ' || c == '\t') && !inQuotes)
                {
                    break;
                }

                // Nothing special; add the character to the current argument.
                currentArgument.Append(c);
                i++;
            }

            return StringBuilderCache.GetStringAndRelease(currentArgument);
        }
    }
}

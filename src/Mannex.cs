#region License, Terms and Author(s)
//
// Mannex - Extension methods for .NET
// Copyright (c) 2009 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Atif Aziz, http://www.raboof.com
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

namespace Mannex
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using IO;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="string"/>.
    /// </summary>

    static partial class StringExtensions
    {
        /// <summary>
        /// Splits string into lines where a line is terminated
        /// by CR and LF, or just CR or just LF.
        /// </summary>
        /// <remarks>
        /// This method uses deferred exection.
        /// </remarks>

        public static IEnumerable<string> SplitIntoLines(this string str)
        {
            if (str == null) throw new ArgumentNullException("str");
            return SplitIntoLinesImpl(str);
        }

        private static IEnumerable<string> SplitIntoLinesImpl(string str)
        {
            using (var reader = str.Read())
            using (var line = reader.ReadLines())
                while (line.MoveNext())
                    yield return line.Current;
        }
    }
}

namespace Mannex.IO
{
    #region Imports

    using System;
    using System.IO;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="string"/>.
    /// </summary>

    static partial class StringExtensions
    {
        /// <summary>
        /// Returns a <see cref="TextReader"/> for reading string.
        /// </summary>

        public static TextReader Read(this string str)
        {
            if (str == null) throw new ArgumentNullException("str");
            return new StringReader(str);
        }
    }
}

namespace Mannex.Collections.Generic
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="List{T}"/>.
    /// </summary>

    static partial class ListExtensions
    {
        /// <summary>
        /// Removes and returns the item at a given index of the list.
        /// </summary>

        [DebuggerStepThrough]
        public static T PopAt<T>(this IList<T> list, int index)
        {
            if (list == null) throw new ArgumentNullException("list");
            var item = list[index];
            list.RemoveAt(index);
            return item;
        }
    }
}

namespace Mannex.IO
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.IO;

    #endregion

    /// <summary>
    /// Extension methods for <see cref="TextReader"/>.
    /// </summary>

    static partial class TextReaderExtensions
    {
        /// <summary>
        /// Reads all lines from reader using deferred semantics.
        /// </summary>

        public static IEnumerator<string> ReadLines(this TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            return ReadLinesImpl(reader);
        }

        static IEnumerator<string> ReadLinesImpl(this TextReader reader)
        {
            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                yield return line;
        }
    }
}

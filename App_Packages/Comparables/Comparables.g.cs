#region Copyright (c) 2015 Atif Aziz. All rights reserved.
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

// This code was generated (Wed, 18 Mar 2015 09:25:36 GMT) by a tool.
// Any changes made manually will be lost next time this code is regenerated.

using System;

partial class Comparables
{
    /// <summary>
    /// Compares a pair of tuples of two.
    /// </summary>
    /// <returns>
    /// A signed integer that is zero (0) if the tuples are equal,
    /// less than zero if the first tuple compares lower in order and greater
    /// than zero if second tuple compares higher in order.
    /// </returns>

    public static int Compare<T1, T2>(
        /* tuple 1 */ T1 a1, T2 a2,
        /* tuple 2 */ T1 b1, T2 b2)
        where T1 : IComparable<T1>
        where T2 : IComparable<T2>
    {
        int cmp1;
        return (cmp1 = a1.CompareTo(b1)) != 0 ? cmp1
             : a2.CompareTo(b2);
    }

    /// <summary>
    /// Compares a pair of tuples of three.
    /// </summary>
    /// <returns>
    /// A signed integer that is zero (0) if the tuples are equal,
    /// less than zero if the first tuple compares lower in order and greater
    /// than zero if second tuple compares higher in order.
    /// </returns>

    public static int Compare<T1, T2, T3>(
        /* tuple 1 */ T1 a1, T2 a2, T3 a3,
        /* tuple 2 */ T1 b1, T2 b2, T3 b3)
        where T1 : IComparable<T1>
        where T2 : IComparable<T2>
        where T3 : IComparable<T3>
    {
        int cmp1, cmp2;
        return (cmp1 = a1.CompareTo(b1)) != 0 ? cmp1
             : (cmp2 = a2.CompareTo(b2)) != 0 ? cmp2
             : a3.CompareTo(b3);
    }

    /// <summary>
    /// Compares a pair of tuples of four.
    /// </summary>
    /// <returns>
    /// A signed integer that is zero (0) if the tuples are equal,
    /// less than zero if the first tuple compares lower in order and greater
    /// than zero if second tuple compares higher in order.
    /// </returns>

    public static int Compare<T1, T2, T3, T4>(
        /* tuple 1 */ T1 a1, T2 a2, T3 a3, T4 a4,
        /* tuple 2 */ T1 b1, T2 b2, T3 b3, T4 b4)
        where T1 : IComparable<T1>
        where T2 : IComparable<T2>
        where T3 : IComparable<T3>
        where T4 : IComparable<T4>
    {
        int cmp1, cmp2, cmp3;
        return (cmp1 = a1.CompareTo(b1)) != 0 ? cmp1
             : (cmp2 = a2.CompareTo(b2)) != 0 ? cmp2
             : (cmp3 = a3.CompareTo(b3)) != 0 ? cmp3
             : a4.CompareTo(b4);
    }

    /// <summary>
    /// Compares a pair of tuples of five.
    /// </summary>
    /// <returns>
    /// A signed integer that is zero (0) if the tuples are equal,
    /// less than zero if the first tuple compares lower in order and greater
    /// than zero if second tuple compares higher in order.
    /// </returns>

    public static int Compare<T1, T2, T3, T4, T5>(
        /* tuple 1 */ T1 a1, T2 a2, T3 a3, T4 a4, T5 a5,
        /* tuple 2 */ T1 b1, T2 b2, T3 b3, T4 b4, T5 b5)
        where T1 : IComparable<T1>
        where T2 : IComparable<T2>
        where T3 : IComparable<T3>
        where T4 : IComparable<T4>
        where T5 : IComparable<T5>
    {
        int cmp1, cmp2, cmp3, cmp4;
        return (cmp1 = a1.CompareTo(b1)) != 0 ? cmp1
             : (cmp2 = a2.CompareTo(b2)) != 0 ? cmp2
             : (cmp3 = a3.CompareTo(b3)) != 0 ? cmp3
             : (cmp4 = a4.CompareTo(b4)) != 0 ? cmp4
             : a5.CompareTo(b5);
    }
}

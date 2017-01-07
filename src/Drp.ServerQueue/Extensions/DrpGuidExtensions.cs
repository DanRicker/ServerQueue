/*
    Copyright 2016 Daniel Ricker III and Peoplutions
*/

namespace Drp
{

    #region Using Statements

    using System;

    #endregion

    /// <summary>
    ///  Extension methods for Guid data type
    /// </summary>
    public static class DrpGuidExtensions
    {
        /// <summary>
        /// Returns Guid string with no formatting
        ///  N : 00000000000000000000000000000000
        /// </summary>
        /// <param name="guid">to format</param>
        /// <returns>formatted guid string</returns>
        public static string ToStringNoFormatting(this Guid guid)
        {
            return guid.ToString("N");
        }

        /// <summary>
        /// Returns Guid string formatted with dash separators
        ///  D : 00000000-0000-0000-0000-000000000000
        /// </summary>
        /// <param name="guid">to format</param>
        /// <returns>formatted guid string</returns>
        public static string ToStringDashes(this Guid guid)
        {
            return guid.ToString("D");
        }

        /// <summary>
        /// Returns Guid string formatted with dash separators and outer braces
        ///  B : {00000000-0000-0000-0000-000000000000}
        /// </summary>
        /// <param name="guid">to format</param>
        /// <returns>formatted guid string</returns>
        public static string ToStringOuterBraces(this Guid guid)
        {
            return guid.ToString("B");
        }

        /// <summary>
        /// Returns Guid string formatted with dash separators and outer parenthesis
        ///  P : (00000000-0000-0000-0000-000000000000)
        /// </summary>
        /// <param name="guid">to format</param>
        /// <returns>formatted guid string</returns>
        public static string ToStringOuterParenthesis(this Guid guid)
        {
            return guid.ToString("P");
        }

        /// <summary>
        /// Returns Guid string formatted with dash separators and, inner and outer braces
        ///  X : {0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}
        /// </summary>
        /// <param name="guid">to format</param>
        /// <returns>formatted guid string</returns>
        public static string ToStringOuterInnerBraces(this Guid guid)
        {
            return guid.ToString("X");
        }

    }
}

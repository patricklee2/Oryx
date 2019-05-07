﻿// --------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Oryx.Common.Extensions
{
    public static class StringExtensions
    {
        private static readonly string UrlPattern = @"(https?|ftp|git|git+ssh|git+http|git+https|git+file):\/\/" +
                                                    @"(?<user>[^\s/$.?#@]+):(?<pass>[^\s/$.?#@]+)@[^\s/$.?#].[^\s]*";

        /// <summary>
        /// Determines whether a string contains a URL substring in it, and returns its position.
        /// </summary>
        /// <param name="str">string to inspect</param>
        /// <param name="replacement">passwords will be replaced with this string</param>
        /// <returns>str with passwords in URL replaced by replacement</returns>
        public static string ReplaceUrlPasswords(this string str, string replacement = "***")
        {
            try
            {
                StringBuilder result = new StringBuilder();
                ICollection<Match> matches = Regex.Matches(str, UrlPattern, RegexOptions.IgnoreCase);

                int positionInStr = 0;
                foreach (Match m in matches)
                {
                    var passwordGroup = m.Groups["pass"];
                    result.Append(str.Substring(positionInStr, passwordGroup.Index - positionInStr));
                    result.Append(replacement);
                    positionInStr = passwordGroup.Index + passwordGroup.Length; // Skip past password
                }

                result.Append(str.Substring(positionInStr));
                return result.ToString();
            }
            catch
            {
                return str;
            }
        }

        /// <summary>
        /// Splits a string to chunks of the given maximum length.
        /// </summary>
        /// <param name="str">string to split</param>
        /// <param name="maxLength">maximum length of each chunk</param>
        /// <returns>list of chunks</returns>
        public static IList<string> Chunkify(this string str, int maxLength)
        {
            var result = new List<string>();
            for (int i = 0; i < str.Length; i += maxLength)
            {
                result.Add(str.Substring(i, Math.Min(maxLength, str.Length - i)));
            }

            return result;
        }
    }
}

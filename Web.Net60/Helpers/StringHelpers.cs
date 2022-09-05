// Copyright 2022 Murray Grant
//
//    Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MurrayGrant.MakeMeAPassword.Web.Net60.Helpers
{
    public static class StringHelpers
    {
        public static readonly string WindowsNewLine = "\r\n";

        public static bool IsTruthy(this string s, bool? valueIfEmpty = null)
        {
            if (String.IsNullOrEmpty(s) && !valueIfEmpty.HasValue)
                return false;
            if (String.IsNullOrEmpty(s) && valueIfEmpty.HasValue)
                return valueIfEmpty.Value;

            if (s.StartsWith("Y", StringComparison.OrdinalIgnoreCase))
                return true;
            if (s.StartsWith("T", StringComparison.OrdinalIgnoreCase))
                return true;
            if (Char.IsDigit(s, 0) && !s.StartsWith("0", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private static readonly string _HexCharacters = "0123456789abcdefABCDEF";
        public static byte[] ToByteArray(this string s)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (String.IsNullOrEmpty(s))
                return new byte[0];
            if (s.Length % 2 != 0)
                throw new ArgumentOutOfRangeException("s", "Must be even number of hex digits.");
            if (s.Any(c => !_HexCharacters.Contains(c)))
                throw new ArgumentOutOfRangeException("s", String.Format("Invalid character in string '{0}'", s));

            var result = new byte[s.Length/2];
            for (int i = 0; i < result.Length-1; i++)
                result[i] = Byte.Parse(s[i*2].ToString() + s[(i*2)+1].ToString(), System.Globalization.NumberStyles.HexNumber);
            return result;
        }

        public static Guid? ParseAsGuidOrNull(this string s)
        {
            if (String.IsNullOrEmpty(s))
                return null;

            if (Guid.TryParse(s, out var result))
                return result;
            else
                return null;
        }
    }
}
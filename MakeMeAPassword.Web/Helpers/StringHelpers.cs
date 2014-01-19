// Copyright 2014 Murray Grant
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

namespace MurrayGrant.PasswordGenerator.Web.Helpers
{
    public static class StringHelpers
    {
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
    }
}
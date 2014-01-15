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
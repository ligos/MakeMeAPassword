using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MurrayGrant.PasswordGenerator.Web.Helpers
{
    public static class ByteArrayHelpers
    {
        public static bool SlowEquals(this byte[] lhs, byte[] rhs)
        {
            var result = true;
            result &= lhs.Length == rhs.Length;

            for (int i = 0; i < lhs.Length; i++)
                result &= lhs[i] == rhs[i];

            return result;
        }

        public static string ToHexString(this byte[] bytes)
        {
            return String.Join("", bytes.Select(b => b.ToString("x2")));
        }
    }
}
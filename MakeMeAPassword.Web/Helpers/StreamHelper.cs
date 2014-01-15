using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace MurrayGrant.PasswordGenerator.Web.Helpers
{
    public static class StreamHelper
    {
        public static void WriteBytes(this Stream s, byte[] buf)
        {
            s.Write(buf, 0, buf.Length);
        }
    }
}
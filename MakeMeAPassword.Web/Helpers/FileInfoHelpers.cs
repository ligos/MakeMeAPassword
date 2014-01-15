using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace MurrayGrant.PasswordGenerator.Web.Helpers
{
    public static class FileInfoHelpers
    {
        public static IEnumerable<string> YieldLines(this FileInfo fi)
        {
            return YieldLines(fi, Encoding.UTF8);
        }
        public static IEnumerable<string> YieldLines(this FileInfo fi, Encoding enc)
        {
            using (var stream = fi.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream, enc))
            {
                while (!reader.EndOfStream)
                    yield return reader.ReadLine();
            }
        }
    }
}
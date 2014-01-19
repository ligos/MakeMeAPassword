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
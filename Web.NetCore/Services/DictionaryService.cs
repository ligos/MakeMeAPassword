// Copyright 2019 Murray Grant
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.WebUtilities;

namespace MurrayGrant.MakeMeAPassword.Web.NetCore.Services
{
    /// <summary>
    /// Lazy loads various dictionaries from disk. Cached in memory after first call.
    /// </summary>
    public class DictionaryService
    {
        private IEnumerable<string> _PinBlacklist;
        private IList<string> _PassphraseDictionary;
        private ReadablePassphrase.Dictionaries.WordDictionary _ReadablePassphraseDictionary;

        public async Task<IEnumerable<string>> ReadPinBlacklist()
        {
            if (_PinBlacklist == null)
            {
                // Black list taken from values published here: http://www.datagenetics.com/blog/september32012/
                var blacklist = new HashSet<string>(StringComparer.Ordinal);
                await ReadDictionaryAsync(blacklist, "wwwroot/data/PinBlacklist.txt", l => l.Trim());
                _PinBlacklist = blacklist;
            }
            return _PinBlacklist;
        }

        public async Task<IList<string>> ReadPassphraseDictionary()
        {
            if (_PassphraseDictionary == null)
            {
                // Dictionary derived from the most common published English words after 1950 from Google ngrams: http://storage.googleapis.com/books/ngrams/books/datasetsv2.html
                var words = new List<string>();
                await ReadDictionaryAsync(words, "wwwroot/data/DictionaryEnglish.txt", l => l.Trim());
                _PassphraseDictionary = words;
            }
            return _PassphraseDictionary;
        }

        public ReadablePassphrase.Dictionaries.WordDictionary ReadReadablePassphraseDictionary()
        {
            if (_ReadablePassphraseDictionary == null)
            {
                // There's IO behind this, but no async.
                _ReadablePassphraseDictionary = ReadablePassphrase.Dictionaries.Default.Load();
            }
            return _ReadablePassphraseDictionary;
        }

        private async Task ReadDictionaryAsync(ICollection<string> result, string filename, Func<string, string> linePostProcessing)
        {
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    result.Add(linePostProcessing(line));
                }
            }
        }
    }
}
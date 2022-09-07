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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MurrayGrant.MakeMeAPassword.Web.Net60.Services
{
    /// <summary>
    /// Lazy loads various dictionaries from disk. Cached in memory after first call.
    /// </summary>
    public record DictionaryService
    {
        public IEnumerable<string> PinBlacklist { get; init; } = Enumerable.Empty<string>();
        public IList<string> PassphraseDictionary { get; init; } = Array.Empty<string>();
        public ReadablePassphrase.Dictionaries.WordDictionary ReadablePassphraseDictionaryForAllWords { get; init; } = new ReadablePassphrase.Dictionaries.EmptyDictionary();
        public ReadablePassphrase.Dictionaries.WordDictionary ReadablePassphraseDictionaryForNonFakeWords { get; init; } = new ReadablePassphrase.Dictionaries.EmptyDictionary();

        public static async Task<DictionaryService> Load()
            => new DictionaryService()
            {
                PinBlacklist = await ReadPinBlacklist(),
                PassphraseDictionary = await ReadPassphraseDictionary(),
                ReadablePassphraseDictionaryForAllWords = ReadReadablePassphraseDictionary(Array.Empty<string>()),
                ReadablePassphraseDictionaryForNonFakeWords = ReadReadablePassphraseDictionary(new[] { ReadablePassphrase.Words.Tags.Fake }),
            };

        private static async Task<IEnumerable<string>> ReadPinBlacklist()
        {
            // Black list taken from values published here: http://www.datagenetics.com/blog/september32012/
            var blacklist = new HashSet<string>(StringComparer.Ordinal);
            await LoadDictionaryAsync(blacklist, "wwwroot/data/PinBlacklist.txt", l => l.Trim());
            return blacklist;
        }

        private static async Task<IList<string>> ReadPassphraseDictionary()
        {
            // Dictionary derived from the most common published English words after 1950 from Google ngrams: http://storage.googleapis.com/books/ngrams/books/datasetsv2.html
            var words = new List<string>();
            await LoadDictionaryAsync(words, "wwwroot/data/DictionaryEnglish.txt", l => l.Trim());
            return words;
        }

        private static ReadablePassphrase.Dictionaries.WordDictionary ReadReadablePassphraseDictionary(IReadOnlyList<string> excludeTags)
            // There's IO behind this, but no async.
            => ReadablePassphrase.Dictionaries.Default.Load(excludeTags: excludeTags);

        private static async Task LoadDictionaryAsync(ICollection<string> result, string filename, Func<string, string> linePostProcessing)
        {
            using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 16*1024, true);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (!String.IsNullOrEmpty(line))
                    result.Add(linePostProcessing(line));
            }
        }
    }
}
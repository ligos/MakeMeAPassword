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
using System.Web.Mvc;
using MurrayGrant.PasswordGenerator.Web.ActionResults;
using MurrayGrant.PasswordGenerator.Web.ViewModels.ApiV1;
using MurrayGrant.PasswordGenerator.Web.Services;
using MurrayGrant.PasswordGenerator.Web.Helpers;
using System.Text;
using MurrayGrant.PasswordGenerator.Web.Filters;
using MurrayGrant.ReadablePassphrase.Mutators;
using System.Threading.Tasks;
using MurrayGrant.Terninger;

namespace MurrayGrant.PasswordGenerator.Web.Controllers.Api.v1
{
    [OutputCache(NoStore = true, Duration = 0)]
    [IpThrottlingFilter]
    public class ApiPassphraseV1Controller : Controller
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public readonly static int MaxWords = 16;
        public readonly static int MaxCount = 50;
        public readonly static int DefaultMinChars = 1;
        public readonly static int DefaultMaxChars = Int32.MaxValue;
        public readonly static int DefaultWords = 4;
        public readonly static int DefaultCount = 1;
        public readonly static int MaxAttemptsPerCount = 100;
        public readonly static NumericStyles DefaultWhenNumeric = NumericStyles.Never;
        public readonly static int DefaultNumbers = 2;
        public readonly static AllUppercaseStyles DefaultWhenUppercase = AllUppercaseStyles.Never;
        public readonly static int DefaultUppercase = 2;


        public readonly static Lazy<List<string>> Dictionary;

        static ApiPassphraseV1Controller()
        {
            Dictionary = new Lazy<List<string>>(() =>
                {
                    // Dictionary derived from the most common published English words after 1950 from Google ngrams: http://storage.googleapis.com/books/ngrams/books/datasetsv2.html
                    var fi = new System.IO.FileInfo(System.Web.HttpContext.Current.Request.MapPath("~/content/data/DictionaryEnglish.txt"));
                    return new List<string>(fi.YieldLines().Select(x => x.Trim()));
                }, true);
        }


        // GET: /api/v1/passphrase/plain
        public async Task<String> Plain(int? pc, int? wc, string sp, int? minCh, int? maxCh, NumericStyles? whenNum, int? nums, AllUppercaseStyles? whenUp, int? ups)
        {
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var phrases = SelectPhrases(random,
                                        wc.HasValue ? wc.Value : DefaultWords,
                                        pc.HasValue ? pc.Value : DefaultCount,
                                        sp.IsTruthy(true),
                                        minCh.HasValue ? minCh.Value : DefaultMinChars,
                                        maxCh.HasValue ? maxCh.Value : DefaultMaxChars,
                                        whenNum.HasValue ? whenNum.Value : DefaultWhenNumeric,
                                        nums.HasValue ? nums.Value : DefaultNumbers,
                                        whenUp.HasValue ? whenUp.Value : DefaultWhenUppercase,
                                        ups.HasValue ? ups.Value : DefaultUppercase
                                    );
                return String.Join(Environment.NewLine, phrases);
            }
        }

        // GET: /api/v1/passphrase/json
        public async Task<ActionResult> Json(int? pc, int? wc, string sp, int? minCh, int? maxCh, NumericStyles? whenNum, int? nums, AllUppercaseStyles? whenUp, int? ups)
        {
            // Return as Json array.
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var phrases = SelectPhrases(random,
                                        wc.HasValue ? wc.Value : DefaultWords,
                                        pc.HasValue ? pc.Value : DefaultCount,
                                        sp.IsTruthy(true),
                                        minCh.HasValue ? minCh.Value : DefaultMinChars,
                                        maxCh.HasValue ? maxCh.Value : DefaultMaxChars,
                                        whenNum.HasValue ? whenNum.Value : DefaultWhenNumeric,
                                        nums.HasValue ? nums.Value : DefaultNumbers,
                                        whenUp.HasValue ? whenUp.Value : DefaultWhenUppercase,
                                        ups.HasValue ? ups.Value : DefaultUppercase
                                    );
                return new JsonNetResult(new JsonPasswordContainer() { pws = phrases.ToList() });
            }
        }

        // GET: /api/v1/passphrase/xml
        public async Task<ActionResult> Xml(int? pc, int? wc, string sp, int? minCh, int? maxCh, NumericStyles? whenNum, int? nums, AllUppercaseStyles? whenUp, int? ups)
        {
            // Return as XML.
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var phrases = SelectPhrases(random,
                                        wc.HasValue ? wc.Value : DefaultWords,
                                        pc.HasValue ? pc.Value : DefaultCount,
                                        sp.IsTruthy(true),
                                        minCh.HasValue ? minCh.Value : DefaultMinChars,
                                        maxCh.HasValue ? maxCh.Value : DefaultMaxChars,
                                        whenNum.HasValue ? whenNum.Value : DefaultWhenNumeric,
                                        nums.HasValue ? nums.Value : DefaultNumbers,
                                        whenUp.HasValue ? whenUp.Value : DefaultWhenUppercase,
                                        ups.HasValue ? ups.Value : DefaultUppercase
                                    );
                return new XmlResult(phrases.ToList());
            }
        }

        // GET: /api/v1/passphrase/combinations
#if !DEBUG
        [OutputCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public ActionResult Combinations(int? wc)
        {
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), 1);

            // Return information about the number of combinations as a JSON object.
            var result = new JsonCombinationContainer();
            var wordCount = Math.Min(wc.HasValue ? wc.Value : DefaultWords, MaxWords);
            var dict = Dictionary.Value;

            result.combinations = Math.Pow(dict.Count, wordCount);
            return new JsonNetResult(result);
        }

        private IEnumerable<string> SelectPhrases(IRandomNumberGenerator random, int wordCount, int phraseCount, bool spaces, int minChars, int maxChars, NumericStyles whenNumeric, int numbersToAdd, AllUppercaseStyles whenUpper, int uppersToAdd)
        {
            if (minChars > maxChars)
                yield break;

            phraseCount = Math.Min(phraseCount, MaxCount);
            wordCount = Math.Min(wordCount, MaxWords);
            if (phraseCount <= 0 || wordCount <= 0)
                yield break;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sb = new StringBuilder();
            var dict = Dictionary.Value;
            int attempts = 0;

            ICollection<IMutator> mutators = null;
            if (whenNumeric != NumericStyles.Never || whenUpper != AllUppercaseStyles.Never)
                mutators = new List<IMutator>();
            if (whenNumeric != NumericStyles.Never)
                mutators.Add(new NumericMutator() { When = whenNumeric, NumberOfNumbersToAdd = numbersToAdd });
            if (whenUpper == AllUppercaseStyles.Anywhere)
                mutators.Add(new UppercaseMutator() { When = UppercaseStyles.Anywhere, NumberOfCharactersToCapitalise = uppersToAdd });
            else if (whenUpper == AllUppercaseStyles.StartOfWord)
                mutators.Add(new UppercaseMutator() { When = UppercaseStyles.StartOfWord, NumberOfCharactersToCapitalise = uppersToAdd });
            else if (whenUpper == AllUppercaseStyles.WholeWord)
                mutators.Add(new UppercaseWordMutator() { NumberOfWordsToCapitalise = uppersToAdd });
            else if (whenUpper == AllUppercaseStyles.RunOfLetters)
                mutators.Add(new UppercaseRunMutator() { NumberOfRuns = uppersToAdd });
            MurrayGrant.ReadablePassphrase.Random.RandomSourceBase randomWrapper = null;
            if (mutators != null)
                randomWrapper = new MurrayGrant.ReadablePassphrase.Random.ExternalRandomSource(random.GetRandomBytes);

            for (int c = 0; c < phraseCount; c++)
            {
                do 
                {
                    // Generate a phrase.
                    for (int l = 0; l < wordCount; l++)
                    {
                        sb.Append(dict[random.GetRandomInt32(dict.Count)]);
                        sb.Append(' ');
                    }
                    sb.Remove(sb.Length - 1, 1);

                    // Apply mutators.
                    if (mutators != null)
                    {
                        foreach (var m in mutators)
                            m.Mutate(sb, randomWrapper);
                    }

                    // Finally, remove spaces if required (as the mutators depend on whitespace to do their work).
                    if (!spaces)
                    {
                        for (int i = sb.Length - 1; i >= 0; i--)
                        {
                            if (sb[i] == ' ')
                                sb.Remove(i, 1);
                        }
                    }

                    attempts++;
                    
                // Ensure the final phrase is within the min / max chars.
                } while (attempts < MaxAttemptsPerCount && (sb.Length < minChars || sb.Length > maxChars));
                if (attempts >= MaxAttemptsPerCount)
                {
                    sb.Clear();
                    sb.Append("A passphrase could not be found matching your minimum and maximum length requirements");
                }


                // Yield the phrase and reset state.
                var result = sb.ToString();
                yield return result;
                sb.Clear();
                attempts = 0;
            }
            sw.Stop();

            var bytesRequested = (int)((random as Terninger.Random.CypherBasedPrngGenerator)?.BytesRequested).GetValueOrDefault();
            RandomService.LogPasswordStat("Passphrase", phraseCount, sw.Elapsed, bytesRequested, IPAddressHelpers.GetHostOrCacheIp(Request).AddressFamily, HttpContext.GetApiKeyId());
            if (!IpThrottlerService.HasAnyUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request)))
                RandomService.AddWebRequestEntropy(this.Request);
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), phraseCount);
        }
    }
}

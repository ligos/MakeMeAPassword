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
using MurrayGrant.ReadablePassphrase;
using MurrayGrant.ReadablePassphrase.Dictionaries;
using MurrayGrant.ReadablePassphrase.Words;
using MurrayGrant.ReadablePassphrase.PhraseDescription;
using MurrayGrant.ReadablePassphrase.Mutators;
using MurrayGrant.PasswordGenerator.Web.ActionResults;
using MurrayGrant.PasswordGenerator.Web.ViewModels.ApiV1;
using MurrayGrant.PasswordGenerator.Web.Helpers;
using MurrayGrant.PasswordGenerator.Web.Services;
using MurrayGrant.PasswordGenerator.Web.Filters;
using System.Threading.Tasks;
using MurrayGrant.Terninger;

namespace MurrayGrant.PasswordGenerator.Web.Controllers.Api.v1
{
    [OutputCache(NoStore = true, Duration = 0)]
    [IpThrottlingFilter]
    public class ApiReadablePassphraseV1Controller : Controller
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public readonly static int MaxCount = 50;
        public readonly static int DefaultMinChars = 1;
        public readonly static int DefaultMaxChars = Int32.MaxValue;
        public readonly static PhraseStrength DefaultPhraseStrength = PhraseStrength.Random;
        public readonly static int DefaultCount = 1;
        public readonly static int MaxAttemptsPerCount = 100;
        public readonly static NumericStyles DefaultWhenNumeric = NumericStyles.Never;
        public readonly static int DefaultNumbers = 2;
        public readonly static AllUppercaseStyles DefaultWhenUppercase = AllUppercaseStyles.Never;
        public readonly static int DefaultUppercase = 2;

        private readonly static Lazy<WordDictionary> Dictionary;

        static ApiReadablePassphraseV1Controller()
        {
            Dictionary = new Lazy<WordDictionary>(() =>
                                new ExplicitXmlDictionaryLoader().LoadFrom(System.Web.HttpContext.Current.Request.MapPath("~/content/data/dictionary.xml"))
                            );
        }

        // GET: /api/v1/readablepassphrase/plain
        public async Task<string> Plain(PhraseStrength? s, int? pc, string sp, int? minCh, int? maxCh, NumericStyles? whenNum, int? nums, AllUppercaseStyles? whenUp, int? ups)
        {
            // Return as plain text string.
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var phrases = this.SelectPhrases(random,
                            s.HasValue ? s.Value : PhraseStrength.Random,
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

        // GET: /api/v1/readablepassphrase/json
        public async Task<ActionResult> Json(PhraseStrength? s, int? pc, string sp, int? minCh, int? maxCh, NumericStyles? whenNum, int? nums, AllUppercaseStyles? whenUp, int? ups)
        {
            // Return as Json array.
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var phrases = this.SelectPhrases(random,
                            s.HasValue ? s.Value : PhraseStrength.Random,
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
        
        // GET: /api/v1/readablepassphrase/xml
        public async Task<ActionResult> Xml(PhraseStrength? s, int? pc, string sp, int? minCh, int? maxCh, NumericStyles? whenNum, int? nums, AllUppercaseStyles? whenUp, int? ups)
        {
            // Return as XML.
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var phrases = this.SelectPhrases(random,
                            s.HasValue ? s.Value : PhraseStrength.Random,
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

        // GET: /api/v1/readablepassphrase/combinations
#if !DEBUG
        [OutputCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public async Task<ActionResult> Combinations(PhraseStrength? s)
        {
            // Return information about the number of combinations as a JSON object.
            var result = new JsonRangedCombinationContainer();
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var generator = this.GetGenerator(random);
                var strength = s.HasValue ? s.Value : DefaultPhraseStrength;
                var combinations = generator.CalculateCombinations(strength);

                // This is reported as a range instead of just the usual single value.
                result.middle = new JsonCombinationContainer() { combinations = combinations.OptionalAverage };
                result.upper = new JsonCombinationContainer() { combinations = combinations.Longest };
                result.lower = new JsonCombinationContainer() { combinations = combinations.Shortest };
            }
            return new JsonNetResult(result);
        }

        private IEnumerable<string> SelectPhrases(IRandomNumberGenerator random, PhraseStrength strength, int phraseCount, bool includeSpaces, int minChars, int maxChars, NumericStyles whenNumeric, int numbersToAdd, AllUppercaseStyles whenUpper, int uppersToAdd)
        {
            if (minChars > maxChars)
                yield break;

            phraseCount = Math.Min(phraseCount, MaxCount);
            if (phraseCount <= 0)
                yield break;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var generator = this.GetGenerator(random);
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

            for (int c = 0; c < phraseCount; c++)
            {
                string candidate = "";
                do
                {
                    // Generate a phrase.
                    candidate = generator.Generate(strength, " ", mutators);

                    // Finally, remove spaces if required (as the mutators depend on whitespace to do their work).
                    if (!includeSpaces)
                        candidate = new string(candidate.Where(ch => ch != ' ').ToArray());
                    attempts++;

                    // Ensure the final phrase is within the min / max chars.
                } while (attempts < MaxAttemptsPerCount && (candidate.Length < minChars || candidate.Length > maxChars));
                if (attempts >= MaxAttemptsPerCount)
                    candidate = "A passphrase could not be found matching your minimum and maximum length requirements";

                // Yield the phrase and reset state.
                yield return candidate;
                attempts = 0;
            }
            sw.Stop();

            var bytesRequested = (int)((random as Terninger.Generator.CypherBasedPrngGenerator)?.BytesRequested).GetValueOrDefault();
            RandomService.LogPasswordStat("ReadablePassphrase", phraseCount, sw.Elapsed, bytesRequested);

            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), phraseCount);
        }


        private ReadablePassphraseGenerator GetGenerator(IRandomNumberGenerator random)
        {
            // Creates a generator with a seeded random number generator, and loaded dictionary.
            // Only one dictionary is loaded and shared between all threads.
            var wrapper = new MurrayGrant.ReadablePassphrase.Random.ExternalRandomSource(random.GetRandomBytes);
            var result = new ReadablePassphraseGenerator(Dictionary.Value, wrapper);
            return result;
        }
    }
}

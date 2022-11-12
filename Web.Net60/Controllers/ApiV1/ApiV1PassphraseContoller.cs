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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MurrayGrant.MakeMeAPassword.Web.Net60.Services;
using MurrayGrant.MakeMeAPassword.Web.Net60.Helpers;
using MurrayGrant.MakeMeAPassword.Web.Net60.Models.ApiV1;
using MurrayGrant.Terninger;
using MurrayGrant.Terninger.Random;
using MurrayGrant.ReadablePassphrase.Mutators;

namespace MurrayGrant.MakeMeAPassword.Web.Net60.Controllers.ApiV1
{
    
    public class ApiV1PassphraseContoller : ApiV1Controller
    {
        public readonly static int MaxWords = 16;
        public readonly static int MaxCount = 50;
        public readonly static int DefaultMinChars = 1;
        public readonly static int DefaultMaxChars = Int32.MaxValue;
        public readonly static int DefaultWords = 4;
        public readonly static int DefaultCount = 1;
        public readonly static int MaxAttemptsPerCount = 500;
        public readonly static int MaxAttemptsPerCountWithMutators = 25;   // Mutators are quite expensive, so we won't try as hard.
        public readonly static NumericStyles DefaultWhenNumeric = NumericStyles.Never;
        public readonly static int DefaultNumbers = 2;
        public readonly static AllUppercaseStyles DefaultWhenUppercase = AllUppercaseStyles.Never;
        public readonly static int DefaultUppercase = 2;


        public ApiV1PassphraseContoller(PooledEntropyCprngGenerator terninger, PasswordRatingService ratingService, PasswordStatisticService statisticService, DictionaryService dictionaryService) 
            : base(terninger, ratingService, statisticService, dictionaryService) { }

        [HttpGet("/api/v1/passphrase/plain")]
        public IActionResult Plain([FromQuery]int? pc, [FromQuery]int? wc, [FromQuery]string sp, [FromQuery]int? minCh, [FromQuery]int? maxCh, [FromQuery]NumericStyles? whenNum, [FromQuery]int? nums, [FromQuery]AllUppercaseStyles? whenUp, [FromQuery]int? ups)
        {
            // Return as plain text string.
            using (var random = _Terninger.CreateCypherBasedGenerator())
            {
                var phrases = SelectPhrases(_DictionaryService.PassphraseDictionary, random,
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
                return Plain(phrases.ToList());
            }
        }

        [HttpGet("/api/v1/passphrase/json")]
        public IActionResult Json([FromQuery]int? pc, [FromQuery]int? wc, [FromQuery]string sp, [FromQuery]int? minCh, [FromQuery]int? maxCh, [FromQuery]NumericStyles? whenNum, [FromQuery]int? nums, [FromQuery]AllUppercaseStyles? whenUp, [FromQuery]int? ups)
        {
            // Return as Json array.
            using (var random = _Terninger.CreateCypherBasedGenerator())
            {
                var phrases = SelectPhrases(_DictionaryService.PassphraseDictionary, random,
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
                return Json(new JsonPasswordContainer() { pws = phrases.ToList() });
            }
        }

        [HttpGet("/api/v1/passphrase/xml")]
        public IActionResult Xml([FromQuery]int? pc, [FromQuery]int? wc, [FromQuery]string sp, [FromQuery]int? minCh, [FromQuery]int? maxCh, [FromQuery]NumericStyles? whenNum, [FromQuery]int? nums, [FromQuery]AllUppercaseStyles? whenUp, [FromQuery]int? ups)
        {
            // Return as XML.
            using (var random = _Terninger.CreateCypherBasedGenerator())
            {
                var phrases = SelectPhrases(_DictionaryService.PassphraseDictionary, random,
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
                return Xml(phrases.ToList());
            }
        }

        [HttpGet("/api/v1/passphrase/combinations")]
#if !DEBUG
        [ResponseCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public IActionResult Combinations([FromQuery]int? wc)
        {
            // Return information about the number of combinations as a JSON object.
            var wordCount = Math.Min(wc.HasValue ? wc.Value : DefaultWords, MaxWords);
            var combinations = Math.Pow(_DictionaryService.PassphraseDictionary.Count, wordCount);
            var result = new JsonCombinationContainer()
            {
                combinations = combinations,
                rating = _RatingService.Rate(combinations),
            };
            return Json(result);
        }

        private IEnumerable<string> SelectPhrases(IList<string> dict, IRandomNumberGenerator random, int wordCount, int phraseCount, bool spaces, int minChars, int maxChars, NumericStyles whenNumeric, int numbersToAdd, AllUppercaseStyles whenUpper, int uppersToAdd)
        {
            if (minChars > maxChars)
                yield break;

            phraseCount = Math.Min(phraseCount, MaxCount);
            wordCount = Math.Min(wordCount, MaxWords);
            if (phraseCount <= 0 || wordCount <= 0)
                yield break;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sb = new StringBuilder();
            int attempts = 0;
            int maxAttemptsPerCount = MaxAttemptsPerCount;

            // Setup any mutators required.
            ICollection<IMutator>? mutators = null;
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
            MurrayGrant.ReadablePassphrase.Random.RandomSourceBase? randomWrapper = null;
            if (mutators != null)
            {
                randomWrapper = new MurrayGrant.ReadablePassphrase.Random.ExternalRandomSource(random.GetRandomBytes);
                maxAttemptsPerCount = MaxAttemptsPerCountWithMutators;
            }

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
                } while (attempts < maxAttemptsPerCount && (sb.Length < minChars || sb.Length > maxChars));
                if (attempts >= maxAttemptsPerCount)
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

            PostSelectionAction("Passphrase", phraseCount, sw.Elapsed, random);
        }
    }
}

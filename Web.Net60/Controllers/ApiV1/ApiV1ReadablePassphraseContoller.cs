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
using MurrayGrant.ReadablePassphrase;
using MurrayGrant.ReadablePassphrase.Dictionaries;

namespace MurrayGrant.MakeMeAPassword.Web.Net60.Controllers.ApiV1
{
    
    public class ApiV1ReadablePassphraseContoller : ApiV1Controller
    {
        public readonly static int MaxCount = 50;
        public readonly static int DefaultMinChars = 1;
        public readonly static int DefaultMaxChars = Int32.MaxValue;
        public readonly static PhraseStrength DefaultPhraseStrength = PhraseStrength.Random;
        public readonly static int DefaultCount = 1;
        public readonly static int MaxAttemptsPerCount = 500;
        public readonly static NumericStyles DefaultWhenNumeric = NumericStyles.Never;
        public readonly static int DefaultNumbers = 2;
        public readonly static AllUppercaseStyles DefaultWhenUppercase = AllUppercaseStyles.Never;
        public readonly static int DefaultUppercase = 2;


        public ApiV1ReadablePassphraseContoller(PooledEntropyCprngGenerator terninger, PasswordRatingService ratingService, PasswordStatisticService statisticService, DictionaryService dictionaryService) 
            : base(terninger, ratingService, statisticService, dictionaryService) { }

        [HttpGet("/api/v1/readablepassphrase/plain")]
        public IActionResult Plain(
            [FromQuery]PhraseStrength? s, 
            [FromQuery]int? pc, 
            [FromQuery]string sp, 
            [FromQuery]int? minCh, 
            [FromQuery]int? maxCh, 
            [FromQuery]NumericStyles? whenNum, 
            [FromQuery]int? nums, 
            [FromQuery]AllUppercaseStyles? whenUp, 
            [FromQuery]int? ups,
            [FromQuery]string noFake)
        {
            // Return as plain text string.
            using (var random = _Terninger.CreateCypherBasedGenerator())
            {
                var dictionary = noFake.IsTruthy(false)
                                ? _DictionaryService.ReadablePassphraseDictionaryForNonFakeWords
                                : _DictionaryService.ReadablePassphraseDictionaryForAllWords;
                var phrases = SelectPhrases(dictionary, 
                                    random,
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
                return Plain(phrases.ToList());
            }
        }

        [HttpGet("/api/v1/readablepassphrase/json")]
        public IActionResult Json(
            [FromQuery]PhraseStrength? s, 
            [FromQuery]int? pc, 
            [FromQuery]string sp, 
            [FromQuery]int? minCh, 
            [FromQuery]int? maxCh, 
            [FromQuery]NumericStyles? whenNum, 
            [FromQuery]int? nums, 
            [FromQuery]AllUppercaseStyles? whenUp, 
            [FromQuery]int? ups,
            [FromQuery] string noFake)
        {
            // Return as Json array.
            using (var random = _Terninger.CreateCypherBasedGenerator())
            {
                var dictionary = noFake.IsTruthy(false)
                                ? _DictionaryService.ReadablePassphraseDictionaryForNonFakeWords
                                : _DictionaryService.ReadablePassphraseDictionaryForAllWords;
                var phrases = SelectPhrases(dictionary,
                                    random,
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
                return Json(new JsonPasswordContainer() { pws = phrases.ToList() });
            }
        }

        [HttpGet("/api/v1/readablepassphrase/xml")]
        public IActionResult Xml(
            [FromQuery]PhraseStrength? s, 
            [FromQuery]int? pc, 
            [FromQuery]string sp, 
            [FromQuery]int? minCh, 
            [FromQuery]int? maxCh, 
            [FromQuery]NumericStyles? whenNum, 
            [FromQuery]int? nums, 
            [FromQuery]AllUppercaseStyles? whenUp, 
            [FromQuery]int? ups,
            [FromQuery]string noFake)
        {
            // Return as XML.
            using (var random = _Terninger.CreateCypherBasedGenerator())
            {
                var dictionary = noFake.IsTruthy(false)
                                ? _DictionaryService.ReadablePassphraseDictionaryForNonFakeWords
                                : _DictionaryService.ReadablePassphraseDictionaryForAllWords;
                var phrases = SelectPhrases(dictionary,
                                    random,
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
                return Xml(phrases.ToList());
            }
        }

        [HttpGet("/api/v1/readablepassphrase/dictionary")]
#if !DEBUG
        [ResponseCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public ActionResult Dictionary() =>
            new FileStreamResult(
                    typeof(MurrayGrant.ReadablePassphrase.Dictionaries.Default).Assembly.GetManifestResourceStream("MurrayGrant.ReadablePassphrase.dictionary.xml.gz")
                    , "application/x-gzip"
            );


        [HttpGet("/api/v1/readablepassphrase/combinations")]
#if !DEBUG
        [ResponseCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public IActionResult Combinations([FromQuery]PhraseStrength? s, [FromQuery]string noFake)
        {
            // Return information about the number of combinations as a JSON object.
            using (var random = _Terninger.CreateCypherBasedGenerator())
            {
                var dictionary = noFake.IsTruthy(false)
                                ? _DictionaryService.ReadablePassphraseDictionaryForNonFakeWords
                                : _DictionaryService.ReadablePassphraseDictionaryForAllWords;
                var generator = this.GetGenerator(dictionary, random);
                var strength = s.HasValue ? s.Value : DefaultPhraseStrength;
                var combinations = generator.CalculateCombinations(strength);

                // This is reported as a range instead of just the usual single value.
                var result = new JsonRangedCombinationContainer()
                {
                    middle = new JsonCombinationContainer()
                    {
                        combinations = combinations.OptionalAverage,
                        rating = _RatingService.Rate(combinations.OptionalAverage),
                    },
                    upper = new JsonCombinationContainer() 
                    { 
                        combinations = combinations.Longest,
                        rating = _RatingService.Rate(combinations.Longest),
                    },
                    lower = new JsonCombinationContainer() 
                    { 
                        combinations = combinations.Shortest,
                        rating = _RatingService.Rate(combinations.Shortest),
                    }
                };
                return Json(result);
            }
        }

        private IEnumerable<string> SelectPhrases(WordDictionary dictionary, IRandomNumberGenerator random, PhraseStrength strength, int phraseCount, bool includeSpaces, int minChars, int maxChars, NumericStyles whenNumeric, int numbersToAdd, AllUppercaseStyles whenUpper, int uppersToAdd)
        {
            if (minChars > maxChars)
                yield break;

            phraseCount = Math.Min(phraseCount, MaxCount);
            if (phraseCount <= 0)
                yield break;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var generator = this.GetGenerator(dictionary, random);
            int attempts = 0;

            // Setup mutators.
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

            PostSelectionAction("ReadablePassphrase", phraseCount, sw.Elapsed, random);
        }

        private ReadablePassphraseGenerator GetGenerator(WordDictionary dictionary, IRandomNumberGenerator random)
        {
            // Creates a generator with a seeded random number generator, and loaded dictionary.
            // Only one dictionary is loaded and shared between all threads.
            var wrapper = new MurrayGrant.ReadablePassphrase.Random.ExternalRandomSource(random.GetRandomBytes);
            var result = MurrayGrant.ReadablePassphrase.Generator.Create(dictionary, wrapper);
            return result;
        }
    }
}

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
using MurrayGrant.PasswordGenerator.Web.ActionResults;
using MurrayGrant.PasswordGenerator.Web.ViewModels.ApiV1;
using MurrayGrant.PasswordGenerator.Web.Helpers;
using MurrayGrant.PasswordGenerator.Web.Services;
using MurrayGrant.PasswordGenerator.Web.Filters;

namespace MurrayGrant.PasswordGenerator.Web.Controllers.Api.v1
{
    [OutputCache(NoStore = true, Duration = 0)]
    [IpThrottlingFilter]
    [ApiCorsAnyFilter]
#if !DEBUG && !NOHTTPS
    [RequireHttps]
#endif
    public class ApiReadablePassphraseV1Controller : Controller
    {
        public readonly static int MaxCount = 50;
        public readonly static int DefaultMinChars = 1;
        public readonly static int DefaultMaxChars = Int32.MaxValue;
        public readonly static PhraseStrength DefaultPhraseStrength = PhraseStrength.Random;
        public readonly static int DefaultCount = 1;
        public readonly static int MaxAttemptsPerCount = 100;

        private readonly static Lazy<WordDictionary> Dictionary;

        static ApiReadablePassphraseV1Controller()
        {
            Dictionary = new Lazy<WordDictionary>(() =>
                                new ExplicitXmlDictionaryLoader().LoadFrom(System.Web.HttpContext.Current.Request.MapPath("~/content/data/dictionary.xml"))
                            );
        }

        // GET: /api/v1/readablepassphrase/plain
        public string Plain(PhraseStrength? s, int? pc, string sp, int? minCh, int? maxCh)
        {
            // Return as plain text string.
            var phrases = this.SelectPhrases(
                            s.HasValue ? s.Value : PhraseStrength.Random,
                            pc.HasValue ? pc.Value : DefaultCount,
                            sp.IsTruthy(true),
                            minCh.HasValue ? minCh.Value : DefaultMinChars,
                            maxCh.HasValue ? maxCh.Value : DefaultMaxChars);
            
            return String.Join(Environment.NewLine, phrases);
        }

        // GET: /api/v1/readablepassphrase/json
        public ActionResult Json(PhraseStrength? s, int? pc, string sp, int? minCh, int? maxCh)
        {
            // Return as Json array.
            var phrases = this.SelectPhrases(
                            s.HasValue ? s.Value : PhraseStrength.Random,
                            pc.HasValue ? pc.Value : DefaultCount,
                            sp.IsTruthy(true),
                            minCh.HasValue ? minCh.Value : DefaultMinChars,
                            maxCh.HasValue ? maxCh.Value : DefaultMaxChars);
            return new JsonNetResult(new JsonPasswordContainer() { pws = phrases.ToList() });
        }
        
        // GET: /api/v1/readablepassphrase/xml
        public ActionResult Xml(PhraseStrength? s, int? pc, string sp, int? minCh, int? maxCh)
        {
            // Return as XML.
            var phrases = this.SelectPhrases(
                            s.HasValue ? s.Value : PhraseStrength.Random,
                            pc.HasValue ? pc.Value : DefaultCount,
                            sp.IsTruthy(true),
                            minCh.HasValue ? minCh.Value : DefaultMinChars,
                            maxCh.HasValue ? maxCh.Value : DefaultMaxChars);
            return new XmlResult(phrases.ToList());
        }

        // GET: /api/v1/readablepassphrase/combinations
#if !DEBUG
        [OutputCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public ActionResult Combinations(PhraseStrength? s)
        {
            // Return information about the number of combinations as a JSON object.
            var result = new JsonRangedCombinationContainer();
            var generator = this.GetGenerator(RandomService.GetForCurrentThread());
            var strength = s.HasValue ? s.Value : DefaultPhraseStrength;
            var combinations = generator.CalculateCombinations(strength);

            // This is reported as a range instead of just the usual single value.
            result.middle = new JsonCombinationContainer() { combinations = combinations.OptionalAverage };
            result.upper = new JsonCombinationContainer() { combinations = combinations.Longest };
            result.lower = new JsonCombinationContainer() { combinations = combinations.Shortest };
            return new JsonNetResult(result);
        }

        private IEnumerable<string> SelectPhrases(PhraseStrength strength, int phraseCount, bool includeSpaces, int minChars, int maxChars)
        {
            if (minChars > maxChars)
                yield break;

            phraseCount = Math.Min(phraseCount, MaxCount);
            var random = RandomService.GetForCurrentThread();
            var generator = this.GetGenerator(random);
            int attempts = 0;

            random.BeginStats(this.GetType());
            try
            {
                for (int c = 0; c < phraseCount; c++)
                {
                    string candidate = "";
                    do
                    {
                        // Generate a phrase.
                        candidate = generator.Generate(strength, includeSpaces);
                        attempts++;

                        // Ensure the final phrase is within the min / max chars.
                    } while (attempts < MaxAttemptsPerCount && (candidate.Length < minChars || candidate.Length > maxChars));
                    if (attempts >= MaxAttemptsPerCount)
                        candidate = "A passphrase could not be found matching your minimum and maximum length requirements";

                    // Yield the phrase and reset state.
                    random.IncrementStats(candidate);
                    yield return candidate;
                    attempts = 0;
                }
            } finally {
                random.EndStats();
            }
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), phraseCount);
        }


        private ReadablePassphraseGenerator GetGenerator(RandomService random)
        {
            // Creates a generator with a seeded random number generator, and loaded dictionary.
            // Only one dictionary is loaded and shared between all threads.
            var wrapper = new MurrayGrant.ReadablePassphrase.Random.ExternalRandomSource(random.GetNextBytes);
            var result = new ReadablePassphraseGenerator(Dictionary.Value, wrapper);
            return result;
        }

        protected override void OnException(ExceptionContext filterContext)
        {
            if (!filterContext.ExceptionHandled)
            {

            }
        }
    }
}

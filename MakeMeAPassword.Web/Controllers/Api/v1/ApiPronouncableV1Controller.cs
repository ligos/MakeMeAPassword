﻿// Copyright 2014 Murray Grant
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
using MurrayGrant.PasswordGenerator.Web.Filters;
using System.Text;
using System.Threading.Tasks;
using MurrayGrant.Terninger;

namespace MurrayGrant.PasswordGenerator.Web.Controllers.Api.v1
{
    [OutputCache(NoStore = true, Duration = 0)]
    [IpThrottlingFilter]
    public class ApiPronouncableV1Controller : Controller
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public readonly static int MaxSyllableCount = 32;
        public readonly static int MaxCount = 50;
        public readonly static int DefaultSyllableCount = 4;
        public readonly static int DefaultCount = 1;

        private static readonly string[] ConsonantSounds = new string[] { "b", "c", "ck", "d", "f", "ph", "g", "gh", "h", "wh", "j", "dg", "l", "ll", "m", "mm", "mn", "n", "nn", "kn", "p", "pp", "qu", "r", "rr", "rh", "wr", "s", "ss", "st", "ps", "sc", "t", "tt", "v", "w", "wh", "x", "ks", "z", "zz", "sh", "ch", "th", "ar", "al", "ng", }.Distinct().ToArray();
        private static readonly string[] VowelSounds = new string[] { "a", "e", "ei", "ie", "ee", "ea", "ey", "i", "y", "o", "u", "ou", "ah", "oo", "oi", "oy", "oa", "ow", "ough", "or", "au", "augh", "igh", "ye", "er", "ir", "ur", "or", "re", "our", "ew", "ue", "ough", "eu", "ue", }.Distinct().ToArray();
        private static readonly Single ProbabilityOfTwoConsonantsInOneSyllable = 0.212f;

        // GET: /api/v1/pronouncable/plain
        public async Task<String> Plain(int? sc, int? c, string dsh)
        {
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var pws = SelectPasswords(random,
                                        sc.HasValue ? sc.Value : DefaultSyllableCount,
                                        c.HasValue ? c.Value : DefaultCount,
                                        dsh.IsTruthy(true));
                return String.Join(Environment.NewLine, pws);
            }
        }

        // GET: /api/v1/pronouncable/json
        public async Task<ActionResult> Json(int? sc, int? c, string dsh)
        {
            // Return as Json array.
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var pws = SelectPasswords(random,
                                        sc.HasValue ? sc.Value : DefaultSyllableCount,
                                        c.HasValue ? c.Value : DefaultCount,
                                        dsh.IsTruthy(true));
                return new JsonNetResult(new JsonPasswordContainer() { pws = pws.ToList() });
            }
        }

        // GET: /api/v1/pronouncable/xml
        public async Task<ActionResult> Xml(int? sc, int? c, string dsh)
        {
            // Return as XML.
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var pws = SelectPasswords(random,
                                        sc.HasValue ? sc.Value : DefaultSyllableCount,
                                        c.HasValue ? c.Value : DefaultCount,
                                        dsh.IsTruthy(true));
                return new XmlResult(pws.ToList());
            }
        }

        // GET: /api/v1/pronouncable/combinations
#if !DEBUG
        [OutputCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public ActionResult Combinations(int? sc)
        {
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), 1);
            
            var syllableCount = Math.Min(sc.HasValue ? sc.Value : DefaultSyllableCount, MaxSyllableCount);
            
            // Return information about the number of combinations as a JSON object.
            var result = new JsonCombinationContainer();
            result.combinations = Math.Pow(ConsonantSounds.Length * VowelSounds.Length * (ConsonantSounds.Length * ProbabilityOfTwoConsonantsInOneSyllable), syllableCount);
            return new JsonNetResult(result);
        }

        private IEnumerable<string> SelectPasswords(IRandomNumberGenerator random, int syllableCount, int count, bool hyphansBetweenSyllables)
        {
            syllableCount = Math.Min(syllableCount, MaxSyllableCount);
            count = Math.Min(count, MaxCount);
            if (count <= 0 || syllableCount <= 0)
                yield break;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sb = new StringBuilder();

            for (int c = 0; c < count; c++)
            {
                // Generate a password.
                for (int l = 0; l < syllableCount; l++)
                {
                    sb.Append(ConsonantSounds[random.GetRandomInt32(ConsonantSounds.Length)]);
                    sb.Append(VowelSounds[random.GetRandomInt32(VowelSounds.Length)]);
                    if (sb[sb.Length-2] != 'g' && sb[sb.Length-1] != 'h' 
                            && random.GetRandomSingle() < ProbabilityOfTwoConsonantsInOneSyllable)
                        sb.Append(ConsonantSounds[random.GetRandomInt32(ConsonantSounds.Length)]);

                    if (hyphansBetweenSyllables)
                        sb.Append('-');
                }
                if (hyphansBetweenSyllables && sb[sb.Length-1] == '-')
                    sb.Remove(sb.Length - 1, 1);


                // Yield the phrase and reset state.
                var result = sb.ToString();
                yield return result;
                sb.Clear();
            }
            sw.Stop();

            var bytesRequested = (int)((random as Terninger.Random.CypherBasedPrngGenerator)?.BytesRequested).GetValueOrDefault();
            RandomService.LogPasswordStat("Pronouncable", count, sw.Elapsed, bytesRequested, IPAddressHelpers.GetHostOrCacheIp(Request).AddressFamily, HttpContext.GetApiKeyId());
            if (!IpThrottlerService.HasAnyUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request)))
                RandomService.AddWebRequestEntropy(this.Request);
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), count);
        }
    }
}

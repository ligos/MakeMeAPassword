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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Services;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Helpers;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Filters;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Models.ApiV1;
using MurrayGrant.Terninger;
using MurrayGrant.Terninger.Random;

namespace MurrayGrant.MakeMeAPassword.Web.NetCore.Controllers.ApiV1
{
    
    public class ApiV1PinController : ApiV1Controller
    {
        public readonly static int MaxLength = 128;
        public readonly static int MaxCount = 50;
        public readonly static string Characters = "0123456789";
        public readonly static int DefaultLength = 4;
        public readonly static int DefaultCount = 1;

        public ApiV1PinController(PooledEntropyCprngGenerator terninger, PasswordRatingService ratingService, PasswordStatisticService statisticService, IpThrottlerService ipThrottler, DictionaryService dictionaryService) 
            : base(terninger, ratingService, statisticService, ipThrottler, dictionaryService) { }

        [HttpGet("/api/v1/pin/plain")]
        public async Task<IActionResult> Plain([FromQuery]int? l, [FromQuery]int? c, [FromQuery]string sym)
        {
            // Return as plain text string.
            using (var random = await _Terninger.CreateCypherBasedGeneratorAsync())
            {
                var blacklist = await _DictionaryService.ReadPinBlacklist();
                var passwords = SelectPasswords(random, blacklist,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount);
                return Plain(passwords.ToList());
            }
        }

        [HttpGet("/api/v1/pin/json")]
        public async Task<IActionResult> Json([FromQuery]int? l, [FromQuery]int? c, [FromQuery]string sym)
        {
            // Return as Json array.
            using (var random = await _Terninger.CreateCypherBasedGeneratorAsync())
            {
                var blacklist = await _DictionaryService.ReadPinBlacklist();
                var passwords = SelectPasswords(random, blacklist,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount);
                return Json(new JsonPasswordContainer() { pws = passwords.ToList() });
            }
        }

        [HttpGet("/api/v1/pin/xml")]
        public async Task<IActionResult> Xml([FromQuery]int? l, [FromQuery]int? c, [FromQuery]string sym)
        {
            // Return as XML.
            using (var random = await _Terninger.CreateCypherBasedGeneratorAsync())
            {
                var blacklist = await _DictionaryService.ReadPinBlacklist();
                var passwords = SelectPasswords(random, blacklist,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount);
                return Xml(passwords.ToList());
            }
        }

        [HttpGet("/api/v1/pin/combinations")]
#if !DEBUG
        [ResponseCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public async Task<IActionResult> Combinations(int? l, string sym)
        {
            IncrementUsage(1);

            // Return information about the number of combinations as a JSON object.
            var result = new JsonCombinationContainer();
            var length = Math.Min(l.HasValue ? l.Value : DefaultLength, MaxLength);
            var blacklist = await _DictionaryService.ReadPinBlacklist();
            result.combinations = Math.Pow(Characters.Length, length);
            result.combinations -= (double)blacklist.Count(x => x.Length == l);       // Remove blacklist entries.
            result.rating = _RatingService.RatePin(result.combinations);
            return Json(result);
        }

        private IEnumerable<string> SelectPasswords(IRandomNumberGenerator random, IEnumerable<string> blacklist, int length, int count)
        {
            length = Math.Min(length, MaxLength);
            count = Math.Min(count, MaxCount);
            if (count <= 0 || length <= 0)
                yield break;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sb = new StringBuilder();

            for (int c = 0; c < count; c++)
            {
                for (int l = 0; l < length; l++)
                    sb.Append(Characters[random.GetRandomInt32(Characters.Length)]);

                var candidate = sb.ToString();
                if (!blacklist.Contains(candidate)
                        // 4 digit PINs starting with '19' are more likely, so weight them lower.
                        || (length == 4 && candidate.Substring(0, 2) == "19" && random.GetRandomInt32(0, 3) == 0))
                {
                    yield return candidate;
                }

                sb.Clear();
            }
            sw.Stop();

            PostSelectionAction("Pin", count, sw.Elapsed, random);
        }
    }
}

// Copyright 2024 Murray Grant
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
using MurrayGrant.MakeMeAPassword.Web.Services;
using MurrayGrant.MakeMeAPassword.Web.Helpers;
using MurrayGrant.MakeMeAPassword.Web.Models.ApiV1;
using MurrayGrant.Terninger;
using MurrayGrant.Terninger.Random;

namespace MurrayGrant.MakeMeAPassword.Web.Controllers.ApiV1
{
    
    public class ApiV1HexController : ApiV1Controller
    {
        public readonly static int MaxLength = 128;      // 96 (=1024 bits) yields Double.Infinity for combinations!
        public readonly static int MaxCount = 50;
        public readonly static int DefaultLength = 8;
        public readonly static int DefaultCount = 1;

        public ApiV1HexController(PooledEntropyCprngGenerator terninger, PasswordRatingService ratingService, PasswordStatisticService statisticService, DictionaryService dictionaryService)
            : base(terninger, ratingService, statisticService, dictionaryService) { }

        [HttpGet("/api/v1/hex/plain")]
        [HttpHead("/api/v1/hex/plain")]
        public IActionResult Plain([FromQuery]int? l, [FromQuery]int? c, [FromQuery]string sym)
        {
            // Return as plain text string.
            using (var random = _Terninger.CreateCypherBasedGenerator())
            {
                var passwords = SelectPasswords(random,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount);
                return Plain(passwords.ToList());
            }
        }

        [HttpGet("/api/v1/hex/json")]
        [HttpHead("/api/v1/hex/json")]
        public IActionResult Json([FromQuery]int? l, [FromQuery]int? c, [FromQuery]string sym)
        {
            // Return as Json array.
            using (var random = _Terninger.CreateCypherBasedGenerator())
            {
                var passwords = SelectPasswords(random,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount);
                return Json(new JsonPasswordContainer() { pws = passwords.ToList() });
            }
        }

        [HttpGet("/api/v1/hex/xml")]
        [HttpHead("/api/v1/hex/xml")]
        public IActionResult Xml([FromQuery]int? l, [FromQuery]int? c, [FromQuery]string sym)
        {
            // Return as XML.
            using (var random = _Terninger.CreateCypherBasedGenerator())
            {
                var passwords = SelectPasswords(random,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount);
                return Xml(passwords.ToList());
            }
        }

        [HttpGet("/api/v1/hex/combinations")]
        [HttpHead("/api/v1/hex/combinations")]
#if !DEBUG
        [ResponseCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public ActionResult Combinations([FromQuery]int? l, [FromQuery]string sym)
        {
            // Return information about the number of combinations as a JSON object.
            var length = Math.Min(l.HasValue ? l.Value : DefaultLength, MaxLength);
            var combinations = Math.Pow(256, length);
            var result = new JsonCombinationContainer()
            {
                combinations = combinations,
                rating = _RatingService.Rate(combinations),
            };
            return Json(result);
        }

        private IEnumerable<string> SelectPasswords(IRandomNumberGenerator random, int length, int count)
        {
            length = Math.Min(length, MaxLength);
            count = Math.Min(count, MaxCount);
            if (count <= 0 || length <= 0)
                yield break;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                var bytes = random.GetRandomBytes(length);
                var result = String.Join("", bytes.Select(x => x.ToString("x2")));

                yield return result;
            }
            sw.Stop();

            PostSelectionAction("Hex", count, sw.Elapsed, random);
        }
    }
}

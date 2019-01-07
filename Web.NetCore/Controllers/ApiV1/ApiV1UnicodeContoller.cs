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
using System.Globalization;
using Microsoft.Extensions.Caching.Memory;

namespace MurrayGrant.MakeMeAPassword.Web.NetCore.Controllers.ApiV1
{
    
    public class ApiV1UnicodeController : ApiV1Controller
    {
        private readonly IMemoryCache _MemoryCache;     // Used to cache Unicode combination counts.

        public readonly static int MaxLength = 64;
        public readonly static int MaxCount = 50;
        public readonly static int MaxAttemptsPerCodePoint = 5000;
        public readonly static int DefaultLength = 8;
        public readonly static int DefaultCount = 1;
        public readonly static bool DefaultBmp = true;
        public readonly static bool DefaultAsian = false;

        public readonly static HashSet<UnicodeCategory> DefaultCategories = new HashSet<UnicodeCategory>()
            {
                UnicodeCategory.ClosePunctuation,
                UnicodeCategory.ConnectorPunctuation,
                UnicodeCategory.CurrencySymbol,
                UnicodeCategory.DashPunctuation,
                UnicodeCategory.DecimalDigitNumber,
                UnicodeCategory.EnclosingMark,
                UnicodeCategory.FinalQuotePunctuation,
                UnicodeCategory.InitialQuotePunctuation,
                UnicodeCategory.LetterNumber,
                UnicodeCategory.LowercaseLetter,
                UnicodeCategory.MathSymbol,
                UnicodeCategory.OpenPunctuation,
                // The 'OtherLetter' category contains East Asian characters and swamps other categories.
		        UnicodeCategory.OtherNumber,
                UnicodeCategory.OtherPunctuation,
                UnicodeCategory.OtherSymbol,
                UnicodeCategory.SpaceSeparator,
                UnicodeCategory.TitlecaseLetter,
                UnicodeCategory.UppercaseLetter,
            };
        public readonly static HashSet<UnicodeCategory> AsianCategories = new HashSet<UnicodeCategory>()
            {
                UnicodeCategory.ClosePunctuation,
                UnicodeCategory.ConnectorPunctuation,
                UnicodeCategory.CurrencySymbol,
                UnicodeCategory.DashPunctuation,
                UnicodeCategory.DecimalDigitNumber,
                UnicodeCategory.EnclosingMark,
                UnicodeCategory.FinalQuotePunctuation,
                UnicodeCategory.InitialQuotePunctuation,
                UnicodeCategory.LetterNumber,
                UnicodeCategory.LowercaseLetter,
                UnicodeCategory.MathSymbol,
                UnicodeCategory.OpenPunctuation,
                UnicodeCategory.OtherLetter,        // This category contains East Asian characters, about 49k of them!.
		        UnicodeCategory.OtherNumber,
                UnicodeCategory.OtherPunctuation,
                UnicodeCategory.OtherSymbol,
                UnicodeCategory.SpaceSeparator,
                UnicodeCategory.TitlecaseLetter,
                UnicodeCategory.UppercaseLetter,
            };


        public ApiV1UnicodeController(PooledEntropyCprngGenerator terninger, PasswordRatingService ratingService, PasswordStatisticService statisticService, IpThrottlerService ipThrottler, DictionaryService dictionaryService
                , IMemoryCache memoryCache)
            : base(terninger, ratingService, statisticService, ipThrottler, dictionaryService)
        {
            _MemoryCache = memoryCache;
        }

        [HttpGet("/api/v1/unicode/plain")]
        public async Task<IActionResult> Plain([FromQuery]int? l, [FromQuery]int? c, [FromQuery]string bmp, [FromQuery]string asian)
        {
            // Return as plain text string.
            using (var random = await _Terninger.CreateCypherBasedGeneratorAsync())
            {
                var passwords = SelectPasswords(random,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount,
                                bmp.IsTruthy(DefaultBmp),
                                asian.IsTruthy(DefaultAsian)
                            );
                return Plain(passwords.ToList());
            }
        }

        [HttpGet("/api/v1/unicode/json")]
        public async Task<IActionResult> Json([FromQuery]int? l, [FromQuery]int? c, [FromQuery]string bmp, [FromQuery]string asian)
        {
            // Return as Json array.
            using (var random = await _Terninger.CreateCypherBasedGeneratorAsync())
            {
                var passwords = SelectPasswords(random,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount,
                                bmp.IsTruthy(DefaultBmp),
                                asian.IsTruthy(DefaultAsian)
                            );
                return Json(new JsonPasswordContainer() { pws = passwords.ToList() });
            }
        }

        [HttpGet("/api/v1/unicode/xml")]
        public async Task<IActionResult> Xml([FromQuery]int? l, [FromQuery]int? c, [FromQuery]string bmp, [FromQuery]string asian)
        {
            // Return as XML.
            using (var random = await _Terninger.CreateCypherBasedGeneratorAsync())
            {
                var passwords = SelectPasswords(random,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount,
                                bmp.IsTruthy(DefaultBmp),
                                asian.IsTruthy(DefaultAsian)
                            );
                return Xml(passwords.ToList());
            }
        }

        [HttpGet("/api/v1/unicode/combinations")]
#if !DEBUG
        [OutputCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public ActionResult Combinations([FromQuery]int? l, [FromQuery]string bmp, [FromQuery]string asian)
        {
            IncrementUsage(1);

            var length = Math.Min(l.HasValue ? l.Value : DefaultLength, MaxLength);
            var onlyFromBasicMultilingualPlane = bmp.IsTruthy(DefaultBmp);
            var includeEastAsianCharacters = asian.IsTruthy(DefaultAsian);

            var allowedCategories = includeEastAsianCharacters ? AsianCategories : DefaultCategories;
            var mask = onlyFromBasicMultilingualPlane ? 0x0000ffff : 0x001fffff;

            // This takes ~100ms to calculate on Murray's laptop, so we cache it.
            // Unless new unicode characters magically appear, the result will always be the same for our 3 inputs.
            var cacheKey = "UnicodeCombinations-" +
                           (length
                            | (onlyFromBasicMultilingualPlane ? 0x01000000 : 0)
                            | (includeEastAsianCharacters ? 0x02000000 : 0)).ToString("x8");
            int keyspace = _MemoryCache.GetOrCreate(cacheKey, ce =>
            {
                var cacheKeyspace = Enumerable.Range(0, 0x001fffff & mask)
                    .Where(cp => !(this.InvalidSurrogateCodePoints(cp) || this.InvalidMaxCodePoints(cp)))
                    .Select(cp => Char.ConvertFromUtf32(cp))
                    .Where(s => allowedCategories.Contains(Char.GetUnicodeCategory(s, 0)))
                    .Count();
                ce.SetSlidingExpiration(TimeSpan.FromHours(24))
                    .SetValue(cacheKeyspace);
                return cacheKeyspace;
            });

            // Return information about the number of combinations as a JSON object.
            var result = new JsonCombinationContainer();
            result.combinations = Math.Pow(keyspace, length);
            result.rating = _RatingService.Rate(result.combinations);
            return Json(result);
        }

        private IEnumerable<string> SelectPasswords(IRandomNumberGenerator random, int length, int count, bool onlyFromBasicMultilingualPlane, bool includeEastAsianCharacters)
        {
            length = Math.Min(length, MaxLength);
            count = Math.Min(count, MaxCount);
            if (count <= 0 || length <= 0)
                yield break;
            var allowedCategories = includeEastAsianCharacters ? AsianCategories : DefaultCategories;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            int numberOfCharacters = 0, attempts = 0;
            var mask = onlyFromBasicMultilingualPlane ? 0x0000ffff : 0x001fffff;
            var maxAttempts = MaxAttemptsPerCodePoint * length;
            var sb = new StringBuilder();

            for (int i = 0; i < count; i++)
            {
                numberOfCharacters = 0;
                attempts = 0;
                sb.Clear();

                while (numberOfCharacters < length)
                {
                    // Get random int32 and create a code point from it.
                    // PERF: can reduce number of bytes required here based on the mask.
                    var codePoint = random.GetRandomInt32();
                    codePoint = codePoint & mask;       // Mask off the top bits, which aren't used.
                    attempts++;

                    // Break if too many attempts.
                    if (attempts > maxAttempts)
                        break;

                    // Surrogate code points are invalid.
                    if (this.InvalidSurrogateCodePoints(codePoint))
                        continue;
                    // Ensure the code point is not outside the maximum range.
                    if (this.InvalidMaxCodePoints(codePoint))
                        continue;

                    // the Int32 to up to 2 Char structs (in a string).
                    var s = Char.ConvertFromUtf32(codePoint);
                    var category = Char.GetUnicodeCategory(s, 0);
                    if (!allowedCategories.Contains(category))
                        // Not allowed category.
                        continue;
                    sb.Append(s);
                    numberOfCharacters++;
                }

                if (attempts >= maxAttempts)
                {
                    sb.Clear();
                    sb.Append("A password could not be found matching your requirements.");
                    if (!onlyFromBasicMultilingualPlane)
                        sb.Append(" Try only selecting characters from the Basic Multilingual Plane (`bmp` parameter).");
                }

                var result = sb.ToString();
                yield return result;
                attempts = 0;
            }
            sw.Stop();

            PostSelectionAction("Unicode", count, sw.Elapsed, random);
        }


        private bool InvalidSurrogateCodePoints(int cp) => cp >= 0xd800 && cp <= 0xdfff;
        private bool InvalidMaxCodePoints(int cp) => cp >= 0x10ffff;
    }
}

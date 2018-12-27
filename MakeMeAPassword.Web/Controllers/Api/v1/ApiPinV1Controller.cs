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
using MurrayGrant.Terninger;
using System.Threading.Tasks;

namespace MurrayGrant.PasswordGenerator.Web.Controllers.Api.v1
{
    [OutputCache(NoStore = true, Duration = 0)]
    [IpThrottlingFilter]
    public class ApiPinV1Controller : Controller
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public readonly static int MaxLength = 128;
        public readonly static int MaxCount = 50;
        public readonly static string Characters = "0123456789";
        public readonly static int DefaultLength = 4;
        public readonly static int DefaultCount = 1;

        public readonly static Lazy<HashSet<string>> Blacklist;

        static ApiPinV1Controller()
        {
            Blacklist = new Lazy<HashSet<string>>(() =>
                {
                    // Black list taken from values published here: http://www.datagenetics.com/blog/september32012/
                    var fi = new System.IO.FileInfo(System.Web.HttpContext.Current.Request.MapPath("~/content/data/PinBlacklist.txt"));
                    return new HashSet<string>(fi.YieldLines().Select(x => x.Trim()), StringComparer.Ordinal);
                }, true);
        }


        // GET: /api/v1/pin/plain
        public async Task<String> Plain(int? c, int? l)
        {
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var pins = SelectPins(random,
                                  l.HasValue ? l.Value : DefaultLength,
                                  c.HasValue ? c.Value : DefaultCount);
                return String.Join(Environment.NewLine, pins);
            }
        }

        // GET: /api/v1/pin/json
        public async Task<ActionResult> Json(int? c, int? l)
        {
            // Return as Json array.
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var pins = SelectPins(random,
                                  l.HasValue ? l.Value : DefaultLength,
                                  c.HasValue ? c.Value : DefaultCount);
                return new JsonNetResult(new JsonPasswordContainer() { pws = pins.ToList() });
            }
        }

        // GET: /api/v1/readablepassphrase/xml
        public async Task<ActionResult> Xml(int? c, int? l)
        {
            // Return as XML.
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var pins = SelectPins(random,
                                      l.HasValue ? l.Value : DefaultLength,
                                      c.HasValue ? c.Value : DefaultCount);
                return new XmlResult(pins.ToList());
            }
        }

        // GET: /api/v1/readablepassphrase/combinations
#if !DEBUG
        [OutputCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public ActionResult Combinations(int? l)
        {
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), 1);

            // Return information about the number of combinations as a JSON object.
            var result = new JsonCombinationContainer();
            var length = Math.Min(l.HasValue ? l.Value : DefaultLength, MaxLength);
            
            result.combinations = Math.Pow(Characters.Length, length);
            result.combinations -= (double)Blacklist.Value.Count(x => x.Length == l);       // Remove blacklist entries.
            result.rating = PasswordRatingService.RatePin(result.combinations);
            return new JsonNetResult(result);
        }

        private IEnumerable<string> SelectPins(IRandomNumberGenerator random, int length, int count)
        {
            length = Math.Min(length, MaxLength);
            count = Math.Min(count, MaxCount);
            if (count <= 0 || length <= 0)
                yield break;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sb = new StringBuilder();
            var blacklist = Blacklist.Value;

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

            var bytesRequested = (int)((random as Terninger.Random.CypherBasedPrngGenerator)?.BytesRequested).GetValueOrDefault();
            RandomService.LogPasswordStat("Pin", count, sw.Elapsed, bytesRequested, IPAddressHelpers.GetHostOrCacheIp(Request).AddressFamily, HttpContext.GetApiKeyId());
            if (!IpThrottlerService.HasAnyUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request)))
                RandomService.AddWebRequestEntropy(this.Request);
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), count);
        }
    }
}

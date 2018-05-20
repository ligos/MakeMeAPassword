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
using MurrayGrant.PasswordGenerator.Web.Filters;
using System.Threading.Tasks;
using MurrayGrant.Terninger;

namespace MurrayGrant.PasswordGenerator.Web.Controllers.Api.v1
{
    [OutputCache(NoStore = true, Duration = 0)]
    [IpThrottlingFilter]
    public class ApiHexV1Controller : Controller
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public readonly static int MaxLength = 128;      // 96 (=1024 bits) yields Double.Infinity for combinations!
        public readonly static int MaxCount = 50;
        public readonly static int DefaultLength = 8;
        public readonly static int DefaultCount = 1;

        // GET: /api/v1/hex/plain
        public async Task<String> Plain(int? l, int? c)
        {
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var passwords = SelectPasswords(random,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount);
                return String.Join(Environment.NewLine, passwords);
            }
        }

        // GET: /api/v1/hex/json
        public async Task<ActionResult> Json(int? l, int? c)
        {
            // Return as Json array.
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var passwords = SelectPasswords(random,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount);
                return new JsonNetResult(new JsonPasswordContainer() { pws = passwords.ToList() });
            }
        }

        // GET: /api/v1/hex/xml
        public async Task<ActionResult> Xml(int? l, int? c)
        {
            // Return as XML.
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var passwords = SelectPasswords(random,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount);
                return new XmlResult(passwords.ToList());
            }
        }

        // GET: /api/v1/hex/combinations
#if !DEBUG
        [OutputCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public ActionResult Combinations(int? l)
        {
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), 1);

            // Return information about the number of combinations as a JSON object.
            var length = Math.Min(l.HasValue ? l.Value : DefaultLength, MaxLength);
            var result = new JsonCombinationContainer();
            result.combinations = Math.Pow(256, length);
            return new JsonNetResult(result);
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

            var bytesRequested = (int)((random as Terninger.Generator.CypherBasedPrngGenerator)?.BytesRequested).GetValueOrDefault();
            RandomService.LogPasswordStat("Hex", count, sw.Elapsed, bytesRequested);
            if (!IpThrottlerService.HasAnyUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request)))
                RandomService.AddWebRequestEntropy(this.Request);
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), count);
        }
    }
}

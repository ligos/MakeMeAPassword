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
    public class ApiAlphaNumericV1Controller : Controller
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public readonly static int MaxLength = 128;
        public readonly static int MaxCount = 50;
        public readonly static int DefaultLength = 8;
        public readonly static int DefaultCount = 1;
        public readonly static bool DefaultSymbols = false;

        public readonly static string AlphanumericCharacters = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public readonly static string SymbolCharacters = "!@#$%^&*()`-=~_+[]\\;',./{}|:\"<>?";
        public readonly static string AllCharacters = AlphanumericCharacters + SymbolCharacters;

        // GET: /api/v1/alphanumeric/plain
        public async Task<string> Plain(int? l, int? c, string sym)
        {
            // Return as plain text string.
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var passwords = SelectPasswords(random,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount,
                                sym.IsTruthy(DefaultSymbols));
                return String.Join(Environment.NewLine, passwords);
            }
        }

        // GET: /api/v1/readablepassphrase/json
        public async Task<ActionResult> Json(int? l, int? c, string sym)
        {
            // Return as Json array.
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var passwords = SelectPasswords(random,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount,
                                sym.IsTruthy(DefaultSymbols));
                return new JsonNetResult(new JsonPasswordContainer() { pws = passwords.ToList() });
            }
        }
        
        // GET: /api/v1/readablepassphrase/xml
        public async Task<ActionResult> Xml(int? l, int? c, string sym)
        {
            // Return as XML.
            using (var random = await RandomService.PooledGenerator.CreateCypherBasedGeneratorAsync())
            {
                var passwords = SelectPasswords(random,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount,
                                sym.IsTruthy(DefaultSymbols));
                return new XmlResult(passwords.ToList());
            }
        }

        // GET: /api/v1/alphanumeric/combinations
#if !DEBUG
        [OutputCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public ActionResult Combinations(int? l, string sym)
        {
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), 1);

            // Return information about the number of combinations as a JSON object.
            var result = new JsonCombinationContainer();
            var length = Math.Min(l.HasValue ? l.Value : DefaultLength, MaxLength);
            var symbols = sym.IsTruthy(DefaultSymbols);

            var charCount = symbols ? AllCharacters.Length : AlphanumericCharacters.Length;
            result.combinations = Math.Pow(charCount, length);
            return new JsonNetResult(result);
        }

        private IEnumerable<string> SelectPasswords(IRandomNumberGenerator random, int length, int count, bool includeSymbols)
        {
            length = Math.Min(length, MaxLength);
            count = Math.Min(count, MaxCount);
            if (count <= 0 || length <= 0)
                yield break;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var chars = includeSymbols ? AllCharacters : AlphanumericCharacters;
            var sb = new StringBuilder();

            for (int c = 0; c < count; c++)
            {
                for (int l = 0; l < length; l++)
                    sb.Append(chars[random.GetRandomInt32(chars.Length)]);

                var result = sb.ToString();
                yield return result;
                sb.Clear();
            }
            sw.Stop();

            var bytesRequested = (int)((random as Terninger.Random.CypherBasedPrngGenerator)?.BytesRequested).GetValueOrDefault();
            RandomService.LogPasswordStat("AlphaNumeric", count, sw.Elapsed, bytesRequested, IPAddressHelpers.GetHostOrCacheIp(Request).AddressFamily, HttpContext.GetApiKeyId());
            if (!IpThrottlerService.HasAnyUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request)))
                RandomService.AddWebRequestEntropy(this.Request);
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), count);
        }
    }
}

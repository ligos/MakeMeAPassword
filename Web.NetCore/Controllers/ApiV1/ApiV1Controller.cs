﻿// Copyright 2019 Murray Grant
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
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Xml.Serialization;

using Microsoft.AspNetCore.Mvc;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Middleware;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Services;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Helpers;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Models.ApiV1;
using MurrayGrant.Terninger;
using MurrayGrant.Terninger.Random;
using Microsoft.AspNetCore.Cors;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Filters;

namespace MurrayGrant.MakeMeAPassword.Web.NetCore.Controllers.ApiV1
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ServiceFilter(typeof(IpThrottlingFilter), IsReusable = true)]
    [EnableCors("Mmap")]
    public class ApiV1Controller : Controller
    {
        protected readonly PooledEntropyCprngGenerator _Terninger;
        protected readonly PasswordRatingService _RatingService;
        private readonly PasswordStatisticService _StatisticService;
        private readonly IpThrottlerService _IpThrottler;
        protected readonly DictionaryService _DictionaryService;

        public ApiV1Controller(PooledEntropyCprngGenerator terninger
                , PasswordRatingService ratingService
                , PasswordStatisticService statisticService
                , IpThrottlerService ipThrottler
                , DictionaryService dictionaryService)
        {
            _Terninger = terninger;
            _RatingService = ratingService;
            _StatisticService = statisticService;
            _IpThrottler = ipThrottler;
            _DictionaryService = dictionaryService;
        }

        protected void IncrementUsage(int count)
        {
            _IpThrottler.IncrementUsage(HttpContext.Connection.RemoteIpAddress, count, HttpContext.GetApiKeyId());
        }

        protected void PostSelectionAction(string name, int count, TimeSpan duration, IRandomNumberGenerator random)
        {
            var bytesRequested = (int)((random as Terninger.Random.CypherBasedPrngGenerator)?.BytesRequested).GetValueOrDefault();
            var clientIp = HttpContext.Connection.RemoteIpAddress;

            _StatisticService.LogPasswordStat(name, count, duration, bytesRequested, clientIp.AddressFamily, HttpContext.GetApiKeyId());
            IncrementUsage(count);
        }

        protected IActionResult Plain(IEnumerable<string> lines)
        {
            return new ContentResult() { Content = String.Join(StringHelpers.WindowsNewLine, lines) + StringHelpers.WindowsNewLine, ContentType = "text/plain" };
        }
        protected IActionResult Xml(object o)
        {
            if (o == null)
                return new ContentResult() { Content = "", ContentType = "text/xml" };

            var serialiser = new XmlSerializer(o.GetType());
            using (var output = new StringWriter())
            {
                output.NewLine = StringHelpers.WindowsNewLine;
                serialiser.Serialize(output, o);
                output.Flush();

                return new ContentResult() { Content = output.GetStringBuilder().ToString(), ContentType = "text/xml" };
            }
        }
    }
}

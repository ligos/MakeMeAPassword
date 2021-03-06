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
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using MurrayGrant.MakeMeAPassword.Web.NetCore.Helpers;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Services;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Middleware;
using System.Xml.Serialization;
using System.IO;

namespace MurrayGrant.MakeMeAPassword.Web.NetCore.Filters
{
    /// <summary>
    /// Implements IP address based limits.
    /// </summary>
    public class IpThrottlingFilter : IActionFilter
    {
        private readonly IpThrottlerService _IpThrottler;

        public IpThrottlingFilter(IpThrottlerService ipThrottler)
        {
            _IpThrottler = ipThrottler;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (_IpThrottler.HasExceededLimit(context.HttpContext.Connection.RemoteIpAddress, context.HttpContext.GetApiKeyId()))
            {
                var limitExceededMessage = "You have exceeded IP based limits. These will be lifted automatically within " + (int)(_IpThrottler.Options.ResetPeriodWithDefault.TotalHours + 1) + " hour(s).";
                if (!context.HttpContext.Request.Path.HasValue || context.HttpContext.Request.Path.Value.Contains("/plain"))
                    context.Result = new ContentResult() { ContentType = "text/plain", StatusCode = 429, Content = limitExceededMessage };
                else if (context.HttpContext.Request.Path.Value.Contains("/json"))
                    context.Result = new ContentResult() { ContentType = "application/json", StatusCode = 429, Content = "{ 'message':'" + limitExceededMessage + "'" };
                else if (context.HttpContext.Request.Path.Value.Contains("/xml"))
                {
                    var limitMessageAsList = new List<string>();
                    limitMessageAsList.Add(limitExceededMessage);
                    var serialiser = new XmlSerializer(limitMessageAsList.GetType());
                    using (var output = new StringWriter())
                    {
                        output.NewLine = StringHelpers.WindowsNewLine;
                        serialiser.Serialize(output, limitMessageAsList);
                        output.Flush();

                        context.Result = new ContentResult() { ContentType = "text/xml", StatusCode = 429, Content = output.GetStringBuilder().ToString(),  };
                    }
                }
                else
                    context.Result = new ContentResult() { ContentType = "text/plain", StatusCode = 429, Content = limitExceededMessage };
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No op.
        }

    }
}

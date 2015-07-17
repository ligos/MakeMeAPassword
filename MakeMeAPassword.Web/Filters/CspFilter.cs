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
using MurrayGrant.PasswordGenerator.Web.Services;
using MurrayGrant.PasswordGenerator.Web.Helpers;

namespace MurrayGrant.PasswordGenerator.Web.Filters
{
    /// <summary>
    /// Adds a Content-Security-Policy header to the request.
    /// </summary>
    public sealed class CspFilter : ActionFilterAttribute
    {
        public static readonly string BaseCsp = @"
default-src 'self';
script-src 'self' 'unsafe-inline' https://oss.maxcdn.com https://code.jquery.com https://netdna.bootstrapcdn.com;
style-src 'self' 'unsafe-inline' https://netdna.bootstrapcdn.com;
img-src 'self' https://netdna.bootstrapcdn.com https://i.creativecommons.org https://licensebuttons.net;
font-src 'self' https://netdna.bootstrapcdn.com;
".Replace(Environment.NewLine, "");

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var csp = BaseCsp;
            if (!filterContext.HttpContext.Request.IsSecureConnection)
                csp = csp.Replace("https://", "http://");

            filterContext.HttpContext.Response.AddHeader("Content-Security-Policy", csp);
        }
    }
}
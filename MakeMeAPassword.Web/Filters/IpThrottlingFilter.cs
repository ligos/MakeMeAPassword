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
    /// Implements IP address based limits.
    /// </summary>
    public sealed class IpThrottlingFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (IpThrottlerService.HasExceededLimit(IPAddressHelpers.GetHostOrCacheIp(filterContext.HttpContext.Request)))
            {
                bool isAjaxRequest = false;
                if (isAjaxRequest)
                {
                    // TODO: return a friendly message about IP limiting with appropriate HTTP status code.
                    filterContext.Result = new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden, "You have exceeded IP based limits. These will be lifted automatically within 2 hours.");
                }
                else
                {
                    // TODO: return plain text details about the error.
                    filterContext.Result = new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden, "You have exceeded IP based limits. These will be lifted automatically within 2 hours.");
                }
            }
        }
    }
}
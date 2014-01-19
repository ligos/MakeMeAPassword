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
using System.Net;
using System.Web;

namespace MurrayGrant.PasswordGenerator.Web.Helpers
{
    public static class IPAddressHelpers
    {
        /// <summary>
        /// Create an IP Address object based on the request. Pick up the user's underlying IP address if they're proxied.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static IPAddress GetHostOrCacheIp(HttpRequestBase request)
        {
            // TODO: if the user is reporting a proxy, use their underlying IP.
            var headers = request.Headers;
            return IPAddress.Parse(request.UserHostAddress);
        }
    }
}
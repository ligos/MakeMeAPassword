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
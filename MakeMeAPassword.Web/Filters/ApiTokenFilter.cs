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
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace MurrayGrant.PasswordGenerator.Web.Filters
{
    /// <summary>
    /// Loads the API usage limit bypass token.
    /// </summary>
    public sealed class ApiTokenFilter : ActionFilterAttribute
    {
        private static readonly Dictionary<string, string> _LimitByPassKeyToId =
                (ConfigurationManager.AppSettings["BypassKeys"] ?? "").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x =>
                    {
                        var pair = x.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        return new KeyValuePair<string, string>(pair[0].ToLower(), pair[1]);
                    })
                .ToDictionary(x => x.Key, x => x.Value);


        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var (maybeKey, keyFromPost) = GetBypassKey(filterContext.HttpContext.Request);
            if (String.IsNullOrEmpty(maybeKey))
                // No key.
                return;
            var keyId = LookupBypassKeyId(maybeKey);
            if (String.IsNullOrEmpty(maybeKey))
                // Key doesn't match anything.
                return;

            // User has provided a legit key!
            // Set the id in the http context for other parts of the app.
            filterContext.HttpContext.Items["Mmap.ApiKeyId"] = keyId;

            // As a positive acknowledgement, set an HTTP header.
            if (!filterContext.HttpContext.Response.HeadersWritten)
                filterContext.HttpContext.Response.Headers.Add("X-MmapApiId", keyId);

            // If the key was provided from a POST, we set a cookie.
            // This lets external callers provide the key from another site, and we remember it for a short time.
            if (keyFromPost)
                filterContext.HttpContext.Response.Cookies.Add(new HttpCookie("MmapApiKey", maybeKey) { HttpOnly = true, Expires = DateTime.UtcNow.Add(TimeSpan.FromHours(2)), Secure = true });
        }


        private string LookupBypassKeyId(string key)
        {
            if (String.IsNullOrEmpty(key))
                return null;
            var lookupKey = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(key)).ToHexString().ToLower();
            if (_LimitByPassKeyToId.TryGetValue(lookupKey, out var value))
                return value;
            else
                return null;
        }

        private (string key, bool wasFromPost) GetBypassKey(HttpRequestBase req)
        {
            // Bypass key can be in a HTTP header, query string, cookie, or post field.
            var result = "";
            result = req.Headers["X-MmapApiKey"];
            if (!String.IsNullOrEmpty(result))
                return (result, false);

            result = req.Form["ApiKey"];
            if (!String.IsNullOrEmpty(result))
                return (result, true);

            result = req.QueryString["ApiKey"];
            if (!String.IsNullOrEmpty(result))
                return (result, false);

            result = req.Cookies["MmapApiKey"]?.Value;

            return (result, false);
        }
    }
}
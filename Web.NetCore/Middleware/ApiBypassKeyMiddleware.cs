using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Microsoft.AspNetCore.Http;

using MurrayGrant.MakeMeAPassword.Web.NetCore.Helpers;
using Microsoft.AspNetCore.Builder;

namespace MurrayGrant.MakeMeAPassword.Web.NetCore.Middleware
{
    /// <summary>
    /// Middleware to read an API bypass key from a request, validate it and store in the http context.
    /// </summary>
    public class ApiBypassKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private Dictionary<string, string> _LimitByPassKeyToId;


        public ApiBypassKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var (maybeKey, keyFromPost) = GetBypassKey(context.Request);
            if (String.IsNullOrEmpty(maybeKey))
            {
                // No key.
                await _next(context);
                return;
            }
            // Ensure we have loaded the list of keys.
            if (_LimitByPassKeyToId == null)
                _LimitByPassKeyToId = await ReadBypassKeys();
            // Now look up the key.
            var keyId = LookupBypassKeyId(maybeKey);
            if (String.IsNullOrEmpty(keyId))
            {
                // Key doesn't match anything.
                await _next(context);
                return;
            }

            // User has provided a legit key!
            // Set the id in the http context for other parts of the app.
            context.Items["Mmap.ApiKeyId"] = keyId;

            // As a positive acknowledgment, set an HTTP header.
            if (!context.Response.HasStarted)
                context.Response.Headers.Add("X-MmapApiId", keyId);

            // If the key was provided from a POST, we set a cookie.
            // This lets external callers provide the key from another site, and we remember it for a short time.
            if (!context.Response.HasStarted && keyFromPost)
                context.Response.Cookies.Append("MmapApiKey", maybeKey, new CookieOptions() { HttpOnly = true, Expires = DateTime.UtcNow.Add(TimeSpan.FromHours(2)), Secure = true });

            await _next(context);
        }

        private async Task<Dictionary<string, string>> ReadBypassKeys()
        {
            // The file is a list of colon delimited hashes of keys to meaningful ids (so I can identify the user).
            // Eg:
            //   e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855:Null
            //   5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8:AUser
            var lines = await File.ReadAllLinesAsync("bypasskeys.txt");
            var result = lines.Where(l => !String.IsNullOrWhiteSpace(l))
                .Select(l => l.Trim())
                .Where(l => !l.StartsWith('#'))
                .Select(l => l.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(p => p.Length == 2)
                .Select(p => new KeyValuePair<string, string>(p[0].ToLower(), p[1]))
                .ToDictionary(x => x.Key, x => x.Value);
            return result;
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

        private (string key, bool wasFromPost) GetBypassKey(HttpRequest req)
        {
            // Bypass key can be in a HTTP header, query string, cookie, or post field.
            var result = "";
            result = req.Headers["X-MmapApiKey"];
            if (!String.IsNullOrEmpty(result))
                return (result, false);

            if (req.Method == "POST")
            {
                result = req.Form["ApiKey"];        // Throws if Content-Type isn't set right for a POST.
                if (!String.IsNullOrEmpty(result))
                    return (result, true);
            }

            result = req.Query["ApiKey"];
            if (!String.IsNullOrEmpty(result))
                return (result, false);

            result = req.Cookies["MmapApiKey"]; //?.Value;
            return (result, false);
        }
    }
    public static class ApiBypassKeyMiddlewareExtension
    {
        public static IApplicationBuilder UseApiBypassKeys(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ApiBypassKeyMiddleware>();
        }

        public static string GetApiKeyId(this HttpContext context) => (string)context.Items["Mmap.ApiKeyId"];
    }
}

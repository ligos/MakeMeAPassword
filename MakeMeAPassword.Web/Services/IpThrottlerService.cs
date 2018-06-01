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
using System.Collections.Concurrent;
using System.Linq;
using System.Web;
using System.Net;
using System.Runtime.Caching;
using TakeIo.NetworkAddress;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;
using MurrayGrant.PasswordGenerator.Web.Helpers;

namespace MurrayGrant.PasswordGenerator.Web.Services
{
    /// <summary>
    /// Throttle requests by IP address over a time period.
    /// </summary>
    public static class IpThrottlerService
    {
        public static readonly TimeSpan Timeout = TimeSpan.FromHours(1);
        
        // A request unit is one password / passphrase or one combination count (which is cached for an hour anyway).
        // Hitting any of the detailed pages requires 2 minimum, the front page requires 1.
        // Once an ip address exceeds this limit, it gets an access denied error until the limit is cleared.
        public static readonly int MaxRequestUnits = 200;
        private static object _Lock = new object();

        
        // Assume IPv6 netmasks are always /64
        private static readonly IPAddress _IPv6DefaultNetMask = new IPAddress(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

        private static readonly IPNetworkAddress[] _LocalNetworks = IPGlobalProperties.GetIPGlobalProperties().GetUnicastAddresses()
                .Where(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork || x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                .Where(x => !x.Address.Equals(IPAddress.Loopback) && !x.Address.Equals(IPAddress.IPv6Loopback))
                .Select(x => new IPNetworkAddress(x.Address, x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? _IPv6DefaultNetMask : x.IPv4Mask))
                .ToArray();

        private static readonly Dictionary<string, string> _LimitByPassKeyToId =
                (ConfigurationManager.AppSettings["BypassKeys"] ?? "").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x =>
                    {
                        var pair = x.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        return new KeyValuePair<string, string>(pair[0].ToLower(), pair[1]);
                    })
                .ToDictionary(x => x.Key, x => x.Value);
            


        public static bool HasExceededLimit(IPAddress ip, string maybeBypassKey)
        {
            // Local addresses are always valid.
            if (IPAddress.IsLoopback(ip))
                return false;

            // Local network addresses are always valid.
            for (int i = 0; i < _LocalNetworks.Length; i++)
            {
                var n = _LocalNetworks[i];
                if (n.Address.AddressFamily == ip.AddressFamily && n.IsSameNetwork(ip))
                    return false;
            }

            // A valid bypass key is always valid.
            if (!String.IsNullOrEmpty(maybeBypassKey) && !String.IsNullOrEmpty(LookupBypassKeyId(maybeBypassKey)))
                return false;

            var key = CacheKey(ip);
            int usage = 0;
            lock (_Lock)
            {
                var maybeUsage = MemoryCache.Default[key];
                if (maybeUsage != null)
                    usage = (int)maybeUsage;
            }

            return usage > MaxRequestUnits;
        }
        public static void IncrementUsage(IPAddress ip, int units)
        {
            var key = CacheKey(ip);
            lock(_Lock)
            {
                int usage = units;
                var maybeUsage = MemoryCache.Default[key];
                if (maybeUsage != null)
                    usage = usage + (int)maybeUsage;
                MemoryCache.Default.Set(key, usage, DateTimeOffset.Now.Add(Timeout));
            }
        }

        public static bool HasAnyUsage(IPAddress ip)
        {
            var key = CacheKey(ip);
            lock (_Lock)
            {
                var maybeUsage = MemoryCache.Default[key];
                return (maybeUsage != null);
            }
        }

        public static string LookupBypassKeyId(HttpRequestBase req) => LookupBypassKeyId(IpThrottlerService.GetBypassKey(req));
        public static string LookupBypassKeyId(string key)
        {
            if (String.IsNullOrEmpty(key))
                return null;
            var lookupKey = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(key)).ToHexString().ToLower();
            if (_LimitByPassKeyToId.TryGetValue(lookupKey, out var value))
                return value;
            else
                return null;
        }

        public static string GetBypassKey(HttpRequestBase req)
        {
            // Bypass key can be in a 
            var result = "";
            result = req.Headers["X-MmapApiKey"];
            if (!String.IsNullOrEmpty(result))
                return result;
            result = req.QueryString["ApiKey"];
            if (!String.IsNullOrEmpty(result))
                return result;
            result = req.Cookies["MmapApiKey"]?.Value;
            return result;
        }

        private static string CacheKey(IPAddress ip)
        {
            return "Throttle-" + ip.ToString();
        }
    }
}
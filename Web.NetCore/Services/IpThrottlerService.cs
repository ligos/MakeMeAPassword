// Copyright 2019 Murray Grant
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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using NetTools;

using MurrayGrant.MakeMeAPassword.Web.NetCore.Helpers;

namespace MurrayGrant.MakeMeAPassword.Web.NetCore.Services
{
    /// <summary>
    /// Throttle requests by IP address over a time period.
    /// </summary>
    public class IpThrottlerService
    {
        private readonly IOptions<IpThrottlerOptions> _Options;
        public IpThrottlerOptions Options => _Options.Value;
        private readonly IMemoryCache _MemoryCache;     // Used to store IP usage with timeouts.
        private readonly object _Lock = new object();

        public IpThrottlerService(IOptions<IpThrottlerOptions> options, IMemoryCache cache)
        {
            _Options = options;
            _MemoryCache = cache;
        }
        public bool HasExceededLimit(IPAddress ip, string maybeBypassId)
        {
            if (IsNonCountedAddress(ip, maybeBypassId))
                return false;

            var key = CacheKey(ip);
            lock (_Lock)
            {
                if (_MemoryCache.TryGetValue(key, out int currentUsage))
                    return currentUsage > _Options.Value.LimitWithDefault;
                else
                    return false;
            }
        }
        public void IncrementUsage(IPAddress ip, int units, string maybeBypassId)
        {
            if (IsNonCountedAddress(ip, maybeBypassId))
                return;

            var key = CacheKey(ip);
            lock (_Lock)
            {
                var currentUsage = _MemoryCache.GetOrCreate(key, ce =>
                {
                    ce.SetSlidingExpiration(_Options.Value.ResetPeriodWithDefault).SetValue(0);
                    return 0;
                });
                _MemoryCache.Set(key, currentUsage + units);
            }
        }

        private bool IsNonCountedAddress(IPAddress ip, string maybeBypassId)
        {
            // Local addresses are never counted.
            if (IPAddress.IsLoopback(ip))
                return true;

            // Link local addresses are never counted.
            if (ip.IsIPv6LinkLocal)
                return true;

            // Local network addresses are never counted.
            foreach (var n in _Options.Value.LocalNetworkRanges)
            {
                if (n.Begin.AddressFamily == ip.AddressFamily && n.Contains(ip))
                    return true;
            }

            // A valid bypass key is not counted.
            if (!String.IsNullOrEmpty(maybeBypassId))
                return true;

            return false;
        }

        // IPv6 addresses are keyed on network address, because there's a practically limitless number you could choose from in a standard /64 range.
        private string CacheKey(IPAddress ip) => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? ip.V6NetworkAddress().ToString()
                                               : ip.ToString();

        public class IpThrottlerOptions
        {
            public double? ResetPeriodHours { get; set; }
            public TimeSpan ResetPeriodWithDefault => TimeSpan.FromHours(ResetPeriodHours.HasValue ? ResetPeriodHours.Value : 1.0);
            public int? Limit { get; set; }
            public int LimitWithDefault => Limit.HasValue ? Limit.Value : 100;
            public IEnumerable<string> LocalNetworks { get; set; }
            private IEnumerable<IPAddressRange> _LocalNetworkRanges;
            public IEnumerable<IPAddressRange> LocalNetworkRanges
            {
                get {
                    if (_LocalNetworkRanges == null)
                        _LocalNetworkRanges = (LocalNetworks ?? Enumerable.Empty<string>()).Select(x => IPAddressRange.Parse(x)).ToList();
                    return _LocalNetworkRanges;
                }
            }
        }
    }
}
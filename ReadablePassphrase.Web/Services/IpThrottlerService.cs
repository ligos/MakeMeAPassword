using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Web;
using System.Net;
using System.Runtime.Caching;

namespace MurrayGrant.PasswordGenerator.Web.Services
{
    /// <summary>
    /// Throttle requests by IP address over a time period.
    /// </summary>
    public static class IpThrottlerService
    {
        public static readonly TimeSpan Timeout = TimeSpan.FromHours(2);
        
        // A request unit is one password / passphrase or one combination count (which is cached for an hour anyway).
        // Hitting any of the detailed pages requires 2 minimum, the front page requires 1.
        // Once an ip address exceeds this limit, it gets an access denied error until the limit is cleared.
        public static readonly int MaxRequestUnits = 150;
        private static object _Lock = new object();

        public static long TotalUsage = 0;

        public static bool HasExceededLimit(IPAddress ip)
        {
            // Local addresses are always valid.
            if (IPAddress.IsLoopback(ip))
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
            System.Threading.Interlocked.Add(ref TotalUsage, units);
        }

        private static string CacheKey(IPAddress ip)
        {
            return "Throttle-" + ip.ToString();
        }
    }
}
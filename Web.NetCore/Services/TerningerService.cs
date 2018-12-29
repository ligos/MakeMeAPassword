// Copyright 2018 Murray Grant
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
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

using MurrayGrant.Terninger;
using MurrayGrant.Terninger.Random;
using MurrayGrant.Terninger.EntropySources.Local;

namespace MurrayGrant.MakeMeAPassword.Web.NetCore.Services
{
    /// <summary>
    /// Provides an instance of the Terninger pooled random number generator.
    /// Should be configured as singleton in your DI framework.
    /// </summary>
    public class TerningerService
    {
        private static readonly NLog.Logger StatsLogger = NLog.LogManager.GetLogger("MurrayGrant.PasswordGenerator.PasswordStats");

        // This uses the new structured logging support in NLog 4.5+ to log to a CSV.
        public static void LogPasswordStat(string name, int count, TimeSpan duration, int randomBytesConsumed, System.Net.Sockets.AddressFamily addressFamily, string bypassKeyId)
            => StatsLogger.Info("{Name} {Count} {RandomBytesConsumed} {Duration:N3} {RandomBytesConsumedEa} {DurationEa:N4} {LocalOffset} {AddressFamily} {BypassKeyId}",
                name, count, randomBytesConsumed, duration.TotalMilliseconds, randomBytesConsumed == 0 ? 0 : (double)randomBytesConsumed / (double)count, duration.TotalMilliseconds / count, (DateTimeOffset.Now.Offset >= TimeSpan.Zero ? "+" : "-") + DateTimeOffset.Now.Offset.ToString("hh\\:mm"), addressFamily, bypassKeyId
            );



        public sealed class StatsByType
        {
            public readonly RuntimeTypeHandle StyleTypeHandle;
            public Type StyleType { get { return Type.GetTypeFromHandle(this.StyleTypeHandle); } }
            public String Style { get { return this.StyleType.Name.Replace("Api", "").Replace("Controller", "").Replace("V1", ""); } }

            private readonly CypherBasedPrngGenerator _Rng;

            public readonly int ThreadId;
            public DateTime LastUsedUtc;
            public long RandomBytesConsumed;
            public long PasswordsGenerated;
            public long PasswordUtf8BytesGenerated;
            public long PasswordCharactersGenerated;

            public StatsByType(int threadId, IRandomNumberGenerator rng, Type t)
            {
                this.StyleTypeHandle = t.TypeHandle;
                this.ThreadId = threadId;
                this._Rng = rng as CypherBasedPrngGenerator;
            }

            public void AddPassword(string password)
            {
                var bytes = Encoding.UTF8.GetByteCount(password);
                this.PasswordsGenerated++;
                this.PasswordUtf8BytesGenerated += bytes;
                this.PasswordCharactersGenerated += password.Length;
            }

            public void Add(StatsByType other)
            {
                this.RandomBytesConsumed += other.RandomBytesConsumed;
                this.PasswordsGenerated += other.PasswordsGenerated;
                this.PasswordUtf8BytesGenerated += other.PasswordUtf8BytesGenerated;
                this.PasswordCharactersGenerated += other.PasswordCharactersGenerated;
            }
        }
    }
    public static class TerningerServiceConfig
    {
        // TODO: move this into the Terninger Config package.
        public static PooledEntropyCprngGenerator AddTerninger(this IServiceCollection services) => AddTerninger(services, null);
        public static PooledEntropyCprngGenerator AddTerninger(this IServiceCollection services, Action<PooledEntropyCprngGenerator> confAction)
        {
            // Create and configure Terninger.
            var t = RandomGenerator.CreateTerninger();
            if (confAction != null)
                confAction(t);

            services.AddSingleton<PooledEntropyCprngGenerator>(t);

            // To support gathering entropy from web requests.
            var webRequestSource = new WebRequestEntropySource();
            t.AddInitialisedSource(webRequestSource);
            services.AddSingleton<WebRequestEntropySource>(webRequestSource);

            return t;
        }

        public static IApplicationBuilder UseTerningerEntropyFromWebRequests(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                var source = app.ApplicationServices.GetService<WebRequestEntropySource>();
                if (source != null)
                {
                    var sb = new StringBuilder();
                    // User's browser's headers (including languages accepted, user agent, etc).
                    foreach (var h in context.Request.Headers.Select(x => x.Key + ":" + x.Value))
                        sb.Append(h);
                    // User's IP address.
                    sb.Append(context.Connection.RemoteIpAddress.ToString());
                    // User's request path & query string.
                    sb.Append(context.Request.Path);
                    sb.Append(context.Request.QueryString);

                    var hasher = SHA256.Create();
                    var requestEntropy = hasher.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                    source.SetEntropy(requestEntropy);
                }

                await next.Invoke();
            });
        }
    }

    public class WebRequestEntropySource : UserSuppliedSource
    {
        // Because the Microsoft DI framework doesn't support named items, we have a custom source for web request entropy.
        public WebRequestEntropySource()
        {
            this.Name = "IncomingWebRequestSource";
        }
    }
}
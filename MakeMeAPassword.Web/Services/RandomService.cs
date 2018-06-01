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
using System.Security.Cryptography;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using MurrayGrant.PasswordGenerator.Web.Helpers;

using MurrayGrant.Terninger;
using MurrayGrant.Terninger.Generator;
using System.Threading.Tasks;
using System.Text;
using System.Web;

namespace MurrayGrant.PasswordGenerator.Web.Services
{
    /// <summary>
    /// Provides a global instance of the Terninger pooled random number generator.
    /// </summary>
    public static class RandomService
    {
        private static readonly NLog.Logger StatsLogger = NLog.LogManager.GetLogger("MurrayGrant.PasswordGenerator.PasswordStats");
        private static readonly Terninger.EntropySources.Local.UserSuppliedSource _WebRequestEntropySource = new Terninger.EntropySources.Local.UserSuppliedSource() { Name = "IncomingWebRequestSource" };

        public static readonly PooledEntropyCprngGenerator PooledGenerator =
                            RandomGenerator.CreateTerninger()
#if !DEBUG
                            .With(RandomGenerator.NetworkSources(
                                        userAgent: "Mozilla/5.0; Microsoft.NET; makemeapassword.ligos.net; makemeapassword@ligos.net; bitbucket.org/ligos/Terninger",
                                        hotBitsApiKey: System.Configuration.ConfigurationManager.AppSettings["HotBits.ApiKey"],
                                        randomOrgApiKey: System.Configuration.ConfigurationManager.AppSettings["RandomOrg.ApiKey"].ParseAsGuidOrNull()
                                )
                            )
#endif
                            .With(_WebRequestEntropySource)     // Gather entropy from people making web requests.
                            .StartNoWait();

        // This uses the new structured logging support in NLog 4.5+ to log to a CSV.
        public static void LogPasswordStat(string name, int count, TimeSpan duration, int randomBytesConsumed, System.Net.Sockets.AddressFamily addressFamily, string bypassKeyId)
            => StatsLogger.Info("{Name} {Count} {RandomBytesConsumed} {Duration:N3} {RandomBytesConsumedEa} {DurationEa:N4} {LocalOffset} {AddressFamily} {BypassKeyId}",
                name, count, randomBytesConsumed, duration.TotalMilliseconds, randomBytesConsumed == 0 ? 0 : (double)randomBytesConsumed / (double)count, duration.TotalMilliseconds / count, (DateTimeOffset.Now.Offset >= TimeSpan.Zero ? "+" : "-") + DateTimeOffset.Now.Offset.ToString("hh\\:mm"), addressFamily, bypassKeyId
            );


        // Gather entropy from people making web requests.
        public static void AddWebRequestEntropy(HttpRequestBase request)
        {
            var sb = new StringBuilder();
            var allThings = (request.AcceptTypes ?? Enumerable.Empty<string>())
                .Concat(request.UserLanguages ?? Enumerable.Empty<string>())
                .Concat(new[] { request.RawUrl ?? "", request.UserHostAddress ?? "", request.UserAgent ?? "" });
            foreach (var x in allThings)
                sb.Append(x);
            var hasher = SHA256.Create();
            var requestEntropy = hasher.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            _WebRequestEntropySource.SetEntropy(requestEntropy);
        }

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
}
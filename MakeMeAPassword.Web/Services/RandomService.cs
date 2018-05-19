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

namespace MurrayGrant.PasswordGenerator.Web.Services
{
    /// <summary>
    /// Provides a global instance of the Terninger pooled random number generator.
    /// </summary>
    public static class RandomService
    {
        public static readonly PooledEntropyCprngGenerator PooledGenerator =
                            RandomGenerator.CreateTerninger()
#if TRUE // !DEBUG
                            .With(RandomGenerator.NetworkSources(
                                        userAgent: "Mozilla/5.0; Microsoft.NET; makemeapassword.ligos.net; makemeapassword@ligos.net; bitbucket.org/ligos/Terninger",
                                        hotBitsApiKey: System.Configuration.ConfigurationManager.AppSettings["HotBits.ApiKey"],
                                        randomOrgApiKey: System.Configuration.ConfigurationManager.AppSettings["RandomOrg.ApiKey"].ParseAsGuidOrNull()
                                )
                            )
#endif
                            ;

        // This uses the new structured logging support in NLog 4.5+ to log to a CSV.
        private static readonly NLog.Logger StatsLogger = NLog.LogManager.GetLogger("MurrayGrant.PasswordGenerator.PasswordStats");
        public static void LogPasswordStat(string name, int count, TimeSpan duration, int randomBytesConsumed)
            => StatsLogger.Info("{Name} {Count} {RandomBytesConsumed} {Duration:N3} {RandomBytesConsumedEa} {DurationEa:N4} {LocalOffset}",
                name, count, randomBytesConsumed, duration.TotalMilliseconds, randomBytesConsumed == 0 ? 0 : (double)randomBytesConsumed / (double)count, duration.TotalMilliseconds / count, (DateTimeOffset.Now.Offset >= TimeSpan.Zero ? "+" : "-") + DateTimeOffset.Now.Offset.ToString("hh\\:mm")
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
}
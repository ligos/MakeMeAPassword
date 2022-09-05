// Copyright 2022 Murray Grant
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
using System.Threading.Tasks;
using MurrayGrant.MakeMeAPassword.Web.Net60.Models;
using MurrayGrant.Terninger.PersistentState;

namespace MurrayGrant.MakeMeAPassword.Web.NetCore.Services
{
    public static class TerningerServiceConfig
    {
        public static async Task<PooledEntropyCprngGenerator> AddTerningerAndWaitForFirstSeed(this IServiceCollection services, TerningerConfiguration config, bool includeNetworkSources)
        {
            // To support gathering entropy from web requests.
            var webRequestSource = new WebRequestEntropySource();

            // Create and configure Terninger.
            var cryptoRandomConfig = new CryptoRandomSource.Configuration() { SampleSize = config.CryptoRandomSampleSize };
            var sources = RandomGenerator.BasicSources(cryptoRandomConfig: cryptoRandomConfig)
                .Concat(ExtendedSources.All())
                .Concat(NetworkSources.All(
                        userAgent: NetworkSources.UserAgent(config.NetworkUserAgentIdentifier),
                        anuApiKey: config.AnuApiKey,
                        hotBitsApiKey: config.HotBitsApiKey,
                        randomOrgApiKey: config.RandomOrgApiKey
                    ).Where(_ => includeNetworkSources)
                )
                .Concat(new[] { webRequestSource });

            TextFileReaderWriter? stateReaderWriter = null;
            if (!String.IsNullOrEmpty(config.PersistentStatePath))
                stateReaderWriter = new TextFileReaderWriter(config.PersistentStatePath);

            var terninger = PooledEntropyCprngGenerator.Create(
                initialisedSources: sources,
                accumulator: new Terninger.Accumulator.EntropyAccumulator(config.LinearPoolCount, config.RandomPoolCount), 
                config: config.TerningerPooledGeneratorConfig, 
                persistentStateReader: stateReaderWriter, 
                persistentStateWriter: stateReaderWriter
            );
            terninger.AddInitialisedSource(webRequestSource);

            await terninger.StartAndWaitForSeedAsync();

            services.AddSingleton(terninger);
            services.AddSingleton(webRequestSource);

            return terninger;
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
                    if (context.Connection.RemoteIpAddress != null)
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
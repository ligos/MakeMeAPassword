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
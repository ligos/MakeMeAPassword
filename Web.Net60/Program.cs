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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MurrayGrant.MakeMeAPassword.Web.Net60.Services;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Services;
using MurrayGrant.Terninger.Random;
using NLog;
using NLog.Web;

namespace MurrayGrant.MakeMeAPassword.Web.Net60;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var logger = NLog.LogManager.Setup().LoadConfigurationFromFile(configFile: "nlog.config").GetCurrentClassLogger();
        try
        {
            System.Threading.Thread.CurrentThread.Name = "Default Thread";

            logger.Debug("MakeMeAPassword startup.");
            var (app, terninger) = await BuildWeb(args, logger);

            logger.Debug("MakeMeAPassword WebApplication.Run().");
            await app.RunAsync();

            logger.Debug("MakeMeAPassword shutdown.");
            await terninger.DisposeAsync();
        }
        catch (Exception ex)
        {
            //NLog: catch setup errors
            logger.Error(ex, "Stopped program because of exception");
            throw;
        }
        finally
        {
            // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
            NLog.LogManager.Shutdown();
        }
    }

    private static async Task<(WebApplication app, PooledEntropyCprngGenerator terninger)> BuildWeb(string[] args, Logger logger)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        logger.Debug("Environment: {0}, IsDevelopment: {1}, IsProduction: {2}.", builder.Environment.EnvironmentName, builder.Environment.IsDevelopment(), builder.Environment.IsProduction());
        logger.Debug("Runtime Version: {0}, System Version: {1}, OS: {2}, Source: {3}.", Environment.Version, System.Runtime.InteropServices.RuntimeEnvironment.GetSystemVersion(), Environment.OSVersion, System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
        
        var appConfig = builder.Configuration.GetSection("Mmap").Get<Models.AppConfig>();

        // Add services to the container.
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
        builder.Host.UseNLog();

        builder.Services.AddControllersWithViews();
        builder.Services.AddCors(c =>
        {
            c.AddPolicy("Mmap", b => b.AllowAnyOrigin()
              .WithMethods("GET", "POST")
              .SetPreflightMaxAge(TimeSpan.FromDays(1))
            );
        });
        builder.Services.AddCookiePolicy(options =>
        {
            // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            // MMAP does not use cookies, only local storage (and even that doesn't record anything personally identifiable).
            options.CheckConsentNeeded = context => false;
            options.MinimumSameSitePolicy = SameSiteMode.None;
        });

        var terninger = await builder.Services.AddTerningerAndWaitForFirstSeed(
            appConfig?.Terninger ?? new Models.TerningerConfiguration(), 
            includeNetworkSources: builder.Environment.IsProduction()
        );

        builder.Services.AddMemoryCache(o =>
        {
            o.ExpirationScanFrequency = TimeSpan.FromMinutes(15);
        });

        builder.Services.AddSingleton<PasswordRatingService>();
        builder.Services.AddSingleton<PasswordStatisticService>();
        var dictionaryService = await DictionaryService.Load();
        builder.Services.AddSingleton(dictionaryService);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }
        app.UseForwardedHeaders(new ForwardedHeadersOptions()
        {
            // Map X-Forwarded-For to HttpContext.Connection
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
        app.UseTerningerEntropyFromWebRequests();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors("Mmap");
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });

        return (app, terninger);
    }
}
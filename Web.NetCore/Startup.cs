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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using MurrayGrant.MakeMeAPassword.Web.NetCore.Services;

namespace MurrayGrant.MakeMeAPassword.Web.NetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Debug("ConfigureServices() start");

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                // MMAP does not use cookies, only local storage (and even that doesn't record anything personally identifiable).
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            
            // Initialise the random number generator.
            services.AddTerninger(t => {
                // TODO: work out how much of this can be added to Terninger Config package.
                t.AddInitialisedSources(Terninger.ExtendedSources.All());
                t.AddInitialisedSources(Terninger.NetworkSources.All(
                    userAgent: Terninger.NetworkSources.UserAgent("makemeapassword.ligos.net")
                ));
                    // TODO: read from config / secrets.
                    //hotBitsApiKey: // System.Configuration.ConfigurationManager.AppSettings["HotBits.ApiKey"],
                    //randomOrgApiKey: System.Configuration.ConfigurationManager.AppSettings["RandomOrg.ApiKey"].ParseAsGuidOrNull()
                //));
            }).Start();

            services.AddSingleton<PasswordRatingService>();

            logger.Debug("ConfigureServices() end");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Debug("Configure() start.");
            logger.Debug("Environment: {0}, IsDevelopment: {1}, IsProduction: {2}.", env.EnvironmentName, env.IsDevelopment(), env.IsProduction());
            logger.Debug("Runtime Version: {0}, System Version: {1}, OS: {2}, Source: {3}.", Environment.Version, System.Runtime.InteropServices.RuntimeEnvironment.GetSystemVersion(), Environment.OSVersion, System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            app.UseTerningerEntropyFromWebRequests();
            app.UseCors(c =>
            {
                c.AllowAnyOrigin()
                  .WithMethods("GET", "POST")
                  .WithHeaders("X-MmapApiKey", "Content-Type")
                  .SetPreflightMaxAge(TimeSpan.FromDays(1))
                  .Build();
            });
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            logger.Debug("Configure() end.");
        }
    }
}

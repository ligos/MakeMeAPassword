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
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MurrayGrant.PasswordGenerator.Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        protected void Application_Start()
        {
            Logger.Info("App Starting");

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // Path is required for native library to call one of the external random number generators.
            // http://stackoverflow.com/a/4598747
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + this.Server.MapPath("~/lib_native"), EnvironmentVariableTarget.Process);

            MvcHandler.DisableMvcResponseHeader = true;

            MurrayGrant.PasswordGenerator.Web.Services.RandomService.PooledGenerator.Start();

            Logger.Info("App Started");
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            Logger.Error(ex, "Unhandled error");
        }


        protected void Application_End(object sender, EventArgs e)
        {
            Logger.Info("Terninger pooled random generater stats: bytes requested = {0}, reseed count = {1}", Services.RandomService.PooledGenerator.BytesRequested, Services.RandomService.PooledGenerator.ReseedCount);
            Logger.Info("App Disposed");
            Services.RandomService.PooledGenerator.RequestStop();
            NLog.LogManager.Flush(TimeSpan.FromSeconds(30));
            NLog.LogManager.Shutdown();
        }
    }
}
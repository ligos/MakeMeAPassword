﻿// Copyright 2014 Murray Grant
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
using Exceptionless;

namespace MurrayGrant.PasswordGenerator.Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // Path is required for native library to call one of the external random number generators.
            // http://stackoverflow.com/a/4598747
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + this.Server.MapPath("~/lib_native"), EnvironmentVariableTarget.Process);

            Services.RandomSeedService.Singleton.Init(
                this.Server.MapPath("~/App_Data/random.org.key.txt"), 
                this.Server.MapPath("~/App_Data/qrng.physik.credentials.txt"),
                this.Server.MapPath("~/App_Data/email.json")
            );
            Services.RandomSeedService.Singleton.BeginLoadingExternalData();
            MvcHandler.DisableMvcResponseHeader = true;
        }

        protected void Application_Error(object sender, EventArgs e)
        {
#if !DEBUG
            Exception ex = Server.GetLastError();
            ex.ToExceptionless().MarkAsCritical().AddTags("Global.asax", "LastChance").Submit();
#endif
        }
    }
}
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
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "ReadablePassphrase",
                url: "api/v1/readablepassphrase/{action}",
                defaults: new { controller = "ApiReadablePassphraseV1" }
            );
            routes.MapRoute(
                name: "PIN",
                url: "api/v1/pin/{action}",
                defaults: new { controller = "ApiPinV1" }
            );
            routes.MapRoute(
                name: "AlphaNumeric",
                url: "api/v1/alphanumeric/{action}",
                defaults: new { controller = "ApiAlphaNumericV1" }
            );
            routes.MapRoute(
                name: "Passphrase",
                url: "api/v1/passphrase/{action}",
                defaults: new { controller = "ApiPassphraseV1" }
            );
            routes.MapRoute(
                name: "Pronouncable",
                url: "api/v1/pronouncable/{action}",
                defaults: new { controller = "ApiPronouncableV1" }
            );
            routes.MapRoute(
                name: "Unicode",
                url: "api/v1/unicode/{action}",
                defaults: new { controller = "ApiUnicodeV1" }
            );
            routes.MapRoute(
                name: "Hex",
                url: "api/v1/hex/{action}",
                defaults: new { controller = "ApiHexV1" }
            );
            

            routes.MapRoute(
                name: "Home",
                url: "",
                defaults: new { controller = "Home", action = "Index" }
            );
            routes.MapRoute(
                name: "About",
                url: "about",
                defaults: new { controller = "Home", action = "About" }
            );
            routes.MapRoute(
                name: "Contact",
                url: "contact",
                defaults: new { controller = "Home", action = "Contact" }
            );
            routes.MapRoute(
                name: "Technical",
                url: "technical",
                defaults: new { controller = "Home", action = "Technical" }
            );
            routes.MapRoute(
                name: "Donate",
                url: "donate",
                defaults: new { controller = "Home", action = "Donate" }
            );
            routes.MapRoute(
                name: "Limits",
                url: "limits",
                defaults: new { controller = "Home", action = "Limits" }
            );
            routes.MapRoute(
                name: "Faq",
                url: "faq",
                defaults: new { controller = "Home", action = "Faq" }
            );
            routes.MapRoute(
                name: "Password Manager",
                url: "passwordmanager",
                defaults: new { controller = "Home", action = "PasswordManager" }
            );
            routes.MapRoute(
                name: "Diagnostics",
                url: "diagnostics",
                defaults: new { controller = "Home", action = "Diagnostics" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
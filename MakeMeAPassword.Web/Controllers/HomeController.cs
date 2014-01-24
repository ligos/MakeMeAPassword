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
using System.Text;
using System.Web;
using System.Web.Mvc;
using MurrayGrant.PasswordGenerator.Web.Helpers;
using System.Dynamic;
using Newtonsoft.Json;
using MurrayGrant.PasswordGenerator.Web.ActionResults;
using MurrayGrant.PasswordGenerator.Web.Services;

namespace MurrayGrant.PasswordGenerator.Web.Controllers
{
    // Cache static pages for 1 hour.
#if !DEBUG
    [OutputCache(Duration = 60 * 60, Location = System.Web.UI.OutputCacheLocation.Any)]
#endif
    public class HomeController : Controller
    {
        // GET: /Home/
        public ActionResult Index()
        {
            ViewBag.CurrentNav = "Home";
            return View();
        }

        public ActionResult About()
        {
            ViewBag.CurrentNav = "About";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.CurrentNav = "Contact";
            return View();
        }

        public ActionResult Technical()
        {
            ViewBag.CurrentNav = "Technical";
            return View();
        }

        public ActionResult Donate()
        {
            // Paypal hasn't been setup yet: so return 404.
            return new HttpStatusCodeResult(System.Net.HttpStatusCode.NotFound);
            ViewBag.CurrentNav = "Donate";
            return View();
        }

        public ActionResult Faq()
        {
            ViewBag.CurrentNav = "Faq";
            return View();
        }
    }
}

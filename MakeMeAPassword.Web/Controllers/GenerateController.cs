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

namespace MurrayGrant.PasswordGenerator.Web.Controllers
{
#if !DEBUG
    [OutputCache(Duration = 60 * 60 * 24, Location = System.Web.UI.OutputCacheLocation.Any)]
#endif
    [Filters.CspFilter]
    public class GenerateController : Controller
    {
        //
        // GET: /Generate/
        public ActionResult Index()
        {
            ViewBag.CurrentNav = "GenerateIndex";
            return View();
        }

        public ActionResult ReadablePassphrase()
        {
            ViewBag.CurrentNav = "ReadablePassphrase";
            return View();
        }

        public ActionResult Passphrase()
        {
            ViewBag.CurrentNav = "DictionaryPassphrase";
            return View("Passphrase");
        }
        public ActionResult Dictionary()
        {
            return Passphrase();
        }

        public ActionResult Hex()
        {
            ViewBag.CurrentNav = "Hex";
            return View();
        }

        public ActionResult Unicode()
        {
            ViewBag.CurrentNav = "Unicode";
            return View();
        }

        public ActionResult AlphaNumeric()
        {
            ViewBag.CurrentNav = "AlphaNumeric";
            return View("AlphaNumeric");
        }
        public ActionResult LettersAndNumbers()
        {
            return AlphaNumeric();
        }

        public ActionResult Pin()
        {
            ViewBag.CurrentNav = "PIN";
            return View();
        }

        public ActionResult Pronouncable()
        {
            ViewBag.CurrentNav = "Pronouncable";
            return View();
        }
    }
}

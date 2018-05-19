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
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using MurrayGrant.PasswordGenerator.Web.ActionResults;
using System.Dynamic;
using MurrayGrant.PasswordGenerator.Web.Services;
using MurrayGrant.PasswordGenerator.Web.Helpers;
using System.Text;
using System.Management;

namespace MurrayGrant.PasswordGenerator.Web.Controllers
{
    public class ApiController : Controller
    {
        //
        // GET: /Api/
#if !DEBUG
        [OutputCache(Duration = 60 * 60 * 24, Location = System.Web.UI.OutputCacheLocation.Any)]
#endif
        public ActionResult Index()
        {
            ViewBag.PassphraseDictionaryCount = MurrayGrant.PasswordGenerator.Web.Controllers.Api.v1.ApiPassphraseV1Controller.Dictionary.Value.Count;
            return View();
        }
    }
}

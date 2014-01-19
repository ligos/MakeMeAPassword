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
using Newtonsoft.Json;

namespace MurrayGrant.PasswordGenerator.Web.ActionResults
{
    /// <summary>
    /// Use Json.Net to serialise JSON instead of the default.
    /// </summary>
    public class JsonNetResult : ActionResult
    {
        private readonly object _Result;
        public JsonNetResult(object o)
        {
            this._Result = o;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            var asJsonString = JsonConvert.SerializeObject(_Result, Formatting.None, settings);
            context.HttpContext.Response.ContentType = "application/json";
            context.HttpContext.Response.Write(asJsonString);
        }
    }
}
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
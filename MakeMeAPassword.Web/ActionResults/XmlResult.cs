using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Serialization;

namespace MurrayGrant.PasswordGenerator.Web.ActionResults
{
    /// <summary>
    /// Serialise an object to XML.
    /// </summary>
    public class XmlResult : ActionResult
    {
        private readonly object _Result;
        public XmlResult(object o)
        {
            this._Result = o;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var serialiser = new XmlSerializer(this._Result.GetType());
            serialiser.Serialize(context.HttpContext.Response.Output, this._Result);
        }
    }
}
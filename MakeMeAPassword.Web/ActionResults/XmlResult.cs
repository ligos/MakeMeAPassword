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
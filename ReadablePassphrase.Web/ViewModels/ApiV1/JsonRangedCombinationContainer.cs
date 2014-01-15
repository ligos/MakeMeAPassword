using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MurrayGrant.PasswordGenerator.Web.Services;

namespace MurrayGrant.PasswordGenerator.Web.ViewModels.ApiV1
{
    public class JsonRangedCombinationContainer
    {
        public JsonCombinationContainer upper { get; set; }
        public JsonCombinationContainer lower { get; set; }
        public JsonCombinationContainer middle { get; set; }
    }
}
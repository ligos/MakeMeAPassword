using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MurrayGrant.PasswordGenerator.Web.ViewModels.ApiV1
{
    public class JsonPasswordContainer
    {
        public IEnumerable<string> pws { get; set; }
        public string error { get; set; }
    }
}
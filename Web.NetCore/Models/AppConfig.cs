using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MurrayGrant.MakeMeAPassword.Web.NetCore.Models
{
    /// <summary>
    /// Application configuration.
    /// </summary>
    public class AppConfig
    {
        public bool TrustXForwardedFor { get; set; }
    }
}

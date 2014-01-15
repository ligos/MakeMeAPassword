using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MurrayGrant.PasswordGenerator.Web.Services;
using MurrayGrant.PasswordGenerator.Web.Helpers;

namespace MurrayGrant.PasswordGenerator.Web.Filters
{
    /// <summary>
    /// Implements IP address based limits.
    /// </summary>
    public sealed class IpThrottlingFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (IpThrottlerService.HasExceededLimit(IPAddressHelpers.GetHostOrCacheIp(filterContext.HttpContext.Request)))
            {
                bool isAjaxRequest = false;
                if (isAjaxRequest)
                {
                    // TODO: return a friendly message about IP limiting with appropriate HTTP status code.
                    filterContext.Result = new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden, "You have exceeded IP based limits. These will be lifted automatically within 2 hours.");
                }
                else
                {
                    // TODO: return plain text details about the error.
                    filterContext.Result = new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden, "You have exceeded IP based limits. These will be lifted automatically within 2 hours.");
                }
            }
        }
    }
}
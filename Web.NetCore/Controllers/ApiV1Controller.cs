using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Xml.Serialization;

using Microsoft.AspNetCore.Mvc;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Services;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Helpers;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Models.ApiV1;
using MurrayGrant.Terninger;
using MurrayGrant.Terninger.Random;

namespace MurrayGrant.MakeMeAPassword.Web.NetCore.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    //[IpThrottlingFilter]
    public class ApiV1Controller : Controller
    {
        protected readonly PooledEntropyCprngGenerator _Terninger;
        protected readonly PasswordRatingService _RatingService;

        public ApiV1Controller(PooledEntropyCprngGenerator terninger, PasswordRatingService ratingService)
        {
            _Terninger = terninger;
            _RatingService = ratingService;
        }

        protected void IncrementUsage(int count)
        {
            //IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), count);
        }

        protected void PostSelectionAction(string name, int count, TimeSpan duration, IRandomNumberGenerator random)
        {
            var bytesRequested = (int)((random as Terninger.Random.CypherBasedPrngGenerator)?.BytesRequested).GetValueOrDefault();
            //RandomService.LogPasswordStat("AlphaNumeric", count, sw.Elapsed, bytesRequested, IPAddressHelpers.GetHostOrCacheIp(Request).AddressFamily, HttpContext.GetApiKeyId());
            //if (!IpThrottlerService.HasAnyUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request)))
            //    RandomService.AddWebRequestEntropy(this.Request);
            //IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), count);
        }

        protected IActionResult Plain(IEnumerable<string> lines)
        {
            return new ContentResult() { Content = String.Join(StringHelpers.WindowsNewLine, lines), ContentType = "text/plain" };
        }
        protected IActionResult Xml(object o)
        {
            if (o == null)
                return new ContentResult() { Content = "", ContentType = "text/xml" };

            var serialiser = new XmlSerializer(o.GetType());
            using (var output = new StringWriter())
            {
                output.NewLine = StringHelpers.WindowsNewLine;
                serialiser.Serialize(output, o);
                output.Flush();

                return new ContentResult() { Content = output.GetStringBuilder().ToString(), ContentType = "text/xml" };
            }
        }
    }
}

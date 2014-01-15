using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MurrayGrant.PasswordGenerator.Web.ActionResults;
using MurrayGrant.PasswordGenerator.Web.ViewModels.ApiV1;
using MurrayGrant.PasswordGenerator.Web.Services;
using MurrayGrant.PasswordGenerator.Web.Helpers;
using System.Text;
using MurrayGrant.PasswordGenerator.Web.Filters;

namespace MurrayGrant.PasswordGenerator.Web.Controllers.Api.v1
{
    [OutputCache(NoStore = true, Duration = 0)]
    [IpThrottlingFilter]
    public class ApiPinV1Controller : Controller
    {
        public readonly static int MaxLength = 128;
        public readonly static int MaxCount = 50;
        public readonly static string Characters = "0123456789";
        public readonly static int DefaultLength = 4;
        public readonly static int DefaultCount = 1;

        public readonly static Lazy<HashSet<string>> Blacklist;

        static ApiPinV1Controller()
        {
            Blacklist = new Lazy<HashSet<string>>(() =>
                {
                    // Black list taken from values published here: http://www.datagenetics.com/blog/september32012/
                    var fi = new System.IO.FileInfo(System.Web.HttpContext.Current.Request.MapPath("~/content/data/PinBlacklist.txt"));
                    return new HashSet<string>(fi.YieldLines().Select(x => x.Trim()), StringComparer.Ordinal);
                }, true);
        }


        // GET: /api/v1/pin/plain
        public String Plain(int? c, int? l)
        {
            var pins = SelectPins(l.HasValue ? l.Value : DefaultLength, 
                                  c.HasValue ? c.Value : DefaultCount);
            return String.Join(Environment.NewLine, pins);
        }

        // GET: /api/v1/pin/json
        public ActionResult Json(int? c, int? l)
        {
            // Return as Json array.
            var pins = SelectPins(l.HasValue ? l.Value : DefaultLength,
                                  c.HasValue ? c.Value : DefaultCount);
            return new JsonNetResult(new JsonPasswordContainer() { pws = pins.ToList() });
        }

        // GET: /api/v1/readablepassphrase/xml
        public ActionResult Xml(int? c, int? l)
        {
            // Return as XML.
            var pins = SelectPins(l.HasValue ? l.Value : DefaultLength,
                                  c.HasValue ? c.Value : DefaultCount);
            return new XmlResult(pins.ToList());
        }

        // GET: /api/v1/readablepassphrase/combinations
#if !DEBUG
        [OutputCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public ActionResult Combinations(int? l)
        {
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), 1);

            // Return information about the number of combinations as a JSON object.
            var result = new JsonCombinationContainer();
            var length = Math.Min(l.HasValue ? l.Value : DefaultLength, MaxLength);
            
            result.combinations = Math.Pow(Characters.Length, length);
            result.combinations -= (double)Blacklist.Value.Count(x => x.Length == l);       // Remove blacklist entries.
            result.rating = PasswordRatingService.RatePin(result.combinations);
            return new JsonNetResult(result);
        }

        private IEnumerable<string> SelectPins(int length, int count)
        {
            length = Math.Min(length, MaxLength);
            count = Math.Min(count, MaxCount);

            var random = RandomService.GetForCurrentThread();
            var sb = new StringBuilder();
            var blacklist = Blacklist.Value;

            random.BeginStats(this.GetType());
            try
            {
                for (int c = 0; c < count; c++)
                {
                    for (int l = 0; l < length; l++)
                        sb.Append(Characters[random.Next(Characters.Length)]);
                
                    var candidate = sb.ToString();
                    if (!blacklist.Contains(candidate)
                            // 4 digit PINs starting with '19' are more likely, so weight them lower.
                            || (length == 4 && candidate.Substring(0, 2) == "19" && random.Next(0, 3) == 0))
                    {
                        random.IncrementStats(candidate);
                        yield return candidate;
                    }

                    sb.Clear();
                }
            } finally {
                random.EndStats();
            }
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), count);
        }
    }
}

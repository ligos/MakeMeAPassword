using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MurrayGrant.PasswordGenerator.Web.ActionResults;
using MurrayGrant.PasswordGenerator.Web.ViewModels.ApiV1;
using MurrayGrant.PasswordGenerator.Web.Services;
using MurrayGrant.PasswordGenerator.Web.Helpers;
using MurrayGrant.PasswordGenerator.Web.Filters;

namespace MurrayGrant.PasswordGenerator.Web.Controllers.Api.v1
{
    [OutputCache(NoStore = true, Duration = 0)]
    [IpThrottlingFilter]
    public class ApiHexV1Controller : Controller
    {
        public readonly static int MaxLength = 128;      // 96 (=1024 bits) yields Double.Infinity for combinations!
        public readonly static int MaxCount = 50;
        public readonly static int DefaultLength = 8;
        public readonly static int DefaultCount = 1;

        // GET: /api/v1/hex/plain
        public String Plain(int? l, int? c)
        {
            var passwords = SelectPasswords(
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount);
            return String.Join(Environment.NewLine, passwords);
        }

        // GET: /api/v1/hex/json
        public ActionResult Json(int? l, int? c)
        {
            // Return as Json array.
            var passwords = SelectPasswords(
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount);
            return new JsonNetResult(new JsonPasswordContainer() { pws = passwords.ToList() });
        }

        // GET: /api/v1/hex/xml
        public ActionResult Xml(int? l, int? c)
        {
            // Return as XML.
            var passwords = SelectPasswords(
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount);
            return new XmlResult(passwords.ToList());
        }

        // GET: /api/v1/hex/combinations
#if !DEBUG
        [OutputCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public ActionResult Combinations(int? l)
        {
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), 1);

            // Return information about the number of combinations as a JSON object.
            var length = Math.Min(l.HasValue ? l.Value : DefaultLength, MaxLength);
            var result = new JsonCombinationContainer();
            result.combinations = Math.Pow(256, length);
            return new JsonNetResult(result);
        }

        private IEnumerable<string> SelectPasswords(int length, int count)
        {
            length = Math.Min(length, MaxLength);
            count = Math.Min(count, MaxCount);

            var random = RandomService.GetForCurrentThread();
            random.BeginStats(this.GetType());
            try
            {

                for (int i = 0; i < count; i++)
                {
                    var bytes = random.GetNextBytes(length);
                    var result = String.Join("", bytes.Select(x => x.ToString("x2")));

                    random.IncrementStats(result);
                    yield return result;
                }
            } finally {
                random.EndStats();
            }
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), count);
        }
    }
}

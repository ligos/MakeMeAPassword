using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class ApiV1AlphaNumericController : ApiV1Controller
    {
        
        public readonly static int MaxLength = 128;
        public readonly static int MaxCount = 50;
        public readonly static int DefaultLength = 8;
        public readonly static int DefaultCount = 1;
        public readonly static bool DefaultSymbols = false;

        public readonly static string AlphanumericCharacters = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public readonly static string SymbolCharacters = "!@#$%^&*()`-=~_+[]\\;',./{}|:\"<>?";
        public readonly static string AllCharacters = AlphanumericCharacters + SymbolCharacters;

        public ApiV1AlphaNumericController(PooledEntropyCprngGenerator terninger, PasswordRatingService ratingService, PasswordStatisticService statisticService) 
            : base(terninger, ratingService, statisticService) { }

        [HttpGet("/api/v1/alphanumeric/plain")]
        public async Task<IActionResult> Plain([FromQuery]int? l, [FromQuery]int? c, [FromQuery]string sym)
        {
            // Return as plain text string.
            using (var random = await _Terninger.CreateCypherBasedGeneratorAsync())
            {
                var passwords = SelectPasswords(random,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount,
                                sym.IsTruthy(DefaultSymbols));
                return Plain(passwords.ToList());
            }
        }

        [HttpGet("/api/v1/alphanumeric/json")]
        public async Task<IActionResult> Json([FromQuery]int? l, [FromQuery]int? c, [FromQuery]string sym)
        {
            // Return as Json array.
            using (var random = await _Terninger.CreateCypherBasedGeneratorAsync())
            {
                var passwords = SelectPasswords(random,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount,
                                sym.IsTruthy(DefaultSymbols));
                return Json(new JsonPasswordContainer() { pws = passwords.ToList() });
            }
        }

        [HttpGet("/api/v1/alphanumeric/xml")]
        public async Task<IActionResult> Xml([FromQuery]int? l, [FromQuery]int? c, [FromQuery]string sym)
        {
            // Return as XML.
            using (var random = await _Terninger.CreateCypherBasedGeneratorAsync())
            {
                var passwords = SelectPasswords(random,
                                l.HasValue ? l.Value : DefaultLength,
                                c.HasValue ? c.Value : DefaultCount,
                                sym.IsTruthy(DefaultSymbols));
                return Xml(passwords.ToList());
            }
        }

        [HttpGet("/api/v1/alphanumeric/combinations")]
#if !DEBUG
        [OutputCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public ActionResult Combinations(int? l, string sym)
        {
            IncrementUsage(1);

            // Return information about the number of combinations as a JSON object.
            var result = new JsonCombinationContainer();
            var length = Math.Min(l.HasValue ? l.Value : DefaultLength, MaxLength);
            var symbols = sym.IsTruthy(DefaultSymbols);

            var charCount = symbols ? AllCharacters.Length : AlphanumericCharacters.Length;
            result.combinations = Math.Pow(charCount, length);
            result.rating = _RatingService.Rate(result.combinations);
            return Json(result);
        }

        private IEnumerable<string> SelectPasswords(IRandomNumberGenerator random, int length, int count, bool includeSymbols)
        {
            length = Math.Min(length, MaxLength);
            count = Math.Min(count, MaxCount);
            if (count <= 0 || length <= 0)
                yield break;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var chars = includeSymbols ? AllCharacters : AlphanumericCharacters;
            var sb = new StringBuilder();

            for (int c = 0; c < count; c++)
            {
                for (int l = 0; l < length; l++)
                    sb.Append(chars[random.GetRandomInt32(chars.Length)]);

                var result = sb.ToString();
                yield return result;
                sb.Clear();
            }
            sw.Stop();

            PostSelectionAction("AlphaNumeric", count, sw.Elapsed, random);
        }
    }
}

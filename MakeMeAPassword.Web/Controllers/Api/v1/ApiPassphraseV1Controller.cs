﻿using System;
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
    public class ApiPassphraseV1Controller : Controller
    {
        public readonly static int MaxWords = 16;
        public readonly static int MaxCount = 50;
        public readonly static int DefaultMinChars = 1;
        public readonly static int DefaultMaxChars = Int32.MaxValue;
        public readonly static int DefaultWords = 4;
        public readonly static int DefaultCount = 1;
        public readonly static int MaxAttemptsPerCount = 100;


        public readonly static Lazy<List<string>> Dictionary;

        static ApiPassphraseV1Controller()
        {
            Dictionary = new Lazy<List<string>>(() =>
                {
                    // Dictionary derived from the most common published English words after 1950 from Google ngrams: http://storage.googleapis.com/books/ngrams/books/datasetsv2.html
                    var fi = new System.IO.FileInfo(System.Web.HttpContext.Current.Request.MapPath("~/content/data/DictionaryEnglish.txt"));
                    return new List<string>(fi.YieldLines().Select(x => x.Trim()));
                }, true);
        }


        // GET: /api/v1/passphrase/plain
        public String Plain(int? pc, int? wc, string sp, int? minCh, int? maxCh)
        {
            var phrases = SelectPhrases(wc.HasValue ? wc.Value : DefaultWords,
                                        pc.HasValue ? pc.Value : DefaultCount,
                                        sp.IsTruthy(true),
                                        minCh.HasValue ? minCh.Value : DefaultMinChars,
                                        maxCh.HasValue ? maxCh.Value : DefaultMaxChars);
            return String.Join(Environment.NewLine, phrases);
        }

        // GET: /api/v1/passphrase/json
        public ActionResult Json(int? pc, int? wc, string sp, int? minCh, int? maxCh)
        {
            // Return as Json array.
            var phrases = SelectPhrases(wc.HasValue ? wc.Value : DefaultWords,
                                        pc.HasValue ? pc.Value : DefaultCount,
                                        sp.IsTruthy(true),
                                        minCh.HasValue ? minCh.Value : DefaultMinChars,
                                        maxCh.HasValue ? maxCh.Value : DefaultMaxChars);
            return new JsonNetResult(new JsonPasswordContainer() { pws = phrases.ToList() });
        }

        // GET: /api/v1/passphrase/xml
        public ActionResult Xml(int? pc, int? wc, string sp, int? minCh, int? maxCh)
        {
            // Return as XML.
            var phrases = SelectPhrases(wc.HasValue ? wc.Value : DefaultWords,
                                        pc.HasValue ? pc.Value : DefaultCount,
                                        sp.IsTruthy(true),
                                        minCh.HasValue ? minCh.Value : DefaultMinChars,
                                        maxCh.HasValue ? maxCh.Value : DefaultMaxChars);
            return new XmlResult(phrases.ToList());
        }

        // GET: /api/v1/passphrase/combinations
#if !DEBUG
        [OutputCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public ActionResult Combinations(int? wc)
        {
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), 1);

            // Return information about the number of combinations as a JSON object.
            var result = new JsonCombinationContainer();
            var wordCount = Math.Min(wc.HasValue ? wc.Value : DefaultWords, MaxWords);
            var dict = Dictionary.Value;

            result.combinations = Math.Pow(dict.Count, wordCount);
            return new JsonNetResult(result);
        }

        private IEnumerable<string> SelectPhrases(int wordCount, int phraseCount, bool spaces, int minChars, int maxChars)
        {
            if (minChars > maxChars)
                yield break;

            phraseCount = Math.Min(phraseCount, MaxCount);
            wordCount = Math.Min(wordCount, MaxWords);

            var random = RandomService.GetForCurrentThread();
            var sb = new StringBuilder();
            var dict = Dictionary.Value;
            int attempts = 0;

            random.BeginStats(this.GetType());
            try
            {
                for (int c = 0; c < phraseCount; c++)
                {
                    do 
                    {
                        // Generate a phrase.
                        for (int l = 0; l < wordCount; l++)
                        {
                            sb.Append(dict[random.Next(dict.Count)]);
                            if (spaces)
                                sb.Append(' ');
                        }
                        if (spaces)
                            sb.Remove(sb.Length - 1, 1);
                        attempts++;

                    // Ensure the final phrase is within the min / max chars.
                    } while (attempts < MaxAttemptsPerCount && (sb.Length < minChars || sb.Length > maxChars));
                    if (attempts >= MaxAttemptsPerCount)
                    {
                        sb.Clear();
                        sb.Append("A passphrase could not be found matching your minimum and maximum length requirements");
                    }
                    

                    // Yield the phrase and reset state.
                    var result = sb.ToString();
                    random.IncrementStats(result);
                    yield return result;
                    sb.Clear();
                    attempts = 0;
                }
            } finally {
                random.EndStats();
            }
            IpThrottlerService.IncrementUsage(IPAddressHelpers.GetHostOrCacheIp(this.HttpContext.Request), phraseCount);
        }
    }
}
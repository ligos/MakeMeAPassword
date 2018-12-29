using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Models;

namespace MurrayGrant.MakeMeAPassword.Web.NetCore.Controllers
{
    // Cache static pages for 1 hour.
    [ResponseCache(Duration = 60 * 60, Location = ResponseCacheLocation.Any)]
    public class HomeController : Controller
    {
        [HttpGet("/")]
        public IActionResult Index()
        {
            ViewBag.CurrentNav = "Home";
            return View();
        }

        [HttpGet("/about")]
        public IActionResult About()
        {
            ViewBag.CurrentNav = "About";
            return View();
        }

        [HttpGet("/contact")]
        public IActionResult Contact()
        {
            ViewBag.CurrentNav = "Contact";
            return View();
        }

        [HttpGet("/technical")]
        public IActionResult Technical()
        {
            ViewBag.CurrentNav = "Technical";
            return View();
        }

        [HttpGet("/donate")]
        public IActionResult Donate()
        {
            ViewBag.CurrentNav = "Donate";
            return View();
        }
        [HttpGet("/donatethanks")]
        public IActionResult DonateThanks()
        {
            ViewBag.CurrentNav = "Donate";
            return View();
        }

        [HttpGet("/faq")]
        public IActionResult Faq()
        {
            ViewBag.CurrentNav = "Faq";
            return View();
        }

        [HttpGet("/api")]
        public IActionResult Api()
        {
            ViewData["SiteAbsoluteUrl"] = "https://" + Request.Host;
            ViewData["PassphraseDictionaryCount"] = 0;  // MurrayGrant.PasswordGenerator.Web.Controllers.Api.v1.ApiPassphraseV1Controller.Dictionary.Value.Count;
            return View();
        }

        [HttpGet("/generate")]
        public IActionResult Generate()
        {
            ViewBag.CurrentNav = "GenerateIndex";
            return View();
        }

        [HttpGet("/generate/readablepassphrase")]
        public IActionResult GenerateReadablePassphrase()
        {
            ViewBag.CurrentNav = "ReadablePassphrase";
            return View("ReadablePassphrase");
        }

        [HttpGet("/generate/passphrase")]
        [HttpGet("/generate/dictionary")]
        public IActionResult GeneratePassphrase()
        {
            ViewBag.CurrentNav = "DictionaryPassphrase";
            return View("Passphrase");
        }

        [HttpGet("/generate/hex")]
        public IActionResult GenerateHex()
        {
            ViewBag.CurrentNav = "Hex";
            return View("Hex");
        }

        [HttpGet("/generate/unicode")]
        public IActionResult GenerateUnicode()
        {
            ViewBag.CurrentNav = "Unicode";
            return View("Unicode");
        }

        [HttpGet("/generate/alphanumeric")]
        [HttpGet("/generate/lettersandnumbers")]
        public IActionResult GenerateAlphaNumeric()
        {
            ViewBag.CurrentNav = "AlphaNumeric";
            return View("AlphaNumeric");
        }

        [HttpGet("/generate/pin")]
        public IActionResult GeneratePin()
        {
            ViewBag.CurrentNav = "PIN";
            return View("PIN");
        }

        [HttpGet("/generate/pronouncable")]
        public IActionResult GeneratePronouncable()
        {
            ViewBag.CurrentNav = "Pronouncable";
            return View("Pronouncable");
        }

        [HttpGet("/error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

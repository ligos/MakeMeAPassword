// Copyright 2022 Murray Grant
//
//    Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MurrayGrant.MakeMeAPassword.Web.Net60.Models;

namespace MurrayGrant.MakeMeAPassword.Web.Net60.Controllers;

// Cache static pages for 1 hour.
[ResponseCache(Duration = 60 * 60, Location = ResponseCacheLocation.Any)]
public class HomeController : Controller
{
    [HttpGet("/")]
    [HttpHead("/")]     // Uptime Robot likes to make HEAD requests to check if the server is happy.
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

    [HttpGet("/generate/pattern")]
    public IActionResult GeneratePattern()
    {
        ViewBag.CurrentNav = "Pattern";
        return View("Pattern");
    }

    [HttpGet("/generate/pronouncable")]
    [HttpGet("/generate/pronounceable")]
    public IActionResult GeneratePronounceable()
    {
        ViewBag.CurrentNav = "Pronounceable";
        return View("Pronounceable");
    }

    [HttpGet("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

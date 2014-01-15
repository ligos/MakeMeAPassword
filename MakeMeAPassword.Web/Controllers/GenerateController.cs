using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MurrayGrant.PasswordGenerator.Web.Controllers
{
//    [OutputCache(Duration=10, Location = System.Web.UI.OutputCacheLocation.Any, CacheProfile="")]
    public class GenerateController : Controller
    {
        //
        // GET: /Generate/
        public ActionResult Index()
        {
            ViewBag.CurrentNav = "GenerateIndex";
            return View();
        }

        public ActionResult ReadablePassphrase()
        {
            ViewBag.CurrentNav = "ReadablePassphrase";
            return View();
        }

        public ActionResult Passphrase()
        {
            ViewBag.CurrentNav = "DictionaryPassphrase";
            return View("Passphrase");
        }
        public ActionResult Dictionary()
        {
            return Passphrase();
        }

        public ActionResult Hex()
        {
            ViewBag.CurrentNav = "Hex";
            return View();
        }

        public ActionResult Unicode()
        {
            ViewBag.CurrentNav = "Unicode";
            return View();
        }

        public ActionResult AlphaNumeric()
        {
            ViewBag.CurrentNav = "AlphaNumeric";
            return View("AlphaNumeric");
        }
        public ActionResult LettersAndNumbers()
        {
            return AlphaNumeric();
        }

        public ActionResult Pin()
        {
            ViewBag.CurrentNav = "PIN";
            return View();
        }

        public ActionResult Pronouncable()
        {
            ViewBag.CurrentNav = "Pronouncable";
            return View();
        }
    }
}

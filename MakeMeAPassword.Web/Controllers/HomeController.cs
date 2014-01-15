using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using MurrayGrant.PasswordGenerator.Web.Helpers;
using System.Dynamic;
using Newtonsoft.Json;
using MurrayGrant.PasswordGenerator.Web.ActionResults;
using MurrayGrant.PasswordGenerator.Web.Services;

namespace MurrayGrant.PasswordGenerator.Web.Controllers
{
    // Cache static pages for 1 day.
#if !DEBUG
    [OutputCache(Duration = 60 * 60 * 24, Location = System.Web.UI.OutputCacheLocation.Any)]
#endif
    public class HomeController : Controller
    {
        // GET: /Home/
        public ActionResult Index()
        {
            ViewBag.CurrentNav = "Home";
            return View();
        }

        public ActionResult About()
        {
            ViewBag.CurrentNav = "About";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.CurrentNav = "Contact";
            return View();
        }

        public ActionResult Technical()
        {
            ViewBag.CurrentNav = "Technical";
            return View();
        }

        public ActionResult Donate()
        {
            // Paypal hasn't been setup yet: so return 404.
            return new HttpStatusCodeResult(System.Net.HttpStatusCode.NotFound);
            ViewBag.CurrentNav = "Donate";
            return View();
        }

        public ActionResult Faq()
        {
            ViewBag.CurrentNav = "Faq";
            return View();
        }
    }
}

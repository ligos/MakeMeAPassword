using System.Web;
using System.Web.Optimization;

namespace MurrayGrant.PasswordGenerator.Web
{
    public class BundleConfig
    {
        // For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/scripts/jquery").Include(
                        "~/content/Scripts/jquery.js",
                        "~/content/Scripts/jquery.validate.js"));

            bundles.Add(new ScriptBundle("~/bundles/scripts/bootstrap").Include(
                        "~/content/Scripts/bootstrap.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/scripts/modernizr").Include(
                        "~/content/Scripts/modernizr.js",
                        "~/content/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/bundles/css/bootstrap").Include(
                        "~/Content/css/bootstrap.css",
                        "~/Content/css/bootstrap-theme.css"));

            bundles.Add(new StyleBundle("~/bundles/css/fontawesome").Include(
                        "~/Content/css/font-awesome.css"));

            
            bundles.Add(new StyleBundle("~/bundles/css/site").Include("~/Content/css/site.css"));

            bundles.Add(new ScriptBundle("~/bundles/scripts/site").Include(
                        "~/content/Scripts/site-*"));




            
            bundles.Add(new StyleBundle("~/bundles/css/all").Include(
                        "~/Content/css/bootstrap.css",
                        "~/Content/css/bootstrap-theme.css",
                        "~/Content/css/font-awesome.css",
                        "~/Content/css/site.css"
                        ));
            
            bundles.Add(new ScriptBundle("~/bundles/scripts/all").Include(
                        "~/content/Scripts/jquery.js",
                        "~/content/Scripts/jquery.validate.js",
                        "~/content/Scripts/bootstrap.js",
                        "~/content/Scripts/site-*"
                        ));
        }
    }
}
using System.Web;
using System.Web.Optimization;

namespace MurrayGrant.PasswordGenerator.Web
{
    public class BundleConfig
    {
        // For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.UseCdn = true;

            bundles.Add(new ScriptBundle("~/bundles/scripts/jquery", "//code.jquery.com/jquery-1.10.2.min.js").Include(
                        "~/content/Scripts/jquery.js"));

            bundles.Add(new ScriptBundle("~/bundles/scripts/bootstrap", "//netdna.bootstrapcdn.com/bootstrap/3.0.3/js/bootstrap.min.js").Include(
                        "~/content/Scripts/bootstrap.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/scripts/modernizr").Include(
                        "~/content/Scripts/modernizr.js",
                        "~/content/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/bundles/css/bootstrap", "//netdna.bootstrapcdn.com/bootstrap/3.0.3/css/bootstrap.min.css").Include(
                        "~/Content/css/bootstrap.css"));
            bundles.Add(new StyleBundle("~/bundles/css/bootstrap-theme", "//netdna.bootstrapcdn.com/bootstrap/3.0.3/css/bootstrap-theme.min.css").Include(
                        "~/Content/css/bootstrap-theme.css"));

            bundles.Add(new StyleBundle("~/bundles/css/fontawesome", "//netdna.bootstrapcdn.com/font-awesome/4.0.3/css/font-awesome.css").Include(
                        "~/Content/css/font-awesome.css"));

            
            bundles.Add(new StyleBundle("~/bundles/css/site").Include("~/Content/css/site.css"));

            bundles.Add(new ScriptBundle("~/bundles/scripts/site").Include(
                        "~/content/Scripts/site-*"));
        }
    }
}
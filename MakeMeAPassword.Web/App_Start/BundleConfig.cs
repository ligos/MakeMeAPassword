// Copyright 2014 Murray Grant
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

            bundles.Add(new ScriptBundle("~/bundles/scripts/html5shiv", "//oss.maxcdn.com/libs/html5shiv/3.7.0/html5shiv.js").Include(
                        "~/content/Scripts/html5shiv.js"));
            bundles.Add(new ScriptBundle("~/bundles/scripts/respond", "//oss.maxcdn.com/libs/respond.js/1.3.0/respond.min.js").Include(
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
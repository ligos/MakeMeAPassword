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

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Web;
using System.Security.Cryptography;
using System.IO;
using System.Web.Mvc;
using Base32;

namespace MurrayGrant.PasswordGenerator.Web.Helpers
{
    public static class WebBundleHelper
    {
        public static bool EmitOptimised()
        {
            var result = true;
#if DEBUG
            result = false;
#endif
            return result;
        }

        private readonly static ConcurrentDictionary<string, string> _CacheBreakerForFile = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public static string CacheBreakerFor(string assetRef)
        {
            var filenameAndPath = assetRef;
            if (assetRef.Contains("~"))
                filenameAndPath = System.Web.Hosting.HostingEnvironment.MapPath(assetRef);

            return _CacheBreakerForFile.GetOrAdd(filenameAndPath, (fileName) =>
                {
                    // Calculate a hash of the file.
                    using (var hasher = new SHA256Managed())
                    using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, 16384))
                    {
                        var hash = hasher.ComputeHash(fs);
                        return Base32Encoder.Encode(hash).ToLower();
                    }
                });
        }
    }
}
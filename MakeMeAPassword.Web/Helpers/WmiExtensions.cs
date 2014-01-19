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
using System.Linq;
using System.Web;
using System.Management;

namespace MurrayGrant.PasswordGenerator.Web.Helpers
{
    public static class WmiExtensions
    {
        public static IEnumerable<IEnumerable<object>> GetNameValueAndType(this ManagementObjectSearcher wmiSource)
        {
            return GetNameValueAndType(wmiSource, Enumerable.Empty<string>());
        }
        public static IEnumerable<IEnumerable<object>> GetNameValueAndType(this ManagementObjectSearcher wmiSource, IEnumerable<string> orderOfResults)
        {
            return wmiSource.Get().Cast<ManagementObject>()
                    .Select(x => x.Properties.Cast<PropertyData>()
                        .Select(pd => new { pd.Name, pd.Value, pd.Type, OrderBy = orderOfResults.IndexOfOrMinusOne(pd.Name) })
                        .OrderBy(y => y.OrderBy)
                        .Select(y => (object)new { y.Name, y.Value, y.Type })
                    );
        }

        public static IEnumerable<IDictionary<string, object>> GetNameAndValue(this ManagementObjectSearcher wmiSource)
        {
            return GetNameAndValue(wmiSource, Enumerable.Empty<string>());
        }
        public static IEnumerable<IDictionary<string, object>> GetNameAndValue(this ManagementObjectSearcher wmiSource, IEnumerable<string> orderOfResults)
        {
            return wmiSource.Get().Cast<ManagementObject>()
                    .Select(x => x.Properties.Cast<PropertyData>()
                        .Select(pd => new { pd.Name, pd.Value, pd.Type, OrderBy = orderOfResults.IndexOfOrMinusOne(pd.Name) })
                        .OrderBy(y => y.OrderBy)
                        .ToDictionary(y => y.Name, y => y.Value)
                    );
        }

        public static int IndexOfOrMinusOne<T>(this IEnumerable<T> collection, T item)
        {
            return IndexOfOrMinusOne(collection, item, EqualityComparer<T>.Default);
        }
        public static int IndexOfOrMinusOne<T>(this IEnumerable<T> collection, T item, IEqualityComparer<T> comparer)
        {
            int result = 0;
            foreach (var i in collection)
            {
                if (comparer.Equals(item, i))
                    return result;
                result++;
            }
            return -1;
        }
    }
}
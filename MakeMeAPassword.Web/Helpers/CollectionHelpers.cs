using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MurrayGrant.PasswordGenerator.Web.Helpers
{
    public static class CollectionHelpers
    {
        public static IEnumerable<T> Randomise<T>(this IEnumerable<T> items)
        {
            return Randomise(items, new Random());
        }
        public static IEnumerable<T> Randomise<T>(this IEnumerable<T> items, Random rand)
        {
            return items.OrderBy(x => rand.Next());
        }
    }
}
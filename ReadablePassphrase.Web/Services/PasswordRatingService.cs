using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MurrayGrant.PasswordGenerator.Web.Services
{
    /// <summary>
    /// Rates passwords based on their number of combinations.
    /// </summary>
    public static class PasswordRatingService
    {
        private static double UnusableBoundary = Math.Pow(2, 32);
        private static double InadequateBoundary = Math.Pow(2, 42);
        private static double PassableBoundary = Math.Pow(2, 54);
        private static double AdequateBoundary = Math.Pow(2, 66);
        private static double StrongBoundary = Math.Pow(2, 80);
        private static double FantasticBoundary = Math.Pow(2, 112);
        private static double UnbreakableBoundary = Math.Pow(2, 146);
        private static double OverkillBoundary = Double.PositiveInfinity;

        public static int Rate(double combinations)
        {
            // This is a static lookup table based on offline cracking time and orders of magnitude.
            // Roughly, this is based on 1G hash attempts / sec.

            if (Double.IsNaN(combinations) || Double.IsNegativeInfinity(combinations) || combinations < UnusableBoundary)
                // Unusable.
                return 0;
            else if (combinations >= UnusableBoundary && combinations < InadequateBoundary)
                // Inadequate.
                return 1;
            else if (combinations >= InadequateBoundary && combinations < PassableBoundary)
                // Passable.
                return 2;
            else if (combinations >= PassableBoundary && combinations < AdequateBoundary)
                // Adequate.
                return 3;
            else if (combinations >= AdequateBoundary && combinations < StrongBoundary)
                // Strong.
                return 4;
            else if (combinations >= StrongBoundary && combinations < FantasticBoundary)
                // Fantastic.
                return 5;
            else if (combinations >= FantasticBoundary && combinations < UnbreakableBoundary)
                // Unbreakable.
                return 6;
            else if (combinations >= UnbreakableBoundary && combinations <= OverkillBoundary)
                // Overkill.
                return 7;
            else
                // Unexpected case.
                return 0;
        }

        public static int RatePin(double combinations)
        {
            // PINs are different. Because of the small keyspace, we have to assume much lower attack rates.
            if (Double.IsNaN(combinations) || Double.IsNegativeInfinity(combinations) || combinations < 1000.0)
                // Unusable.
                return 0;
            else if (combinations >= 1000.0 && combinations < 9800.0)
                // Inadequate.
                return 1;
            else if (combinations >= 9800.0 && combinations < 99900.0)
                // Passable (4 digits).
                return 2;
            else if (combinations >= 99900.0 && combinations < 999000.0)
                // Adequate (5 digits).
                return 3;
            else if (combinations >= 999000.0 && combinations < 99900000.0)
                // Strong (6-7 digits).
                return 4;
            else if (combinations >= 99900000.0 && combinations < 9990000000.0)
                // Fantastic (8-9 digits).
                return 5;
            else if (combinations >= 9990000000.0 && combinations < 9990000000000.0)
                // Unbreakable (10-12 digits).
                return 6;
            else if (combinations >= 9990000000000.0 && combinations < Double.PositiveInfinity)
                // Overkill (13+ numbers).
                return 7;
            else
                // Unexpected case.
                return 0;
        }
    }
}
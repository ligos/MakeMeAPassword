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

namespace MurrayGrant.MakeMeAPassword.Web.Net60.Services
{
    /// <summary>
    /// Rates passwords based on their number of combinations.
    /// </summary>
    public class PasswordRatingService
    {
        private static readonly double UnusableBoundary = Math.Pow(2, 32);
        private static readonly double InadequateBoundary = Math.Pow(2, 42);
        private static readonly double PassableBoundary = Math.Pow(2, 54);
        private static readonly double AdequateBoundary = Math.Pow(2, 66);
        private static readonly double StrongBoundary = Math.Pow(2, 80);
        private static readonly double FantasticBoundary = Math.Pow(2, 112);
        private static readonly double UnbreakableBoundary = Math.Pow(2, 146);
        private static readonly double OverkillBoundary = Double.PositiveInfinity;

        public int Rate(double combinations)
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

        public int RatePin(double combinations)
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


        public int RatePattern(double combinations)
        {
            // Patterns are even worse than PINs.
            if (Double.IsNaN(combinations) || Double.IsNegativeInfinity(combinations) || combinations < 500.0)
                // Unusable.
                return 0;
            else if (combinations >= 500.0 && combinations < 1900.0)
                // Inadequate.
                return 1;
            else if (combinations >= 1900.0 && combinations < 14000.0)
                // Passable (5-6 points on a 3x3 grid).
                return 2;
            else if (combinations >= 14000.0 && combinations < 50000.0)
                // Adequate (7-8 points on a 3x3 grid).
                return 3;
            else if (combinations >= 50000.0 && combinations < 100000.0)
                // Strong (9 points on a 3x3 grid).
                return 4;
            else if (combinations >= 100000.0 && combinations < 10000000.0)
                // Fantastic (need a 4x4 grid to reach this).
                return 5;
            else if (combinations >= 10000000.0 && combinations < 500000000.0)
                // Unbreakable (need a 4x4 grid to reach this).
                return 6;
            else if (combinations >= 500000000.0 && combinations < Double.PositiveInfinity)
                // Overkill (need a 4x4 grid to reach this).
                return 7;
            else
                // Unexpected case.
                return 0;
        }
    }
}
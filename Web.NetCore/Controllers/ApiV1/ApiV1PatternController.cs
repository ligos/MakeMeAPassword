// Copyright 2019 Murray Grant
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
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Services;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Helpers;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Filters;
using MurrayGrant.MakeMeAPassword.Web.NetCore.Models.ApiV1;
using MurrayGrant.Terninger;
using MurrayGrant.Terninger.Random;

namespace MurrayGrant.MakeMeAPassword.Web.NetCore.Controllers.ApiV1
{
    
    public class ApiV1PatternController : ApiV1Controller
    {
        // Patterns are the grid of dots you must connect when unlocking your phone / tablet.
        // They are represented as a 1 dimensional array starting at the top left, 1 based.

        public readonly static int MaxGridSize = 8;     // Size of the grid N x N (eg: 3 x 3)
        public readonly static int MaxPoints = 64;      // Number of points to connect. May also be restricted by grid size.
        public readonly static int MaxCount = 50;
        public readonly static int DefaultGridSize = 3;
        public readonly static int DefaultPoints = 5;
        public readonly static int DefaultCount = 1;

        public ApiV1PatternController(PooledEntropyCprngGenerator terninger, PasswordRatingService ratingService, PasswordStatisticService statisticService, IpThrottlerService ipThrottler, DictionaryService dictionaryService)
            : base(terninger, ratingService, statisticService, ipThrottler, dictionaryService) { }

        [HttpGet("/api/v1/pattern/plain")]
        public async Task<IActionResult> Plain([FromQuery]int? gs, [FromQuery]int? ps, [FromQuery]int? c)
        {
            // Return as plain text string.
            using (var random = await _Terninger.CreateCypherBasedGeneratorAsync())
            {
                var patterns = SelectPatterns(random,
                                gs.HasValue ? gs.Value : DefaultGridSize,
                                ps.HasValue ? ps.Value : DefaultPoints,
                                c.HasValue ? c.Value : DefaultCount);
                return Plain(patterns.Select(p => String.Join(",", p.Select(x => x.ToString()))).ToList());
            }
        }

        [HttpGet("/api/v1/pattern/json")]
        public async Task<IActionResult> Json([FromQuery]int? gs, [FromQuery]int? ps, [FromQuery]int? c)
        {
            // Return as Json array.
            using (var random = await _Terninger.CreateCypherBasedGeneratorAsync())
            {
                var patterns = SelectPatterns(random,
                                gs.HasValue ? gs.Value : DefaultGridSize,
                                ps.HasValue ? ps.Value : DefaultPoints,
                                c.HasValue ? c.Value : DefaultCount);
                return Json(new JsonPasswordContainer() { pws = patterns.Select(p => String.Join(",", p.Select(x => x.ToString()))).ToList() });
            }
        }

        [HttpGet("/api/v1/pattern/xml")]
        public async Task<IActionResult> Xml([FromQuery]int? gs, [FromQuery]int? ps, [FromQuery]int? c)
        {
            // Return as XML.
            using (var random = await _Terninger.CreateCypherBasedGeneratorAsync())
            {
                var patterns = SelectPatterns(random,
                                gs.HasValue ? gs.Value : DefaultGridSize,
                                ps.HasValue ? ps.Value : DefaultPoints,
                                c.HasValue ? c.Value : DefaultCount);
                return Xml(patterns.Select(p => String.Join(",", p.Select(x => x.ToString()))).ToList());
            }
        }

        [HttpGet("/api/v1/pattern/combinations")]
#if !DEBUG
        [OutputCache(Duration = 60 * 60)]       // Cache for one hour.
#endif
        public ActionResult Combinations([FromQuery]int? gs, [FromQuery]int? ps)
        {
            IncrementUsage(1);

            // Return information about the number of combinations as a JSON object.
            var result = new JsonCombinationContainer();
            var gridSize = Math.Min(gs.HasValue ? gs.Value : DefaultGridSize, MaxGridSize);
            var gridPoints = gridSize * gridSize;
            var points = Math.Min(Math.Min(ps.HasValue ? ps.Value : DefaultPoints, MaxPoints), gridPoints);

            // Can't use the nPr permutations formula, because the dots must be adjacent.
            // We calculate the approximate number of options available at each choice, and multiple them together.
            // For the standard 3x3 grid, this starts at 4.4As the grid size increases, this tends toward around 7 possibilities after the starting spot is chosen.

            double combinations;
            if (gridSize == 1)
                combinations = 1;           // 1x1 grid is a bit silly, and trivial.
            else if (gridSize == 2)
                combinations = 4 * 3 * 2 * 1;       // 2x2 grid is very deterministic.
            else
            {
                // Larger grids choose a random starting spot, then adjacent points, which limit possibilities.
                // The following isn't perfect, but a linear approximation of the probabilities involved:
                //  First is always randomly chosen anywhere on the grid: possibilities = N²
                //  Next follows this pattern:
                //    4 corners with 3 possible adjacent squares = 4 * 3
                //    4 sides with 5 possible adjacent squares = (N-2) * 4 * 5
                //    Remainder in the middle have 8 adjacent squares = (N-2)² * 8
                //    Average possibilities = ((4*3) + ((N-2) * 4 * 5) + ((N-2)² * 8)) / N²
                //  After that, we assume a linear decrease in average possibilities (which probably isn't quite right, but close enough).
                //  We assume the last 2 are always 2, then 1.
                //  So the "gap" or "step" between each point = (Average Possibilities - 1) / (N² - 2)
                double gridLengthMinusCorners = gridSize - 2;
                double total2ndChoiceOptions = (4 * 3)     // Corners
                                             + (gridLengthMinusCorners * 4 * 5)        // Sides
                                             + (gridLengthMinusCorners * gridLengthMinusCorners * 8);     // Middle
                double avgOptions = total2ndChoiceOptions / gridPoints;         // Average number of options for the total grid points.
                double availableOptions = avgOptions - 1;                       // The last value we accumulate should be ~1.
                double avgDiffPerStep = availableOptions / (gridPoints - 2);    // This is the step we'll use when accumulating.

                combinations = gridPoints;      // First choice is random between all points on the grid.
                var nextStepOptions = avgOptions;       // Second choice is calculated above.
                for (int n = gridPoints-2; n > 0; n--)
                {
                    combinations = combinations * nextStepOptions;          // Accumulate combinations.
                    nextStepOptions = nextStepOptions - avgDiffPerStep;     // And reduce each next step by the average.
                }
            }
            result.combinations = combinations;
            result.rating = _RatingService.RatePin(result.combinations);
            return Json(result);
        }

        private IEnumerable<IEnumerable<int>> SelectPatterns(IRandomNumberGenerator random, int gridSize, int pointCount, int count)
        {
            gridSize = Math.Min(gridSize, MaxGridSize);
            count = Math.Min(count, MaxCount);
            var gridPoints = gridSize * gridSize;
            pointCount = Math.Min(pointCount, gridPoints);
            if (count <= 0 || gridSize <= 0 || pointCount <= 0)
                yield break;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            // On my Nexus 5X, connected dots must be adjacent, or already chosen.
            // Ie: if I choose the top left dot, and then the bottom right dot (even if I slide my finger around the screen so I don't touch any other dot),
            //     then it still connects the top left, middle and bottom right dots.
            // Only if a dot is already part of the pattern can it be "bypassed".

            for (int c = 0; c < count; c++)
            {
                if (gridSize == 1)
                    // If someone wants to use a 1x1 grid, well, it won't break!
                    yield return new int[] { 1 };
                else if (gridSize == 2)
                {
                    // Although a 2x2 grid works with the code below, we just choose points at random.
                    var result = Enumerable.Range(1, gridPoints).ToList();
                    result.ShuffleInPlace(random);
                    yield return result;
                }
                else
                {
                    // Normal sized grids (minimum 3x3).
                    var result = new List<int>(pointCount);
                    var grid = new bool[gridSize, gridSize];

                    // Choose any point on the grid and mark it as taken.
                    int x = random.GetRandomInt32(gridSize - 1);
                    int y = random.GetRandomInt32(gridSize - 1);
                    grid[y, x] = true;
                    result.Add((y * gridSize) + x + 1);

                    while (result.Count < pointCount)
                    {
                        // Find the distance to the nearest point.
                        var distance = DistanceToNearestUntakenPoint(grid, y, x);
                        if (distance <= -1)
                            break;

                        // Count available points at that distance.
                        var availablePoints = AvailablePointsAtDistance(grid, y, x, distance).ToList();
                        var countAtDistance = availablePoints.Count;
                        if (countAtDistance == 0)
                            break;      // Should be impossible, but you never know.

                        // Choose one at random.
                        var choice = countAtDistance <= 1 ? 0 : random.GetRandomInt32(countAtDistance - 1);
                        (y, x) = availablePoints[choice];
                        grid[y, x] = true;
                        result.Add((y * gridSize) + x + 1);
                    }

                    yield return result;
                }
            }
            sw.Stop();

            PostSelectionAction("Pattern", count, sw.Elapsed, random);
        }

        private static int DistanceToNearestUntakenPoint(bool[,] grid, int y, int x)
        {
            var gridSize = grid.GetUpperBound(0);
            for (int d = 1; d <= gridSize; d++)
            {
                // Check each direction to see if the position is a) on the grid and b) not taken, at distance d.
                // There are plenty of redundant checks, but for sake of symmetry I keep them in.
                // These go in a clockwise circle starting in the top left.
                // PERF: for large grids, this has poor cache locality.

                // 4 corners: top left, top right, bottom right, bottom left.
                if (   x - d >= 0 && x - d <= gridSize
                    && y - d >= 0 && y - d <= gridSize
                    && !grid[y - d, x - d])
                    return d;
                if (   x + d >= 0 && x + d <= gridSize
                    && y - d >= 0 && y - d <= gridSize
                    && !grid[y - d, x + d])
                    return d;
                if (   x + d >= 0 && x + d <= gridSize
                    && y + d >= 0 && y + d <= gridSize
                    && !grid[y + d, x + d])
                    return d;
                if (   x - d >= 0 && x - d <= gridSize
                    && y + d >= 0 && y + d <= gridSize
                    && !grid[y + d, x - d])
                    return d;

                // 4 sides: top, right, bottom, left;
                for (int i = -d + 1; i <= d - 1; i++)
                    if (   x + i >= 0 && x + i <= gridSize
                        && y - d >= 0 && y - d <= gridSize
                        && !grid[y - d, x + i])
                        return d;
                for (int i = -d + 1; i <= d - 1; i++)
                    if (   x + d >= 0 && x + d <= gridSize
                        && y + i >= 0 && y + i <= gridSize
                        && !grid[y + i, x + d])
                        return d;
                for (int i = -d + 1; i <= d - 1; i++)
                    if (   x + i >= 0 && x + i <= gridSize
                        && y + d >= 0 && y + d <= gridSize
                        && !grid[y + d, x + i])
                        return d;
                for (int i = -d + 1; i <= d - 1; i++)
                    if (   x - d >= 0 && x - d <= gridSize
                        && y + i >= 0 && y + i <= gridSize
                        && !grid[y + i, x - d])
                        return d;
            }

            return -1;  // No untaken points.
        }
        private static IEnumerable<(int y, int x)> AvailablePointsAtDistance(bool[,] grid, int y, int x, int distance)
        {
            var gridSize = grid.GetUpperBound(0);
            var d = distance;
            // Return each point at distance to see if the position is a) on the grid and b) not taken.
            // There are plenty of redundant checks, but for sake of symmetry I keep them in.
            // These go in a clockwise circle starting in the top left.
            // PERF: for large grids, this has poor cache locality.

            // 4 corners: top left, top right, bottom right, bottom left.
            if (   x - d >= 0 && x - d <= gridSize
                && y - d >= 0 && y - d <= gridSize
                &&      !grid[y - d, x - d])
                yield return (y - d, x - d);
            if (   x + d >= 0 && x + d <= gridSize
                && y - d >= 0 && y - d <= gridSize
                &&      !grid[y - d, x + d])
                yield return (y - d, x + d);
            if (   x + d >= 0 && x + d <= gridSize
                && y + d >= 0 && y + d <= gridSize
                &&      !grid[y + d, x + d])
                yield return (y + d, x + d);
            if (   x - d >= 0 && x - d <= gridSize
                && y + d >= 0 && y + d <= gridSize
                &&      !grid[y + d, x - d])
                yield return (y + d, x - d);

            // 4 sides: top, right, bottom, left;
            for (int i = -d + 1; i <= d - 1; i++)
                if (   x + i >= 0 && x + i <= gridSize
                    && y - d >= 0 && y - d <= gridSize
                    &&      !grid[y - d, x + i])
                    yield return (y - d, x + i);
            for (int i = -d + 1; i <= d - 1; i++)
                if (   x + d >= 0 && x + d <= gridSize
                    && y + i >= 0 && y + i <= gridSize
                    &&      !grid[y + i, x + d])
                    yield return (y + i, x + d);
            for (int i = -d + 1; i <= d - 1; i++)
                if (   x + i >= 0 && x + i <= gridSize
                    && y + d >= 0 && y + d <= gridSize
                    &&      !grid[y + d, x + i])
                    yield return (y + d, x + i);
            for (int i = -d + 1; i <= d - 1; i++)
                if (   x - d >= 0 && x - d <= gridSize
                    && y + i >= 0 && y + i <= gridSize
                    &&      !grid[y + i, x - d])
                    yield return (y + i, x - d);
        }
    }
}

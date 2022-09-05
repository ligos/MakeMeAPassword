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
    /// Logs statistics about entropy for passwords.
    /// </summary>
    public class PasswordStatisticService
    {
        private static readonly NLog.Logger StatsLogger = NLog.LogManager.GetLogger("MurrayGrant.PasswordGenerator.PasswordStats");

        // This uses the new structured logging support in NLog 4.5+ to log to a CSV.
        public void LogPasswordStat(string name, int count, TimeSpan duration, int randomBytesConsumed, System.Net.Sockets.AddressFamily addressFamily)
            => StatsLogger.Info("{Name} {Count} {RandomBytesConsumed} {Duration:N3} {RandomBytesConsumedEa} {DurationEa:N4} {LocalOffset} {AddressFamily}",
                name, count, randomBytesConsumed, duration.TotalMilliseconds, randomBytesConsumed == 0 ? 0 : (double)randomBytesConsumed / (double)count, duration.TotalMilliseconds / count, (DateTimeOffset.Now.Offset >= TimeSpan.Zero ? "+" : "-") + DateTimeOffset.Now.Offset.ToString("hh\\:mm"), addressFamily
            );
    }
}
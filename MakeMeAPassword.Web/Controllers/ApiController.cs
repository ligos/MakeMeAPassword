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
using System.Web.Mvc;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Net.NetworkInformation;
using MurrayGrant.PasswordGenerator.Web.ActionResults;
using System.Dynamic;
using MurrayGrant.PasswordGenerator.Web.Services;
using MurrayGrant.PasswordGenerator.Web.Helpers;
using System.Text;
using System.Management;
using Exceptionless;

namespace MurrayGrant.PasswordGenerator.Web.Controllers
{
    public class ApiController : Controller
    {
        //
        // GET: /Api/
#if !DEBUG
        [OutputCache(Duration = 60 * 60 * 24, Location = System.Web.UI.OutputCacheLocation.Any)]
#endif
        public ActionResult Index()
        {
            ViewBag.PassphraseDictionaryCount = MurrayGrant.PasswordGenerator.Web.Controllers.Api.v1.ApiPassphraseV1Controller.Dictionary.Value.Count;
            return View();
        }


        private static readonly byte[] _SecretHash = new byte[] { 0x14, 0x60, 0xf1, 0x5b, 0x94, 0xf5, 0x7f, 0xe6, 0x1b, 0x6e, 0xa3, 0x23, 0x75, 0xe5, 0x44, 0x34, 0x3c, 0x89, 0x98, 0x27, 0x2c, 0xcc, 0x8d, 0x77, 0x7e, 0x73, 0x63, 0x83, 0x81, 0x07, 0x52, 0xcf, 0xa4, 0xb3, 0xef, 0x05, 0xbe, 0xfd, 0x21, 0x30, 0x37, 0x0b, 0x45, 0x5f, 0x35, 0x2d, 0x59, 0x76, 0x34, 0x10, 0xfe, 0x6c, 0x16, 0x4f, 0x67, 0x20, 0xc1, 0x01, 0x37, 0x4e, 0xfd, 0x94, 0x89, 0xda };

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult Diagnostics(string s, string bm)
        {
            var secretResult = this.AssertSecret(s);
            if (secretResult != null)
                return secretResult;

            // Load a whole bunch of stuff up into an expando and return it as Json.
            dynamic result = new ExpandoObject();
            result.ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

            var p = Process.GetCurrentProcess();
            result.ProcessId = p.Id;
            result.ProcessHandleCount = p.HandleCount;
            result.ProcessUserProcessorTime = p.UserProcessorTime.Ticks;
            result.ProcessPrivilegedProcessorTime = p.PrivilegedProcessorTime.Ticks;
            result.ProcessStartTime = p.StartTime.Ticks;
            result.ProcessPagedMemorySize64 = p.PagedMemorySize64;
            result.ProcessPeakPagedMemorySize64 = p.PeakPagedMemorySize64;
            result.ProcessPeakVirtualMemorySize64 = p.PeakVirtualMemorySize64;
            result.ProcessPeakWorkingSet64 = p.PeakWorkingSet64;
            result.ProcessPrivateMemorySize64 = p.PrivateMemorySize64;
            result.ProcessWorkingSet64 = p.WorkingSet64;
            result.Is64BitProcess = Environment.Is64BitProcess;

            var rootDrive = new DriveInfo(new DirectoryInfo(Environment.SystemDirectory).Root.Name);
            result.RootDriveCapacity = rootDrive.TotalSize;
            result.RootDriveFreeSpace = rootDrive.TotalFreeSpace;

            // Some WMI goodness!
            try { result.SystemInfo = GetWmiObject("Win32_ComputerSystem"); } 
            catch (ManagementException ex) { result.SystemInfo = ex.GetType().FullName + ": " + ex.Message; }
            try { result.OsInfo = GetWmiObject("Win32_OperatingSystem"); }
            catch (ManagementException ex) { result.OsInfo = ex.GetType().FullName + ": " + ex.Message; }
            try { result.CpuInfo = GetWmiObject("Win32_Processor"); }
            catch (ManagementException ex) { result.CpuInfo = ex.GetType().FullName + ": " + ex.Message; }
            try { result.RamInfo = GetWmiObject("Win32_PhysicalMemory"); }
            catch (ManagementException ex) { result.RamInfo = ex.GetType().FullName + ": " + ex.Message; }
            try { result.NumaInfo = GetWmiObject("Win32_MemoryArray"); }
            catch (ManagementException ex) { result.NumaInfo = ex.GetType().FullName + ": " + ex.Message; }
            try { result.DiskInfo = GetWmiObject("Win32_DiskDrive"); }
            catch (ManagementException ex) { result.DiskInfo = ex.GetType().FullName + ": " + ex.Message; }
            try { result.DiskVolumeInfo = GetWmiObject("Win32_Volume"); }
            catch (ManagementException ex) { result.DiskVolumeInfo = ex.GetType().FullName + ": " + ex.Message; }
            try { result.LogicalDiskInfo = GetWmiObject("Win32_LogicalDisk"); }
            catch (ManagementException ex) { result.LogicalDiskInfo = ex.GetType().FullName + ": " + ex.Message; }
            try { result.NetworkAdapterInfo = GetWmiObject("Win32_NetworkAdapter", "PhysicalAdapter=True"); }
            catch (ManagementException ex) { result.NetworkAdapterInfo = ex.GetType().FullName + ": " + ex.Message; }
            try { result.NetworkInfo = GetWmiObject("Win32_NetworkAdapterConfiguration", "IPEnabled=True OR IPXEnabled=True OR MACAddress IS NOT NULL"); }
            catch (ManagementException ex) { result.NetworkInfo = ex.GetType().FullName + ": " + ex.Message; }

            // A bunch of random bytes.
            var random = RandomService.GetForCurrentThread();
            var bytes = random.GetNextBytes(128);
            result.RandomBytes = bytes.ToHexString();

            // Checking compiler directives.
#if DEBUG
            result.build = "DEBUG";
#else
            result.build = "RELEASE";
#endif
#if NOHTTPS
            result.nohttps = true;
#else
            result.nohttps = false;
#endif

            if (bm.IsTruthy())
            {
                // RandomService benchmark.
                var sw = Stopwatch.StartNew();
                long l = 0L;
                for (int i = 0; i < 1000000; i++)
                    l += random.Next();
                sw.Stop();
                result.MillionRandomsSeconds = sw.Elapsed.TotalSeconds;
                result.MillionRandomsSum = l;
                result.RandomsPerSecond = 1000000 / sw.Elapsed.TotalSeconds;

                // Compare to csp.
                var csp = new RNGCryptoServiceProvider();
                var buf = new byte[4];
                csp.GetBytes(buf);
                csp.GetBytes(buf);
                csp.GetBytes(buf);
                sw.Restart();
                l = 0L;
                for (int i = 0; i < 1000000; i++)
                {
                    csp.GetBytes(buf);
                    l += BitConverter.ToInt32(buf, 0);
                }
                sw.Stop();
                result.MillionCSPRandomsSeconds = sw.Elapsed.TotalSeconds;
                result.MillionCSPRandomsSum = l;
                result.CSPRandomsPerSecond = 1000000 / sw.Elapsed.TotalSeconds;
            }

            result.SeedsInReserve = RandomSeedService.Singleton.SeedsInReserve;

            result.NetworkBytes = NetworkInterface.GetAllNetworkInterfaces().Select(x => x.GetIPStatistics()).Sum(x => x.BytesReceived + x.BytesSent);

            return new JsonNetResult(result);
        }

        private IEnumerable<IDictionary<string, object>> GetWmiObject(string wmiClass)
        {
            return GetWmiObject(wmiClass, Enumerable.Empty<string>(), "");
        }
        private IEnumerable<IDictionary<string, object>> GetWmiObject(string wmiClass, IEnumerable<string> properties)
        {
            return GetWmiObject(wmiClass, properties, "");
        }
        private IEnumerable<IDictionary<string, object>> GetWmiObject(string wmiClass, string whereClause)
        {
            return GetWmiObject(wmiClass, Enumerable.Empty<string>(), whereClause);
        }
        private IEnumerable<IDictionary<string, object>> GetWmiObject(string wmiClass, IEnumerable<string> properties, string whereClause)
        {
            var selectFields = properties == null || !properties.Any() || properties.Contains("*") ? "*" : String.Join(",", properties.ToArray());
            var query = "SELECT " + selectFields + " FROM " + wmiClass;
            if (!String.IsNullOrWhiteSpace(whereClause))
                query += " WHERE " + whereClause;

            using (var result = new ManagementObjectSearcher(query))
            {
                return result.GetNameAndValue(properties).ToList();
            }
        }


        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult Stats(string s)
        {
            var secretResult = this.AssertSecret(s);
            if (secretResult != null)
                return secretResult;

            var utcnow = DateTime.UtcNow;
            dynamic result = new ExpandoObject();
            result.WorkerStarted = System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
            result.WorkingRunningFor = utcnow.Subtract(result.WorkerStarted);
            result.SeedsInReserve = RandomSeedService.Singleton.SeedsInReserve;
            result.SeedsGenerated = RandomSeedService.Singleton.TotalSeedsGenerated;
            result.TotalUsageUnits = IpThrottlerService.TotalUsage;
            result.LastSeedGenerationStats = RandomSeedService.Singleton.LastSeedGenerationStats;

            var stats = RandomService.GetStats();

            // Stats by thread, more for interest's sake.
            result.StatsByThread = stats
                .GroupBy(x => x.ThreadId)
                .Select(x => new
                {
                    ThreadId = x.Key,
                    InitTime = x.Max(y => y.InitTime),
                    LastUsedUtc = x.Max(y => y.LastUsedUtc),
                    TimeSinceLastUsed = utcnow.Subtract(x.Max(y => y.LastUsedUtc)),
                    BytesConsumed = x.Sum(y => y.RandomBytesConsumed),
                    TotalPasswords = x.Sum(y => y.PasswordsGenerated),
                    AverageBytesPerPassword = (double)x.Sum(y => y.RandomBytesConsumed) / (double)x.Sum(y => y.PasswordsGenerated),
                })
                .ToDictionary(x => x.ThreadId);
            result.TotalRandomBytesConsumed = stats.Sum(x => x.RandomBytesConsumed);

            // Stats by password type.
            result.StatsByStyle = stats
                .GroupBy(x => x.Style)
                .Select(x => new
                {
                    Style = x.Key,
                    PasswordsGenerated = x.Sum(y => y.PasswordsGenerated),
                    TotalCharacters = x.Sum(y => y.PasswordCharactersGenerated),
                    TotalUtf8Bytes = x.Sum(y => y.PasswordUtf8BytesGenerated),
                    TotalRandomBytesConsumed = x.Sum(y => y.RandomBytesConsumed),
                    AverageCharactersPerPassword = (double)x.Sum(y => y.PasswordCharactersGenerated) / (double)x.Sum(y => y.PasswordsGenerated),
                    AverageUtf8BytesPerPassword = (double)x.Sum(y => y.PasswordUtf8BytesGenerated) / (double)x.Sum(y => y.PasswordsGenerated),
                    AverageRandomBytesPerPassword = (double)x.Sum(y => y.RandomBytesConsumed) / (double)x.Sum(y => y.PasswordsGenerated),
                })
                .ToDictionary(x => x.Style);

            result.TotalPasswordsGenerated = stats.Sum(x => x.PasswordsGenerated);
            result.TotalCharactersGenerated = stats.Sum(x => x.PasswordCharactersGenerated);
            result.TotalUtf8BytesGenerated = stats.Sum(x => x.PasswordUtf8BytesGenerated);
            result.AverageRandomBytesPerPassword = (double)stats.Sum(x => x.RandomBytesConsumed) / (double)stats.Sum(x => x.PasswordsGenerated);
            result.AverageCharactersPerPassword = (double)stats.Sum(x => x.PasswordCharactersGenerated) / (double)stats.Sum(x => x.PasswordsGenerated);
            result.AverageUtf8BytesPerPassword = (double)stats.Sum(x => x.PasswordUtf8BytesGenerated) / (double)stats.Sum(x => x.PasswordsGenerated);

            return new JsonNetResult(result);
        }

        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult RandomBytes(string s, int byteCount)
        {
            // This is for getting chunks of randomness to test the random number generator.
            var secretResult = this.AssertSecret(s);
            if (secretResult != null)
                return secretResult;

            var random = RandomService.GetForCurrentThread();
            var ms = new MemoryStream();
            random.BeginStats(typeof(Api.v1.ApiHexV1Controller));
            try {
                while (ms.Length < byteCount)
                    // You can only get 1k bytes at a time from the random generator.
                    ms.WriteBytes(random.GetNextBytes((int)Math.Min(1024, Math.Abs(byteCount - ms.Length))));
            } finally {
                random.EndStats();
            }
            ms.Position = 0L;

            return File(ms, "application/octet-stream", "random.bin");
        }

        public ActionResult TestException(string s)
        {
            var secretResult = this.AssertSecret(s);
            if (secretResult != null)
                return secretResult;

            try
            {
                throw new Exception("Test exception");
            }
            catch (Exception ex)
            {
#if !DEBUG
                ex.ToExceptionless().AddTags("Test").Submit();
#endif
                throw;
            }
        }

        private ActionResult AssertSecret(string s)
        {
            // s is a secret. It must match the hash above.
            // And for those reading this code on BitBucket, the secret is randomly generated gibberish. Good luck guessing it.
            if (s == null)
                s = "";
            var hash = new SHA512Managed().ComputeHash(Encoding.UTF8.GetBytes(s));
            if (!hash.SlowEquals(_SecretHash))
                return new HttpNotFoundResult();
            
            return null;
        }
    }
}

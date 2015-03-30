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
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Security.Cryptography;
using System.Net;
using System.Net.Mail;
using System.IO;
using MurrayGrant.PasswordGenerator.Web.Helpers;
using Exceptionless;
using Newtonsoft.Json;
using System.Text;

namespace MurrayGrant.PasswordGenerator.Web.Services
{
    /// <summary>
    /// Gets random seed information used to seed other random number generators.
    /// </summary>
    public sealed class RandomSeedService
    {
        public static readonly RandomSeedService Singleton = new RandomSeedService();

        private readonly RNGCryptoServiceProvider _FallBackEntropy = new RNGCryptoServiceProvider();
        private ConcurrentQueue<byte[]> _Seeds = new ConcurrentQueue<byte[]>();
        private readonly int _MinSeedsInReserve = 32;
        private readonly int _SeedSize = 32;
        private object _LoadingExternalDataFlag = new object();
        private FileInfo _RandomOrgApiKeyFile;
        private FileInfo _QrngPhysikCredentialFile;
        private MailSettings _MailSettings;
        private readonly byte[] _EntropyPool = new byte[16384];
        private int _EntropyIndex = 0;
        private int _MinimumEntropySizeBytes = 128;         // This many external bytes must be added to entropy before we start producing seeds.

        private static readonly IEnumerable<Uri> _UrlEntropySources = new Uri[] {
            // Use the front page of various websites as a source of entropy.
            // Yes, this does rather betray my Australian (and Sydney) heritage.
            new Uri("http://www.abc.net.au/news/"),
            new Uri("http://arstechnica.com/"),
            new Uri("http://www.foxnews.com/"),
            new Uri("http://stackoverflow.com/"),
            new Uri("http://www.news.com.au/"),
            new Uri("https://news.google.com.au/"),
            new Uri("http://news.ninemsn.com.au/"),
            new Uri("http://www.smh.com.au/"),
            new Uri("http://www.bbc.com/news/"),
            new Uri("http://edition.cnn.com/"),
            new Uri("http://www.amazon.com.au/"),
            new Uri("http://digg.com/"),
            new Uri("http://www.reddit.com/"),
            new Uri("http://en.wikipedia.org/wiki/Main_Page"),
            new Uri("http://www.nytimes.com/"),
            new Uri("http://www.livejournal.com/"),
            new Uri("https://github.com/explore"),
            new Uri("http://pastebin.com/trends"),
            new Uri("http://www.msn.com/?st=1"),
            new Uri("http://www.yandex.ru/"),
            new Uri("http://www.imdb.com/"),
            new Uri("http://www.cnet.com/"),
            new Uri("http://www.reuters.com/"),            
            new Uri("http://www.washingtonpost.com/"),            
            new Uri("http://issuu.com/"),            
            new Uri("http://superuser.com"),            
            new Uri("http://www.dailymotion.com/au"),            
            new Uri("http://www.telegraph.co.uk/"),            
            new Uri("http://www.yelp.com/sydney"),            
            new Uri("http://web.mit.edu/"),            
            new Uri("http://www.time.com/"),            
            new Uri("http://bj.100ye.com/"),            
            new Uri("http://www.nbcnews.com/"),            
            new Uri("http://www.nasa.gov/"),            
            new Uri("http://www.dailymail.co.uk/home/index.html"),            
            new Uri("http://www.wired.com/"),            
            new Uri("http://www.bloomberg.com/"),            
            new Uri("http://www.weather.com/"),            
            new Uri("http://www.whitehouse.gov/"),            
            new Uri("http://www.nationalgeographic.com/"),            
            new Uri("http://www.biglobe.ne.jp/"),            
            new Uri("http://marketingland.com/"),            
            new Uri("http://www.last.fm/"),            
            new Uri("http://www.independent.co.uk/"),            
            new Uri("http://www.kickstarter.com/discover"),            
            new Uri("http://www.bizjournals.com/"),            
            new Uri("http://www.nature.com/news/"),            
            new Uri("http://www.cbc.ca/"),            
            new Uri("http://www.un.org/news/"),            
            new Uri("http://www.zdnet.com/"),            
            new Uri("http://www.chicagotribune.com/"),            
            new Uri("http://ftc.gov/opa/index.shtml"),            
            new Uri("http://www.economist.com/"),            
            new Uri("http://www.thetimes.co.uk/tto/news/"),            
            new Uri("http://www.hc360.com/"),            
            new Uri("http://www.infoseek.co.jp/"),            
            new Uri("http://www.meetup.com/find/"),            
            new Uri("http://www.gizmodo.com.au/"),            
            new Uri("http://www.theglobeandmail.com/"),            
            new Uri("http://clerk.house.gov/floorsummary/floor.aspx"),            
            new Uri("http://www.thedailybeast.com/"),            
            new Uri("http://www.indiatimes.com/"),            
            new Uri("http://www.china.com.cn/"),            
            new Uri("http://www.metafilter.com"),
            new Uri("http://hckrnews.com/"),
            new Uri("http://boards.4chan.org/v/"),
            new Uri("http://boards.4chan.org/an/"),
            new Uri("http://boards.4chan.org/sci/"),
            new Uri("http://boards.4chan.org/mu/"),
            new Uri("http://boards.4chan.org/diy/"),
            new Uri("http://boards.4chan.org/biz/"),
            new Uri("http://boards.4chan.org/trv/"),
            new Uri("http://boards.4chan.org/lit/"),
            new Uri("http://boards.4chan.org/b/"),
            new Uri("http://boards.4chan.org/ck/"),
            new Uri("http://boards.4chan.org/p/"),
            new Uri("https://www.flickr.com/explore"),
            new Uri("https://beacon.nist.gov/rest/record/last"),        // https://beacon.nist.gov/home
        };
        private readonly IEnumerable<Func<byte[]>> _RandomGeneratorSources;

        public int SeedsInReserve { get { return this._Seeds.Count; } }
        public int TotalSeedsGenerated { get; private set; }
        public SeedGenerationStats LastSeedGenerationStats { get; private set; }
        public class SeedGenerationStats
        {
            public int SeedsAtStart { get; set; }
            public int SeedsAtEnd { get; set; }
            public int TotalGenerated { get; set ;}
            public TimeSpan TimeToFirstSeed { get; set; }
            public TimeSpan TimeToLastSeed { get; set; }
        }

        private RandomSeedService()
        {
#if !DEBUG
            _FallBackEntropy.GetBytes(_EntropyPool);        // The pool starts random.
#endif
            _RandomGeneratorSources = new Func<byte[]> [] {
                this.FetchRandomOrgData,
                this.FetchAnuRandomData,
                this.FetchNumbersInfoRandomData,
                this.FetchRandomServerRandomData,
                this.FetchPhysikRandomData,           // Requires unmanged code which isn't working in my hosting environment.
            };
        }

        /// <summary>
        /// Gets a 32 byte seed to use to initialise the a RandomService.
        /// </summary>
        public byte[] GetSeed()
        {
            // Check if we need to load more seed data.
            if (_Seeds.Count <= _MinSeedsInReserve)
                this.BeginLoadingExternalData();

            // Get the next seed.
            byte[] result;
            if (!_Seeds.TryDequeue(out result))
            {
                // Use the fallback option.
                result = this.GetFallbackRandomness(_SeedSize);
            }

            return result;
        }

        public void Init(string randomOrgApiKeyKilePath, string qrngPhysikCredentialPath, string mailSettingsPath)
        {
            _RandomOrgApiKeyFile = new FileInfo(randomOrgApiKeyKilePath);
            _QrngPhysikCredentialFile = new FileInfo(qrngPhysikCredentialPath);
            _MailSettings = JsonConvert.DeserializeObject<MailSettings>(File.ReadAllText(mailSettingsPath, Encoding.UTF8));
        }

        /// <summary>
        /// Fetch data from external sources in a background thread.
        /// Any requests for seeds will be serviced from local sources until these complete.
        /// </summary>
        public void BeginLoadingExternalData()
        {
            if (!this.IsLoadingExternalData())
            {
                // Spin up a background thread to load seeds into the queue.
                var t = new Thread(LoadExternalData);
                t.IsBackground = true;
                t.Name = "External Data Loader";
                t.Start();
            }
        }

        private void LoadExternalData()
        {
            try
            {
                var swFirst = new System.Diagnostics.Stopwatch();
                var swLast = new System.Diagnostics.Stopwatch();
                var seedsAtStart = _Seeds.Count;
                int seedsAtEnd = 0;
                var total = 0;
                bool triedToLoadData = false;

                if (Monitor.TryEnter(this._LoadingExternalDataFlag))
                {
                    try
                    {
                        // Don't load if we have enough seeds.
                        if (_Seeds.Count > _MinSeedsInReserve)
                            return;

                        // Fire off requests to all the sources of random data.
                        var sources = _UrlEntropySources.Select<Uri, Func<byte[]>>(url => () => FetchWebsiteData(url)).Concat(_RandomGeneratorSources);
                        var parallelFetch = sources
                                .Randomise()
                                .AsParallel()
                                .AsUnordered()
                                .WithDegreeOfParallelism(8)
                                .WithMergeOptions(ParallelMergeOptions.NotBuffered)
                                .Where(fn => fn != null)
                                .Select(fn =>
                                {
                                    try
                                    {
                                        return fn();
                                    }
                                    catch (Exception ex)
                                    {
#if !DEBUG
                                    ex.ToExceptionless().AddObject(fn.Method.Name + "()").AddTags("RandomSeed").Submit();
#endif
                                        return null;
                                    }
                                })
                                .Where(bs => bs != null);

                        // Timing how long it takes to get first and last seed..
                        triedToLoadData = true; 
                        swFirst.Start();
                        swLast.Start();
                        foreach (var randomBytes in parallelFetch)      // The parallel query does not start running until you start pulling from it.
                        {
                            // Add the random bytes to the pool in 64 byte blocks (which should be the minimum size of incoming byte arrays).
                            // We use smaller blocks here so we produce as many seeds as we can from the larger randomness sources.
                            const int blockSizeBytes = 64;
                            for (int i = 0; i < randomBytes.Length; i += blockSizeBytes)
                            {
                                lock (_EntropyPool)
                                {
                                    // Copy a block to the pool.
                                    if (_EntropyIndex + blockSizeBytes > _EntropyPool.Length)
                                        _EntropyIndex = 0;
                                    Array.Copy(randomBytes, i, _EntropyPool, _EntropyIndex, blockSizeBytes);
                                    _EntropyIndex += blockSizeBytes;

                                    // If we have accumulated a minimum amount of entropy, we start generating SHA256 hashes of the whole pool and add them as seeds.
                                    if (_EntropyIndex > _MinimumEntropySizeBytes)
                                    {
                                        var hasher = new SHA256Managed();
                                        _Seeds.Enqueue(hasher.ComputeHash(_EntropyPool));
                                        Interlocked.Increment(ref total);
                                        if (swFirst.IsRunning)
                                            swFirst.Stop();
                                    }
                                }
                            }
                        }

                        swLast.Stop();
                        seedsAtEnd = _Seeds.Count;
                    }
                    finally
                    {
                        Monitor.Exit(this._LoadingExternalDataFlag);
                    }
                }

                // At the end, send an email to the site owner so we know how often new seeds are being generated.
                if (triedToLoadData)
                {
                    this.LastSeedGenerationStats = new SeedGenerationStats()
                    {
                        SeedsAtStart = seedsAtStart,
                        SeedsAtEnd = seedsAtEnd,
                        TotalGenerated = total,
                        TimeToFirstSeed = swFirst.Elapsed,
                        TimeToLastSeed = swLast.Elapsed,
                    };
#if !DEBUG
                    ExceptionlessClient.Default.CreateFeatureUsage("RandomSeeds").AddObject(this.LastSeedGenerationStats).Submit();
#endif
                }
            }
            catch (Exception ex)
            {
                // Log. And bring down the app domain.
#if !DEBUG
                ex.ToExceptionless().MarkAsCritical().AddTags("RandomSeed", "LastChance").Submit();
#endif
#if DEBUG
                throw;
#endif
            }
        }

        private bool IsLoadingExternalData()
        {
            return Monitor.IsEntered(this._LoadingExternalDataFlag);
        }

        private byte[] FetchWebsiteData(Uri uri)
        {
            var hasher = new SHA512Managed();

            // The front page data of each website is hashed with SHA512 to derive random bytes.
            // This catches and logs exceptions and returns data from the fallback entropy source.
#if DEBUG
            Thread.Sleep(new Random().Next(250));
            var data = this.GetFallbackRandomness(64);
#else
            var sw = System.Diagnostics.Stopwatch.StartNew();
            byte[] siteData;
            try {
                siteData = RandomSeedService.FetchWebsiteRawData(uri);
            } catch (Exception ex) { 
                ex.ToExceptionless().AddObject(uri.ToString()).AddTags("RandomSeed").Submit();
                siteData = new byte[0];
            }
            sw.Stop();
            var data = BitConverter.GetBytes(sw.ElapsedTicks).Concat(siteData).ToArray();
#endif
            var result = hasher.ComputeHash(data);
            return result;
        }

        private byte[] FetchRandomOrgData()
        {
            // http://www.random.org/
            const int numberOfBytes = 512;
#if DEBUG
            Thread.Sleep(new Random().Next(2000));
            var randomOrgData = this.GetFallbackRandomness(numberOfBytes);
#else
            // https://api.random.org/json-rpc/1/introduction
            // https://api.random.org/json-rpc/1/basic
            // https://api.random.org/json-rpc/1/request-builder

            if (_RandomOrgApiKeyFile == null)
                throw new Exception("Random.org Key File not available.");

            var apiKey = Guid.Parse(File.ReadAllText(_RandomOrgApiKeyFile.FullName));
            var body = new {
                jsonrpc = "2.0",
                method = "generateBlobs",
                @params = new {
                    apiKey = apiKey.ToString("D"),
                    n = 1,
                    size = numberOfBytes * 8,
                    format = "base64",
                },
                id = 1,
            };
            var randomOrgApi = new Uri("https://api.random.org/json-rpc/1/invoke");
            var wc = new WebClient();
            wc.Headers.Add("Automatic", "makemeapassword@ligos.net");
            wc.Headers.Add("Content-Type", "application/json-rpc");
            wc.Headers.Add("User-Agent", "Microsoft.NET; makemeapassword.org; makemeapassword@ligos.net");
            var bodyAsString = JsonConvert.SerializeObject(body, Formatting.None);
            var rawResult = wc.UploadString(randomOrgApi, "POST", bodyAsString);

            dynamic jsonResult = JsonConvert.DeserializeObject(rawResult);
            if (jsonResult.error != null)
                throw new Exception(String.Format("Random.org error: {0} - {1}", (string)jsonResult.error.code, (string)jsonResult.error.message));
            var randomBase64 = (string)jsonResult.result.random.data[0];
            var randomOrgData = Convert.FromBase64String(randomBase64);
#endif
            return randomOrgData;
        }

        private byte[] FetchAnuRandomData()
        {
            // http://qrng.anu.edu.au/index.php
            const int arrays = 4;
            const int arraySize = 1024;
            const int numberOfBytes = arrays * arraySize;
#if DEBUG
            Thread.Sleep(new Random().Next(1500));
            var result = this.GetFallbackRandomness(numberOfBytes);
#else
            var apiUri = new Uri("https://qrng.anu.edu.au/API/jsonI.php?length=" + arrays.ToString() + "&type=hex16&size=" + arraySize.ToString());
            var wc = new WebClient();
            wc.Headers.Add("User-Agent", "Microsoft.NET; makemeapassword.org; makemeapassword@ligos.net");
            var rawResult = wc.DownloadString(apiUri);

            var result = new byte[numberOfBytes];
            dynamic jsonResult = JsonConvert.DeserializeObject(rawResult);
            var byteCount = 0;
            foreach (var hexBytes in (Newtonsoft.Json.Linq.JArray)jsonResult.data)
            {
                var bytes = hexBytes.ToString().ToByteArray();
                Array.Copy(bytes, 0, result, byteCount, bytes.Length);
                byteCount += bytes.Length;
            }
#endif
            return result;
        }
        private byte[] FetchPhysikRandomData()
        {
            // https://qrng.physik.hu-berlin.de/download
            // The SSL version of this requires unmanaged OpenSSL, which I'm not going to use just yet.
            const int numberOfBytes = 4096;


#if DEBUG
            Thread.Sleep(new Random().Next(1800));
            var result = this.GetFallbackRandomness(numberOfBytes);
            return result;
#else
            if (Environment.Is64BitProcess)
                // Needs to be a 32-bit process
                return null;

            // This follows the sample provided.
            var dll = new QrngPhysik();
            if (!dll.CheckDLL())
                throw new Exception("Unable to load libqrng.dll");

            var lines = File.ReadAllLines(_QrngPhysikCredentialFile.FullName);
            var username = lines[0];
            var password = lines[1];

            int bytesReceived = 0, iRet = 0;
            var result = new byte[numberOfBytes];
            iRet = QrngPhysik.qrng_connect_and_get_byte_array(username, password, result, result.Length, out bytesReceived);
            if (iRet != 0)
            {
                var errorMsg = "";
                try
                {
                    errorMsg = QrngPhysik.qrng_error_strings[iRet];
                }
                catch (IndexOutOfRangeException)
                {
                    errorMsg = "";
                }
                throw new Exception("Error calling QrngPhysik: " + iRet.ToString() + (String.IsNullOrEmpty(errorMsg) ? "" : " - " + errorMsg));
            }
            return result;
#endif


        }
        private byte[] FetchNumbersInfoRandomData()
        {
            // This supports SSL, but the cert isn't valid (it's for the uni, rather than the correct domain).
            // http://www.randomnumbers.info/content/Download.htm
            const int rangeOfNumbers = 4096-1;      // 12 bits per number (1.5 bytes).
            const int numberOfNumbers = 512;
            const int numberOfBytes = numberOfNumbers * 2;          // We waste 4 bits per number (for a total of 1024 bytes).
#if DEBUG
            Thread.Sleep(new Random().Next(1900));
            var result = this.GetFallbackRandomness(numberOfBytes);
#else
            var apiUri = new Uri("http://www.randomnumbers.info/cgibin/wqrng.cgi?amount=" + numberOfNumbers.ToString() + "&limit=" + rangeOfNumbers.ToString());
            var wc = new WebClient();
            wc.Headers.Add("User-Agent", "Microsoft.NET; makemeapassword.org; makemeapassword@ligos.net");
            var html = wc.DownloadString(apiUri);           // This returns HTML, which means I'm doing some hacky parsing here.
            
            // Locate some pretty clear boundaries around the random numbers returned.
            var startIdx = html.IndexOf("Download random numbers from quantum origin", StringComparison.OrdinalIgnoreCase);
            if (startIdx == -1)
                throw new Exception("Cannot locate start string in html parsing of randomnumbers.info result.");
            var endIdx = html.IndexOf("</td>", startIdx, StringComparison.OrdinalIgnoreCase);
            if (endIdx == -1)
                throw new Exception("Cannot locate end string in html parsing of randomnumbers.info result.");
            var haystack = html.Substring(startIdx, endIdx - startIdx);
            var numbersAndOtherJunk = haystack.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);      // Numbers are space sparated.
            var numbers = numbersAndOtherJunk
                            .Where(x => x.All(Char.IsDigit))        // Remove non-numberic junk.
                            .Select(x => Int16.Parse(x))            // Parse to an int16.
                            .ToList();
            var result = new byte[numberOfBytes];
            for (int i = 0; i < numbers.Count; i++)
            {
                // Take the Int16s in the range 0..4095 (4096 possibilities) and write them into the result array.
                // The top 4 bits will always be empty, but that doesn't matter too much as we hash everything anyway.
                var twoBytes = BitConverter.GetBytes(numbers[i]);
                result[i*2] = twoBytes[0];
                result[(i*2)+1] = twoBytes[1];
            }
#endif
            return result;
        }
        private byte[] FetchRandomServerRandomData()
        {
            // Somewhat unfortunately, this doesn't support https.
            // http://www.randomserver.dyndns.org/client/random.php
            const int numberOfBytes = 1024;
#if DEBUG
            Thread.Sleep(new Random().Next(1700));
            var result = this.GetFallbackRandomness(numberOfBytes);
#else
            var result = new byte[numberOfBytes];
            var apiUri = new Uri("http://www.randomserver.dyndns.org/client/random.php?type=BIN&a=0&b=0&n=" + numberOfBytes.ToString() + "&file=0");
            var wc = new WebClient();
            wc.Headers.Add("User-Agent", "Microsoft.NET; makemeapassword.org; makemeapassword@ligos.net");
            var bytes = wc.DownloadData(apiUri);
            Array.Copy(bytes, result, result.Length);
#endif
            return result;

        }
        private static byte[] FetchWebsiteRawData(Uri uri)
        {
            var request = WebRequest.Create(uri);
            request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.Reload);
            request.Method = "GET";
            if (request is HttpWebRequest)
                (request as HttpWebRequest).UserAgent = "Microsoft.NET; makemeapassword.org; makemeapassword@ligos.net";
            request.Timeout = (int)TimeSpan.FromSeconds(25).TotalMilliseconds;
            var response = request.GetResponse();
            var result = new MemoryStream();
            response.GetResponseStream().CopyTo(result);
            return result.ToArray();
        }

        private byte[] GetFallbackRandomness(int size)
        {
            var result = new byte[size];
            lock (_FallBackEntropy)
            {
                _FallBackEntropy.GetBytes(result);
            }
            return result;
        }

        private class MailSettings
        {
            public string from { get; set; }
            public string to { get; set; }
            public string server { get; set; }
        }
    }
}

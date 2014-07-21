﻿// Copyright 2014 Murray Grant
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
using System.IO;
using MurrayGrant.PasswordGenerator.Web.Helpers;
using Exceptionless;
using Newtonsoft.Json;

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

        private readonly int _BytesPerUrl = 64;     // Because using SHA512.
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
            new Uri("http://www.amazon.com/"),
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
            new Uri("http://www.sina.com.cn/"),
            new Uri("http://www.cnet.com/"),
            new Uri("http://www.reuters.com/"),            
            new Uri("http://www.washingtonpost.com/"),            
            new Uri("http://issuu.com/"),            
            new Uri("http://superuser.com"),            
            new Uri("http://www.dailymotion.com/au"),            
            new Uri("http://www.telegraph.co.uk/"),            
            new Uri("http://www.yelp.com/sydney"),            
            new Uri("http://web.mit.edu/"),            
            new Uri("http://www.time.com/time/"),            
            new Uri("http://www.100ye.com/"),            
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
        };

        public int SeedsInReserve { get { return this._Seeds.Count; } }
        public int TotalSeedsGenerated { get; private set; }
        private readonly Dictionary<Uri, Exception> _FailedUrls = new Dictionary<Uri, Exception>();
        public IEnumerable<Tuple<Uri, Exception>> FailuedUrls { get { return this._FailedUrls.Select(x => Tuple.Create(x.Key, x.Value)).ToList(); } }

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

        public void InitRandomOrg(string randomOrgApiKeyKilePath)
        {
            _RandomOrgApiKeyFile = new FileInfo(randomOrgApiKeyKilePath);
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
                lock (this._LoadingExternalDataFlag)
                {
                    // Fire off a request to random.org and get 4k of data.
                    var bytesToGetFromRandomOrg = _UrlEntropySources.Count() * _BytesPerUrl;
                    var rOrg = Task.Run(() => FetchRandomOrgData(bytesToGetFromRandomOrg));
                    Thread.Sleep(50);        // Yield for a moment to let the random.org request start first.

                    // Fire off requests to all the websites to get another 4k of data between them all.
                    var cancel = new CancellationTokenSource();
                    var parallelFetch = _UrlEntropySources
                            .Where(u => !this._FailedUrls.ContainsKey(u))
                            .Randomise()
                            .AsParallel()
                            .AsUnordered()
                            .WithMergeOptions(ParallelMergeOptions.NotBuffered)
                            .WithCancellation(cancel.Token)
                            .Select(url => 
                                {
                                    try {
                                        return FetchWebsiteData(url);
                                    } catch (Exception ex) {
                                        lock (_FailedUrls)
                                            _FailedUrls[url] = ex;
                                        ex.ToExceptionless().AddObject(url).AddTags("RandomSeed").Submit();
                                        return this.GetFallbackRandomness(64);
                                    }
                                });

                    int halfBlockSize = _SeedSize / 2;
                    int randomOrgIdx = 0;
                    byte[] randomOrgRandomness = null;

                    // The parallel query does not start running until you start pulling from it.
                    foreach (var websiteRandomness in parallelFetch)
                    {
                        // Now we need to synchronise with random.org data.
                        if (randomOrgRandomness == null)
                        {
                            try
                            {
                                rOrg.Wait(TimeSpan.FromSeconds(30));
                                if (rOrg.IsFaulted)
                                {
                                    lock (_FailedUrls)
                                        _FailedUrls[new Uri("http://www.random.org")] = rOrg.Exception.InnerException;
#if !DEBUG
                                    rOrg.Exception.ToExceptionless().MarkAsCritical().AddTags("RandomSeed", "random.org").Submit();
#endif
                                }
                                randomOrgRandomness = rOrg.Result;
                            }
                            catch (Exception ex)
                            {
                                randomOrgRandomness = this.GetFallbackRandomness(bytesToGetFromRandomOrg);
                                lock (_FailedUrls)
                                    _FailedUrls[new Uri("http://www.random.org")] = ex;
#if !DEBUG
                                rOrg.Exception.ToExceptionless().MarkAsCritical().AddTags("RandomSeed", "random.org").Submit();
#endif
                            }
                            Thread.MemoryBarrier();     // Not really using correct synchronisation primitives here, so this is a bit of paranoia.
                        }

                        // If we run out of random.org bytes first, we bail.
                        if (randomOrgIdx + _SeedSize > randomOrgRandomness.Length)
                        {
                            cancel.Cancel();
                            break;
                        }

                        // As requests return from the sites, we merge 16 bytes from random.org and each site.
                        var hasher = new SHA256Managed();
                        for (int siteIdx = 0; siteIdx < websiteRandomness.Length; siteIdx += halfBlockSize)
                        {
                            var block = new byte[halfBlockSize * 2];

                            // Copy data into the block, random.org data in the low half, site in high.
                            Array.Copy(randomOrgRandomness, randomOrgIdx, block, 0, halfBlockSize);
                            randomOrgIdx += halfBlockSize;
                            Array.Copy(websiteRandomness, siteIdx, block, halfBlockSize, halfBlockSize);
                            
                            // Then they are hashed, using SHA256 to produce the final 32 byte seed.
                            var hashedBlock = hasher.ComputeHash(block);

                            // And push each block into the concurrent queue, ready for requests.
                            _Seeds.Enqueue(hashedBlock);
                            this.TotalSeedsGenerated++;
                        }
                    }
                }
            }
        
            catch (Exception ex)
            {
                // Log. And bring down the app domain.
#if !DEBUG
                ex.ToExceptionless().MarkAsCritical().AddTags("RandomSeed", "LastChance").Submit();
#endif
                throw;
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
            // TODO: could use a stopwatch here to include network timings in the hash.
#if DEBUG
            Thread.Sleep(new Random().Next(2000));
            var siteData = this.GetFallbackRandomness(64);
#else
            var siteData = this.FetchWebsiteRawData(uri, "");
#endif
            var result = hasher.ComputeHash(siteData);
            return result;
        }

        private byte[] FetchRandomOrgData(int numberOfBytes)
        {
            // This catches and logs exceptions and returns data from the fallback entropy source.
#if DEBUG
            Thread.Sleep(new Random().Next(2000));
            var randomOrgData = this.GetFallbackRandomness(numberOfBytes);
#else
            // https://api.random.org/json-rpc/1/introduction
            // https://api.random.org/json-rpc/1/basic
            // https://api.random.org/json-rpc/1/request-builder

            if (_RandomOrgApiKeyFile == null)
                return this.GetFallbackRandomness(numberOfBytes);

            var apiKey = Guid.Parse(File.ReadAllText(_RandomOrgApiKeyFile.FullName));
            var body = new {
                jsonrpc = "2.0",
                method = "generateBlobs",
                @params = new {
                    apiKey = apiKey.ToString("D"),
                    n = 1,
                    size = 8 * 8, // numberOfBytes * 8,
                    format = "base64",
                },
                id = 1,
            };
            var randomOrgApi = new Uri("https://api.random.org/json-rpc/1/invoke");
            var wc = new WebClient();
            wc.Headers.Add("Automatic", "makemeapassword@ligos.net");
            wc.Headers.Add("Content-Type", "application/json-rpc");
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

        private byte[] FetchWebsiteRawData(Uri uri, string userAgent)
        {
            var request = WebRequest.Create(uri);
            request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.Reload);
            request.Method = "GET";
            if (!String.IsNullOrEmpty(userAgent) && request is HttpWebRequest)
                (request as HttpWebRequest).UserAgent = userAgent;
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
    }
}
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
using System.Web;
using System.Security.Cryptography;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using MurrayGrant.PasswordGenerator.Web.Helpers;
using System.Dynamic;
using Newtonsoft.Json;
using System.Text;

namespace MurrayGrant.PasswordGenerator.Web.Services
{
    /// <summary>
    /// Provides low level random number services.
    /// </summary>
    public sealed class RandomService
    {
        // Some of this is based on the KeePass CryptoRandom class.

        private readonly Func<byte[]> _SeedGetter;
        private readonly byte[] _Buffer;
        private readonly HMACSHA256 _HashFunction;
        private readonly Stopwatch _Stopwatch;
        private readonly TimeSpan _InitTime;
        private int _CurrentIdx;
        private long _Counter = 103;        // Small prime;
        private readonly static long _CounterPrime1 = 1229;
        private readonly static long _CounterPrime2 = 4357;
        private readonly static long _ANumber = 0x3043e9bcf7d9aa2b;
        private int _BytesUntilGuid = BytesUntilGuid;
        private const int BytesUntilGuid = 0x10007;                         // 64k + 7 bytes, so it doesn't clash with the additional seed as often.
        private int _BytesUntilAdditionalSeed = BytesUntilAdditionalSeed;
        private const int BytesUntilAdditionalSeed = 0x01000001;            // 16M + 1 byte, so it doesn't clash with the guid as often.

        private readonly Dictionary<RuntimeTypeHandle, StatsByType> _Statistics = new Dictionary<RuntimeTypeHandle,StatsByType>();
        private StatsByType _CurrentStats;
        private int _BytesConsumed;

        // Note, these are never killed off. They remain for the duration of the AppDomain.
        private static readonly ConcurrentDictionary<int, RandomService> _ThreadStaticRandoms = new ConcurrentDictionary<int, RandomService>();
        
        public static RandomService GetForCurrentThread()
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var result = _ThreadStaticRandoms.GetOrAdd(threadId, id => new RandomService(RandomSeedService.Singleton.GetSeed));
            return result;
        }
        public static IEnumerable<StatsByType> GetStats()
        {
            // Not sure about how threadsafe this is, but I'm not too concerned if I can't get accurate stats.
            return _ThreadStaticRandoms.SelectMany(x => x.Value._Statistics).Select(x => x.Value).ToList();
        }

        public RandomService(Func<byte[]> seedGetter)
        {
            if (seedGetter == null)
                throw new ArgumentNullException(nameof(seedGetter));

            var initialSeed = seedGetter();
            if (initialSeed.Length != 32)
                throw new ArgumentException("Expected 32 bytes of seed data.");
            _SeedGetter = seedGetter;

            // Start the stopwatch.
            _Stopwatch = Stopwatch.StartNew();
            _Counter += Thread.CurrentThread.ManagedThreadId;

            // The seed is placed in the high 32 bytes of the buffer.
            _Buffer = new byte[64];
            Buffer.BlockCopy(initialSeed, 0, _Buffer, 32, initialSeed.Length);


            // Initialise our HMAC, used as the hash function to generate randomness.
            var secret = new byte[32];
            using (var rand = new RNGCryptoServiceProvider())
                rand.GetBytes(secret);
            _HashFunction = new HMACSHA256(secret);


            // Gather up some additional cheap(-ish) entropy from various sources.
            var ms = new MemoryStream();
            ms.WriteBytes(BitConverter.GetBytes(_Counter));
            ms.WriteBytes(Guid.NewGuid().ToByteArray());
            ms.WriteBytes(BitConverter.GetBytes(DateTime.UtcNow.Ticks));

            // Various numbers from the current process.
            var p = Process.GetCurrentProcess();
            ms.WriteBytes(BitConverter.GetBytes(p.Id));
            ms.WriteBytes(BitConverter.GetBytes(p.HandleCount));
            ms.WriteBytes(BitConverter.GetBytes(p.PagedMemorySize64));
            ms.WriteBytes(BitConverter.GetBytes(p.PeakPagedMemorySize64));
            ms.WriteBytes(BitConverter.GetBytes(p.PeakVirtualMemorySize64));
            ms.WriteBytes(BitConverter.GetBytes(p.PeakWorkingSet64));
            ms.WriteBytes(BitConverter.GetBytes(p.PrivateMemorySize64));
            ms.WriteBytes(BitConverter.GetBytes(p.PrivilegedProcessorTime.Ticks));
            ms.WriteBytes(BitConverter.GetBytes(p.StartTime.Ticks));
            ms.WriteBytes(BitConverter.GetBytes(p.UserProcessorTime.Ticks));
            ms.WriteBytes(BitConverter.GetBytes(p.WorkingSet64));

            // Network bytes consumed.
            foreach (var stats in NetworkInterface.GetAllNetworkInterfaces().Select(x => x.GetIPStatistics()))
            {
                ms.WriteBytes(BitConverter.GetBytes(stats.BytesReceived));
                ms.WriteBytes(BitConverter.GetBytes(stats.BytesSent));
            }

            // Disk size and free space.
            foreach (var di in DriveInfo.GetDrives().Where(x => x.IsReady && (x.DriveType == DriveType.Fixed || x.DriveType == DriveType.Removable)))
            {
                ms.WriteBytes(BitConverter.GetBytes(di.TotalSize));
                ms.WriteBytes(BitConverter.GetBytes(di.TotalFreeSpace));
                ms.WriteBytes(BitConverter.GetBytes(di.AvailableFreeSpace));
            }

            // The time take to get all that information.
            ms.WriteBytes(BitConverter.GetBytes(_Stopwatch.ElapsedTicks));
            
            // And the time of a quick yield operation.
            Thread.Sleep(1);
            ms.WriteBytes(BitConverter.GetBytes(_Stopwatch.ElapsedTicks));

            // Take a hash of all the data we gathered and place in the bottom 32 bytes of our buffer.
            ms.Position = 0L;
            var weakEntropy = new SHA256Managed().ComputeHash(ms);
            Buffer.BlockCopy(weakEntropy, 0, _Buffer, 0, weakEntropy.Length);

            // And reset counters such that the first time GetNextBytes() is called, we hash all the raw data.
            this._CurrentIdx = 32;

            this._InitTime = _Stopwatch.Elapsed;
        }


        private void GenerateNextBlock()
        {
            // The randomisation is achieved by using half the previous block and one of a few sources of entropy.
            // Usually, cheap sources are used (timers, clocks, constants).
            // Occasionally, a Guid is added into the mix.
            // Even less often, an entirely new 32 byte seed is used.

            // Copy the low 32 bytes to the top 32 bytes.
            Buffer.BlockCopy(_Buffer, 0, _Buffer, 32, 32);

            if (_BytesUntilAdditionalSeed <= 0)
            {
                // Fetch a new seed and places it in the bottom 32 bytes.
                var seed32Bytes = _SeedGetter();
                Buffer.BlockCopy(seed32Bytes, 0, _Buffer, 0, 32);
                _BytesUntilAdditionalSeed = BytesUntilAdditionalSeed;
            }
            else if (_BytesUntilGuid <= 0)
            {
                // Use a guid with the counter and stopwatch timer in the bottom 32 bytes.
                Buffer.BlockCopy(BitConverter.GetBytes(_Stopwatch.ElapsedTicks), 0, _Buffer, 0, 8);
                var guid = Guid.NewGuid();
                Buffer.BlockCopy(guid.ToByteArray(), 0, _Buffer, 8, 16);
                unchecked {
                    _Counter *= _CounterPrime1 * _CounterPrime2;
                }
                Buffer.BlockCopy(BitConverter.GetBytes(_Counter), 0, _Buffer, 24, 8);
                _BytesUntilGuid = BytesUntilGuid;
            }
            else
            {
                // Write some cheap entropy to the bottom 32 bytes.
                Buffer.BlockCopy(BitConverter.GetBytes(_ANumber), 0, _Buffer, 0, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(DateTime.UtcNow.Ticks), 0, _Buffer, 8, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(_Stopwatch.ElapsedTicks), 0, _Buffer, 16, 8);
                unchecked {
                    _Counter *= _CounterPrime1 * _CounterPrime2;
                }
                Buffer.BlockCopy(BitConverter.GetBytes(_Counter), 0, _Buffer, 24, 8);
            }

            // Run our hash function over the entire buffer.
            // Note that the hash function contains a secret key.
            var hashResult = this._HashFunction.ComputeHash(_Buffer);

            // And write the result to the bottom 32 bytes to be used as our next block of randomness.
            Buffer.BlockCopy(hashResult, 0, _Buffer, 0, 32);
            
            // Reset the index and update counters.
            _CurrentIdx = 0;
            _BytesUntilGuid -= 32;
            _BytesUntilAdditionalSeed -= 32;

        }
        private byte[] GetRandomBlock(int length)
        {
            // Copy at most length bytes, or whatever is remaining in our current block.
            var size = Math.Min(length, 32 - _CurrentIdx);
            var result = new byte[size];
            Buffer.BlockCopy(_Buffer, _CurrentIdx, result, 0, size);
            
            // Update the index and possibly generate a new block.
            _CurrentIdx += size;
            if (_CurrentIdx >= 31)
                GenerateNextBlock();

            return result;
        }

        public void BeginStats(Type controllerType)
        {
            this._CurrentStats = new StatsByType(controllerType, _InitTime);
            this._BytesConsumed = 0;
        }
        public void IncrementStats(string password)
        {
            var bytes = Encoding.UTF8.GetByteCount(password);
            this._CurrentStats.PasswordsGenerated++;
            this._CurrentStats.PasswordUtf8BytesGenerated += bytes;
            this._CurrentStats.PasswordCharactersGenerated += password.Length;
        }
        public void EndStats()
        {
            this._CurrentStats.RandomBytesConsumed += this._BytesConsumed;
            StatsByType acc;
            if (!this._Statistics.TryGetValue(_CurrentStats.StyleTypeHandle, out acc))
                acc = new StatsByType(_CurrentStats.StyleType, _InitTime);
            acc.Add(this._CurrentStats);
            acc.LastUsedUtc = DateTime.UtcNow;
            this._Statistics[_CurrentStats.StyleTypeHandle] = acc;
        }

        public byte[] GetNextBytes(int length)
        {
            if (length <= 0 || length > 1024)
                throw new ArgumentOutOfRangeException("length", "Length must be between 1 and 1024, inclusive.");

            // Generate length bytes.
            var result = new byte[length];
            var idx = 0;
            while (idx < result.Length)
            {
                var block = GetRandomBlock(result.Length - idx);
                Buffer.BlockCopy(block, 0, result, idx, block.Length);

                idx += block.Length;
            }

            // Increment our generated bytes counter for stats.
            this._BytesConsumed += length;
            return result;
        }


        // Implementation for Next() based on http://codereview.stackexchange.com/questions/6304/algorithm-to-convert-random-bytes-to-integers
        public int Next()
        {
            byte[] bytes = this.GetNextBytes(4);
            int i = BitConverter.ToInt32(bytes, 0);
            return i & Int32.MaxValue;
        }
        public int Next(int maxExlusive)
        {
            if (maxExlusive <= 0) throw new ArgumentOutOfRangeException("maxExlusive", maxExlusive, "maxExlusive must be positive");

            // Let k = (Int32.MaxValue + 1) % maxExcl
            // Then we want to exclude the top k values in order to get a uniform distribution
            // You can do the calculations using uints if you prefer to only have one %
            int k = ((Int32.MaxValue % maxExlusive) + 1) % maxExlusive;
            int result = this.Next();
            while (result > Int32.MaxValue - k)
                result = this.Next();
            return result % maxExlusive;
        }
        public int Next(int minValue, int maxValue)
        {
            if (minValue < 0)
                throw new ArgumentOutOfRangeException("minValue", minValue, "minValue must be non-negative");
            if (maxValue <= minValue)
                throw new ArgumentOutOfRangeException("maxValue", maxValue, "maxValue must be greater than minValue");

            return minValue + this.Next(maxValue - minValue);
        }

        /// <summary>
        /// A Single between 0.0 and 1.0.
        /// </summary>
        public float NextSingle()
        {
            return this.Next() * (1.0f / Int32.MaxValue);
        }




        public class StatsByType
        {
            [JsonIgnore]
            public readonly RuntimeTypeHandle StyleTypeHandle;
            [JsonIgnore]
            public Type StyleType { get { return Type.GetTypeFromHandle(this.StyleTypeHandle); } }
            public String Style { get { return this.StyleType.Name.Replace("Api", "").Replace("Controller", "").Replace("V1", ""); } }

            public readonly int ThreadId;
            public readonly TimeSpan InitTime;
            public DateTime LastUsedUtc;
            public long RandomBytesConsumed;
            public long PasswordsGenerated;
            public long PasswordUtf8BytesGenerated;
            public long PasswordCharactersGenerated;

            public StatsByType(Type t, TimeSpan initTime)
            {
                this.StyleTypeHandle = t.TypeHandle;
                this.ThreadId = Thread.CurrentThread.ManagedThreadId;
                this.InitTime = initTime;
            }

            public void Add(StatsByType other)
            {
                this.RandomBytesConsumed += other.RandomBytesConsumed;
                this.PasswordsGenerated += other.PasswordsGenerated;
                this.PasswordUtf8BytesGenerated += other.PasswordUtf8BytesGenerated;
                this.PasswordCharactersGenerated += other.PasswordCharactersGenerated;
            }
        }
    }
}
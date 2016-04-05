using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Columns;

using MurrayGrant.PasswordGenerator.Web.Services;

namespace Benchmarks
{
    [Config(typeof(RandomServiceBenchmarks.Config))]
    public class RandomServiceBenchmarks
    {
        private static readonly byte[] _Seed = new Random(1).GetBytes(32);

        private readonly RandomService _RandomHmacSha256 = new RandomService(() => _Seed, new HMACSHA256());
        private readonly RandomService _RandomHmacSha512 = new RandomService(() => _Seed, new HMACSHA512());
        private readonly RandomService _RandomSha256Managed = new RandomService(() => _Seed, new SHA256Managed());
        private readonly RandomService _RandomSha256Cng = new RandomService(() => _Seed, new SHA256Cng());
        private readonly RandomService _RandomSha512Managed = new RandomService(() => _Seed, new SHA512Managed());
        private readonly RandomService _RandomSha512Cng = new RandomService(() => _Seed, new SHA512Cng());
        private readonly RandomService _RandomNull256 = new RandomService(() => _Seed, new NullHashFunction256());
        private readonly RandomService _RandomNull512 = new RandomService(() => _Seed, new NullHashFunction512());

        private class Config : ManualConfig 
        { 
            public Config()
            { 
                Add(Job.LegacyJitX86, Job.RyuJitX64);
                Add(StatisticColumn.AllStatistics);
            } 
        }

        [Benchmark()]
        public int Random_Int32_HmacSha256()
        {
            return _RandomHmacSha256.Next();
        }
        [Benchmark()]
        public int Random_Int32_HmacSha512()
        {
            return _RandomHmacSha512.Next();
        }

        [Benchmark()]
        public int Random_Int32_Sha256Managed()
        {
            return _RandomSha256Managed.Next();
        }
        [Benchmark()]
        public int Random_Int32_Sha256Cng()
        {
            return _RandomSha256Cng.Next();
        }

        [Benchmark()]
        public int Random_Int32_Sha512Managed()
        {
            return _RandomSha512Managed.Next();
        }
        [Benchmark()]
        public int Random_Int32_Sha512Cng()
        {
            return _RandomSha512Cng.Next();
        }

        [Benchmark()]
        public int Random_Int32_Null256()
        {
            return _RandomNull256.Next();
        }
        [Benchmark()]
        public int Random_Int32_Null512()
        {
            return _RandomNull512.Next();
        }
    }

    internal class NullHashFunction256 : HashAlgorithm
    {
        public override int HashSize => 256;
        public override void Initialize() { }
        
        protected override void HashCore(byte[] array, int ibStart, int cbSize) { }
        protected override byte[] HashFinal() => new byte[32];
    }
    internal class NullHashFunction512 : HashAlgorithm
    {
        public override int HashSize => 512;
        public override void Initialize() { }

        protected override void HashCore(byte[] array, int ibStart, int cbSize) { }
        protected override byte[] HashFinal() => new byte[64];
    }


    internal static class RandomHelper
    {
        public static byte[] GetBytes(this Random random, int n)
        {
            var result = new byte[n];
            random.NextBytes(result);
            return result;
        }
    }
}

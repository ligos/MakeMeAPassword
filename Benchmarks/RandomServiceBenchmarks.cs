using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private static readonly byte[] _Seed = new byte[32];
        private readonly RandomService _Random = new RandomService(_Seed);

        private class Config : ManualConfig 
        { 
            public Config()
            { 
                Add(Job.LegacyJitX86, Job.RyuJitX64);
                Add(StatisticColumn.AllStatistics);
            } 
        }

        [Benchmark()]
        public int Random_Int32()
        {
            return _Random.Next();
        }
        [Benchmark()]
        public float Random_Single()
        {
            return _Random.NextSingle();
        }
        [Benchmark()]
        public byte[] Random_1024Bytes()
        {
            return _Random.GetNextBytes(1024);
        }
    }
}

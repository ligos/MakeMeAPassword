// Copyright 2024 Murray Grant
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

namespace MurrayGrant.MakeMeAPassword.Web.Models
{
    /// <summary>
    /// Application configuration.
    /// </summary>
    public class AppConfig
    {
        public TerningerConfiguration? Terninger { get; set; }
    }

    public class TerningerConfiguration
    {
        public string? NetworkUserAgentIdentifier { get; set; } = "makemeapassword.ligos.net";

        public int LinearPoolCount { get; set; } = 32;
        public int RandomPoolCount { get; set; } = 16;

        public string? PersistentStatePath { get; set; }

        public int CryptoRandomSampleSize { get; set; } = 64;
        
        public string? AnuApiKey { get; set; }
        public string? RandomOrgApiKey { get; set; }

        public MurrayGrant.Terninger.Random.PooledEntropyCprngGenerator.PooledGeneratorConfig? TerningerPooledGeneratorConfig { get; set; } 
            = new Terninger.Random.PooledEntropyCprngGenerator.PooledGeneratorConfig()
            {
                EntropyToTriggerReseedInHighPriority = 64,
            };
    }
}

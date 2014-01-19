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
using System.Linq;
using System.Web;
using MurrayGrant.PasswordGenerator.Web.Services;

namespace MurrayGrant.PasswordGenerator.Web.ViewModels.ApiV1
{
    public class JsonCombinationContainer
    {
        // Using javascript lowercase conventions.
        private double _combinations;
        public double combinations 
        { 
            get { return this._combinations; }
            set 
            {
                this._combinations = value;
                this.rating = PasswordRatingService.Rate(this.combinations);
            }
        }
        public int rating { get; set; }
        public string formatted { get { return combinations.ToString("N0"); } }
        public string base10 { get { return combinations.ToString("E2"); } }
        public string base2 { get { return Math.Log(combinations, 2).ToString("N2"); } }
    }
}
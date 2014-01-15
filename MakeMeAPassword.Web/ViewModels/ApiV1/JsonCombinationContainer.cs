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
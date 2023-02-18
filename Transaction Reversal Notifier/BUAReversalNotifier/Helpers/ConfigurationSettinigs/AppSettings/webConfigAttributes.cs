using System;
using System.Collections.Generic;
using System.Text;

namespace MbokoReversalNotifier.Helpers.ConfigurationSettinigs.AppSettings
{
    public class webConfigAttributes
    {
        public int jobDelay { get; set; }
        public string CementAccountOBU { get; set; }
        public string CementAccountSOKOTO { get; set; }
        public string FoodAccount { get; set; }
        public string CementVerificationUrl { get; set; }
        public string FoodVerificationUrl { get; set; }
        public string CementLegacyUrl { get; set; }
        public string FoodLegacyUrl { get; set; }
        public string Token { get; set; }
    }
}

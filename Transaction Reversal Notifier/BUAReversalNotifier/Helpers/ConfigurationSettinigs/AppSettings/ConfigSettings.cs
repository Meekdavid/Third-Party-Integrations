using System;
using System.Collections.Generic;
using System.Text;
//using MbokoReversalNotifier.Helpers.ConfigurationSettinigs.AppSettings;

namespace MbokoReversalNotifier.Helpers.ConfigurationSettinigs.AppSettings
{
    public class ConfigSettings
    {
        public static Connectionstrings ConnectionString => ConfigurationSettingsHelper.GetConfigurationSectionObject<Connectionstrings>("ConnectionStrings");
        public static webConfigAttributes webConfigAttributes => ConfigurationSettingsHelper.GetConfigurationSectionObject<webConfigAttributes>("webConfigAttributes");
        //public static webConfigAttributes AppSetting => ConfigEnhancer.GetConfigurationSectionObject<webConfigAttributes>("webConfigAttributes");
        //public static Connectionstrings ConnectionString => GetConfigurationSectionObject<Connectionstrings>("ConnectionStrings");
    }
}

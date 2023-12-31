using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ImageRate
{
    public class SettingsHelper
    {

        private static ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public static String getStringOrDefault(String key, String defaultValue)
        {
            String resultString = localSettings.Values[key] as string;
            return resultString != null ? resultString : defaultValue;
        }

        public static int getIntOrDefault(String key, int defaultValue)
        {
            return int.Parse(getStringOrDefault(key, defaultValue.ToString()));
        }

        public static void set(String key, object value)
        {
            localSettings.Values[key] = value.ToString();
        }

    }
}
